using System;
using System.Runtime.InteropServices;
using System.Text;

namespace UnibetGraphicsCapture
{
    public static class Utils
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public static IntPtr FindWindowByTitleContains(string titlePart)
        {
            IntPtr foundHwnd = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                var sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, sb.Capacity);

                if (sb.ToString().Contains(titlePart, StringComparison.OrdinalIgnoreCase))
                {
                    foundHwnd = hWnd;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            return foundHwnd;
        }
    }
}
