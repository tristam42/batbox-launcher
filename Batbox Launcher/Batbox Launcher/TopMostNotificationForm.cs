using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BatboxLauncher
{
    internal sealed class TopMostNotificationForm : Form
    {
        private readonly System.Windows.Forms.Timer? _closeTimer;
        private readonly System.Windows.Forms.Timer _keepTopMostTimer;

        private static readonly IntPtr HWND_TOPMOST = new(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        public TopMostNotificationForm(string title, string message, bool warning, int durationMs)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            Width = 360;
            Height = 110;
            BackColor = Color.FromArgb(35, 35, 35);

            var borderColor = warning ? Color.FromArgb(220, 80, 80) : Color.FromArgb(70, 150, 255);
            var border = new Panel
            {
                Dock = DockStyle.Left,
                Width = 5,
                BackColor = borderColor
            };
            Controls.Add(border);

            var titleLabel = new Label
            {
                AutoSize = false,
                Location = new Point(16, 10),
                Size = new Size(330, 26),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = title
            };

            var messageLabel = new Label
            {
                AutoSize = false,
                Location = new Point(16, 38),
                Size = new Size(330, 58),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(220, 220, 220),
                Text = message
            };

            Controls.Add(titleLabel);
            Controls.Add(messageLabel);

            var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            int centerX = area.Left + (area.Width - Width) / 2;
            Location = new Point(centerX, area.Top + 12);

            if (durationMs > 0)
            {
                _closeTimer = new System.Windows.Forms.Timer { Interval = Math.Max(1200, durationMs) };
                _closeTimer.Tick += (s, e) =>
                {
                    _closeTimer.Stop();
                    Close();
                };
            }

            // Reassert topmost while visible; helps when other apps grab foreground.
            _keepTopMostTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _keepTopMostTimer.Tick += (s, e) => ForceTopMost();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ForceTopMost();
            _closeTimer?.Start();
            _keepTopMostTimer.Start();
        }

        private void ForceTopMost()
        {
            if (IsDisposed || !IsHandleCreated) return;

            TopMost = true;
            BringToFront();
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _closeTimer?.Dispose();
                _keepTopMostTimer.Stop();
                _keepTopMostTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
