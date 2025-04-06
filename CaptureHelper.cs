using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace UnibetGraphicsCapture
{
    public static class CaptureHelper
    {
        public static async Task<Bitmap> CaptureWindowAsync(IntPtr hwnd)
        {
            var factory = new SharpDX.DXGI.Factory1();
            var adapter = factory.Adapters1[0];
            var device = new SharpDX.Direct3D11.Device(adapter);

            IDirect3DDevice d3dDevice = CreateDirect3DDeviceFromSharpDXDevice(device);

            var item = GraphicsCaptureItem.CreateFromVisual(hwnd);
            var framePool = Direct3D11CaptureFramePool.Create(
                d3dDevice,
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                1,
                item.Size);

            var session = framePool.CreateCaptureSession(item);
            session.StartCapture();

            var frame = await Task.Run(() =>
            {
                GraphicsCaptureFrame capturedFrame = null;
                while (capturedFrame == null)
                {
                    capturedFrame = framePool.TryGetNextFrame();
                }
                return capturedFrame;
            });

            var surface = frame.Surface;
            var tex = Direct3D11Helper.CreateSharpDXTexture2D(surface);

            var bitmap = new Bitmap(item.Size.Width, item.Size.Height, PixelFormat.Format32bppArgb);
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            device.ImmediateContext.CopyResource(tex, tex);
            var mapSource = device.ImmediateContext.MapSubresource(tex, 0, MapMode.Read, MapFlags.None);

            for (int y = 0; y < bitmap.Height; y++)
            {
                Utilities.CopyMemory(data.Scan0 + y * data.Stride, mapSource.DataPointer + y * mapSource.RowPitch, bitmap.Width * 4);
            }

            device.ImmediateContext.UnmapSubresource(tex, 0);
            bitmap.UnlockBits(data);

            session.Dispose();
            framePool.Dispose();
            device.Dispose();

            return bitmap;
        }

        private static IDirect3DDevice CreateDirect3DDeviceFromSharpDXDevice(SharpDX.Direct3D11.Device device)
        {
            var dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device>();
            var comPtr = Marshal.GetIUnknownForObject(dxgiDevice);
            var direct3DDevice = (IDirect3DDevice)WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(comPtr);
            return direct3DDevice;
        }
    }
}