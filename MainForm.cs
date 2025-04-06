using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UnibetGraphicsCapture
{
    public class MainForm : Form
    {
        private ComboBox windowList;
        private Button refreshButton;
        private Button captureButton;

        public MainForm()
        {
            Text = "Capture iPoker/Unibet";
            Width = 400;
            Height = 150;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            windowList = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300, Left = 20, Top = 20 };
            refreshButton = new Button { Text = "🔄 Rafraîchir", Left = 20, Top = 60, Width = 100 };
            captureButton = new Button { Text = "📸 Capturer", Left = 140, Top = 60, Width = 100 };

            refreshButton.Click += (s, e) => LoadWindowList();
            captureButton.Click += async (s, e) => await CaptureSelectedWindow();

            Controls.Add(windowList);
            Controls.Add(refreshButton);
            Controls.Add(captureButton);

            LoadWindowList();
        }

        private void LoadWindowList()
        {
            windowList.Items.Clear();
            PInvoke.EnumWindows((hWnd, lParam) =>
            {
                if (!Utils.IsWindowVisible(hWnd)) return true;
                var sb = new StringBuilder(256);
                Utils.GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();
                if (!string.IsNullOrWhiteSpace(title))
                    windowList.Items.Add(new WindowItem { Handle = hWnd, Title = title });
                return true;
            }, 0);

            if (windowList.Items.Count > 0) windowList.SelectedIndex = 0;
        }

        private async Task CaptureSelectedWindow()
        {
            if (windowList.SelectedItem is WindowItem item)
            {
                var bitmap = await CaptureHelper.CaptureWindowAsync(item.Handle);
                string filename = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                bitmap.Save(filename, ImageFormat.Png);
                MessageBox.Show($"Capture sauvegardée : {filename}", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private class WindowItem
        {
            public IntPtr Handle { get; set; }
            public string Title { get; set; }
            public override string ToString() => Title;
        }
    }
}