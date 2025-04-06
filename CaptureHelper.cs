using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Diagnostics;

namespace UnibetGraphicsCapture
{
    public static class CaptureHelper
    {
        public static async Task<Bitmap> CaptureWindowAsync(IntPtr hwnd)
        {
            try
            {
                // Création du device Direct3D
                var factory = new SharpDX.DXGI.Factory2();
                var adapter = factory.GetAdapter1(0);
                var device = new SharpDX.Direct3D11.Device(adapter);

                // Conversion en IDirect3DDevice pour WinRT
                IDirect3DDevice d3dDevice = CreateDirect3DDeviceFromSharpDXDevice(device);

                // Création de l'élément de capture
                var item = CreateCaptureItemForWindow(hwnd);
                if (item == null)
                {
                    Debug.WriteLine("Impossible de créer l'élément de capture pour la fenêtre");
                    return null;
                }

                // Création du pool de frames
                var framePool = Direct3D11CaptureFramePool.Create(
                    d3dDevice,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    1,
                    item.Size);

                // Démarrage de la session de capture
                var session = framePool.CreateCaptureSession(item);
                session.StartCapture();

                // Récupération d'une frame
                var frame = await Task.Run(() =>
                {
                    GraphicsCaptureFrame capturedFrame = null;
                    int attempts = 0;
                    while (capturedFrame == null && attempts < 100)
                    {
                        capturedFrame = framePool.TryGetNextFrame();
                        attempts++;
                        if (capturedFrame == null)
                            Task.Delay(10).Wait();
                    }
                    return capturedFrame;
                });

                if (frame == null)
                {
                    Debug.WriteLine("Impossible d'obtenir une frame");
                    session.Dispose();
                    framePool.Dispose();
                    return null;
                }

                // Conversion de la frame en bitmap
                var width = item.Size.Width;
                var height = item.Size.Height;
                var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                // Accès à la surface Direct3D
                var surface = frame.Surface;
                var tex = Direct3D11Helper.CreateSharpDXTexture2D(surface);

                // Copie des données vers le bitmap
                var data = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);

                // Lecture des données du texture
                device.ImmediateContext.CopyResource(tex, tex);
                var mapSource = device.ImmediateContext.MapSubresource(
                    tex,
                    0,
                    MapMode.Read,
                    SharpDX.Direct3D11.MapFlags.None);

                // Copie ligne par ligne
                for (int y = 0; y < bitmap.Height; y++)
                {
                    IntPtr dstPtr = data.Scan0 + y * data.Stride;
                    IntPtr srcPtr = mapSource.DataPointer + y * mapSource.RowPitch;
                    Utilities.CopyMemory(dstPtr, srcPtr, bitmap.Width * 4);
                }

                // Nettoyage
                device.ImmediateContext.UnmapSubresource(tex, 0);
                bitmap.UnlockBits(data);
                tex.Dispose();
                frame.Dispose();
                session.Dispose();
                framePool.Dispose();
                device.Dispose();
                adapter.Dispose();
                factory.Dispose();

                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la capture: {ex.Message}");
                return null;
            }
        }

        private static IDirect3DDevice CreateDirect3DDeviceFromSharpDXDevice(SharpDX.Direct3D11.Device device)
        {
            var dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device>();
            var comPtr = Marshal.GetIUnknownForObject(dxgiDevice);
            var direct3DDevice = MarshalInterface<IDirect3DDevice>.FromAbi(comPtr);
            Marshal.Release(comPtr);
            dxgiDevice.Dispose();
            return direct3DDevice;
        }

        private static GraphicsCaptureItem CreateCaptureItemForWindow(IntPtr hwnd)
        {
            try
            {
                if (GraphicsCaptureItem.TryCreateFromWindowId(hwnd, out var item))
                {
                    return item;
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la création de l'élément de capture: {ex.Message}");
                return null;
            }
        }
    }

    // Classe d'aide pour la conversion entre Direct3D11 et SharpDX
    public static class Direct3D11Helper
    {
        [ComImport]
        [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        interface IDirect3DDxgiInterfaceAccess
        {
            IntPtr GetInterface([In] ref Guid iid);
        }

        public static Texture2D CreateSharpDXTexture2D(IDirect3DSurface surface)
        {
            var access = (IDirect3DDxgiInterfaceAccess)surface;
            var iid = typeof(SharpDX.DXGI.Resource).GUID;
            var resourcePointer = access.GetInterface(ref iid);
            var resource = new SharpDX.DXGI.Resource(resourcePointer);
            var texture = resource.QueryInterface<SharpDX.Direct3D11.Texture2D>();
            resource.Dispose();
            return texture;
        }
    }
}
