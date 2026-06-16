using System.Drawing;
using System.Windows.Forms;

namespace BatboxLauncher
{
    internal sealed class TopMostNotificationForm : Form
    {
        private readonly System.Windows.Forms.Timer? _closeTimer;

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
            Location = new Point(area.Right - Width - 12, area.Top + 12);

            if (durationMs > 0)
            {
                _closeTimer = new System.Windows.Forms.Timer { Interval = Math.Max(1200, durationMs) };
                _closeTimer.Tick += (s, e) =>
                {
                    _closeTimer.Stop();
                    Close();
                };
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _closeTimer?.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _closeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
