using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;

namespace UnibetGraphicsCapture
{
    public static class Utils
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        public static IntPtr FindWindowByTitleContains(string titlePart)
        {
            IntPtr foundHwnd = IntPtr.Zero;

            PInvoke.EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                var sb = new System.Text.StringBuilder(256);
                GetWindowText(hWnd, sb, sb.Capacity);

                if (sb.ToString().Contains(titlePart, StringComparison.OrdinalIgnoreCase))
                {
                    foundHwnd = hWnd;
                    return false;
                }

                return true;
            }, 0);

            return foundHwnd;
        }
    }
}