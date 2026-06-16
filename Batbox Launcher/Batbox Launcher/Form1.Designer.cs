namespace BatboxLauncher
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.GroupBox grpControl;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel pnlServerLight;
        private System.Windows.Forms.Label lblServerStatus;
        private System.Windows.Forms.Button btnToggleSettings;

        private System.Windows.Forms.Panel pnlSettings;
        private System.Windows.Forms.Label lblLnkPath;
        private System.Windows.Forms.TextBox txtLnkPath;
        private System.Windows.Forms.Button btnBrowseLnk;
        private System.Windows.Forms.Button btnRestoreDefaults;
        private System.Windows.Forms.Label lblMinMonitors;
        private System.Windows.Forms.NumericUpDown numMinMonitors;
        private System.Windows.Forms.Label lblInterval;
        private System.Windows.Forms.NumericUpDown numInterval;
        private System.Windows.Forms.CheckBox chkSkipMonitorCheck;
        private System.Windows.Forms.CheckBox chkEnforceWindowSize;
        private System.Windows.Forms.CheckBox chkAutoKillSocketBindings;
        private System.Windows.Forms.Button btnAdjustWindow;
        private System.Windows.Forms.Label lblDevices;
        private System.Windows.Forms.DataGridView dataGridDevices;
        private System.Windows.Forms.Button btnSave;

        private System.Windows.Forms.Label lblLog;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Label lblVersion;

        // Monitor visualization panel
        private System.Windows.Forms.Panel pnlMonitors;
        private System.Windows.Forms.Panel pnlMonitorViz;
        private System.Windows.Forms.Panel pnlMonitorLight;
        private System.Windows.Forms.Label lblMonitorStatus;
        private System.Windows.Forms.Label lblMonitorWarning;
        private System.Windows.Forms.Button btnDisplaySettings;
        private System.Windows.Forms.ToolTip toolTip;

        // Device status panel
        private System.Windows.Forms.Panel pnlDeviceStatus;
        private System.Windows.Forms.FlowLayoutPanel flowDeviceStatus;
        private System.Windows.Forms.Label lblLastChecked;

        // Keep for compatibility but not used visually
        private System.Windows.Forms.GroupBox grpConfig;
        private System.Windows.Forms.GroupBox grpDevices;

        // System tray
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip trayContextMenu;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            grpControl = new GroupBox();
            pnlMonitorLight = new Panel();
            lblLastChecked = new Label();
            btnToggleSettings = new Button();
            btnStart = new Button();
            lblMonitorStatus = new Label();
            btnStop = new Button();
            pnlServerLight = new Panel();
            lblStatus = new Label();
            lblServerStatus = new Label();
            flowDeviceStatus = new FlowLayoutPanel();
            pnlSettings = new Panel();
            lblLnkPath = new Label();
            txtLnkPath = new TextBox();
            btnBrowseLnk = new Button();
            btnRestoreDefaults = new Button();
            lblInterval = new Label();
            chkSkipMonitorCheck = new CheckBox();
            chkEnforceWindowSize = new CheckBox();
            chkAutoKillSocketBindings = new CheckBox();
            btnAdjustWindow = new Button();
            lblMinMonitors = new Label();
            numInterval = new NumericUpDown();
            lblDevices = new Label();
            dataGridDevices = new DataGridView();
            numMinMonitors = new NumericUpDown();
            btnSave = new Button();
            lblLog = new Label();
            txtLog = new RichTextBox();
            pnlMonitors = new Panel();
            pnlMonitorViz = new Panel();
            btnDisplaySettings = new Button();
            lblMonitorWarning = new Label();
            toolTip = new ToolTip(components);
            notifyIcon = new NotifyIcon(components);
            trayContextMenu = new ContextMenuStrip(components);
            pnlDeviceStatus = new Panel();
            grpConfig = new GroupBox();
            grpDevices = new GroupBox();
            lblVersion = new Label();
            grpControl.SuspendLayout();
            pnlSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridDevices).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMinMonitors).BeginInit();
            pnlMonitors.SuspendLayout();
            pnlMonitorViz.SuspendLayout();
            SuspendLayout();
            // 
            // grpControl
            // 
            grpControl.Controls.Add(pnlMonitorLight);
            grpControl.Controls.Add(lblLastChecked);
            grpControl.Controls.Add(btnToggleSettings);
            grpControl.Controls.Add(btnStart);
            grpControl.Controls.Add(lblMonitorStatus);
            grpControl.Controls.Add(btnStop);
            grpControl.Controls.Add(pnlServerLight);
            grpControl.Controls.Add(lblStatus);
            grpControl.Controls.Add(lblServerStatus);
            grpControl.Controls.Add(flowDeviceStatus);
            grpControl.ForeColor = Color.FromArgb(230, 230, 230);
            grpControl.Location = new Point(14, 14);
            grpControl.Name = "grpControl";
            grpControl.Size = new Size(750, 70);
            grpControl.TabIndex = 0;
            grpControl.TabStop = false;
            // 
            // pnlMonitorLight
            // 
            pnlMonitorLight.BackColor = Color.FromArgb(80, 80, 80);
            pnlMonitorLight.Location = new Point(301, 45);
            pnlMonitorLight.Name = "pnlMonitorLight";
            pnlMonitorLight.Size = new Size(14, 14);
            pnlMonitorLight.TabIndex = 0;
            // 
            // lblLastChecked
            // 
            lblLastChecked.Font = new Font("Segoe UI", 8F);
            lblLastChecked.ForeColor = Color.FromArgb(120, 120, 120);
            lblLastChecked.Location = new Point(601, 0);
            lblLastChecked.Name = "lblLastChecked";
            lblLastChecked.Size = new Size(134, 18);
            lblLastChecked.TabIndex = 1;
            lblLastChecked.Text = "Checking...";
            lblLastChecked.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnToggleSettings
            // 
            btnToggleSettings.BackColor = Color.FromArgb(60, 60, 60);
            btnToggleSettings.Cursor = Cursors.Hand;
            btnToggleSettings.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            btnToggleSettings.FlatStyle = FlatStyle.Flat;
            btnToggleSettings.Font = new Font("Segoe UI", 9F);
            btnToggleSettings.ForeColor = Color.FromArgb(200, 200, 200);
            btnToggleSettings.Location = new Point(619, 39);
            btnToggleSettings.Name = "btnToggleSettings";
            btnToggleSettings.Size = new Size(125, 24);
            btnToggleSettings.TabIndex = 3;
            btnToggleSettings.Text = "⚙ Settings";
            btnToggleSettings.UseVisualStyleBackColor = false;
            // 
            // btnStart
            // 
            btnStart.BackColor = Color.FromArgb(139, 0, 0);
            btnStart.Cursor = Cursors.Hand;
            btnStart.FlatAppearance.BorderColor = Color.FromArgb(178, 34, 34);
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnStart.ForeColor = Color.White;
            btnStart.Location = new Point(20, 20);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(160, 40);
            btnStart.TabIndex = 0;
            btnStart.Text = "▶ Launch Game";
            btnStart.UseVisualStyleBackColor = false;
            btnStart.Click += btnStart_Click;
            // 
            // lblMonitorStatus
            // 
            lblMonitorStatus.Font = new Font("Segoe UI", 9F);
            lblMonitorStatus.ForeColor = Color.FromArgb(200, 200, 200);
            lblMonitorStatus.Location = new Point(321, 43);
            lblMonitorStatus.Name = "lblMonitorStatus";
            lblMonitorStatus.Size = new Size(90, 20);
            lblMonitorStatus.TabIndex = 1;
            lblMonitorStatus.Text = "Monitors: ?/?";
            lblMonitorStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnStop
            // 
            btnStop.BackColor = Color.FromArgb(50, 50, 50);
            btnStop.Cursor = Cursors.Hand;
            btnStop.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btnStop.FlatStyle = FlatStyle.Flat;
            btnStop.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnStop.ForeColor = Color.FromArgb(230, 230, 230);
            btnStop.Location = new Point(190, 20);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(100, 40);
            btnStop.TabIndex = 1;
            btnStop.Text = "✕ Abort";
            btnStop.UseVisualStyleBackColor = false;
            btnStop.Click += btnStop_Click;
            // 
            // pnlServerLight
            // 
            pnlServerLight.BackColor = Color.FromArgb(80, 80, 80);
            pnlServerLight.Location = new Point(301, 22);
            pnlServerLight.Name = "pnlServerLight";
            pnlServerLight.Size = new Size(14, 14);
            pnlServerLight.TabIndex = 2;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(150, 150, 150);
            lblStatus.Location = new Point(440, 28);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(0, 21);
            lblStatus.TabIndex = 2;
            lblStatus.Visible = false;
            // 
            // lblServerStatus
            // 
            lblServerStatus.AutoSize = true;
            lblServerStatus.Font = new Font("Segoe UI", 9F);
            lblServerStatus.ForeColor = Color.FromArgb(180, 180, 180);
            lblServerStatus.Location = new Point(321, 22);
            lblServerStatus.Name = "lblServerStatus";
            lblServerStatus.Size = new Size(83, 15);
            lblServerStatus.TabIndex = 3;
            lblServerStatus.Text = "Batbox Servers";
            // 
            // flowDeviceStatus
            // 
            flowDeviceStatus.Location = new Point(401, 20);
            flowDeviceStatus.Name = "flowDeviceStatus";
            flowDeviceStatus.Size = new Size(210, 40);
            flowDeviceStatus.TabIndex = 0;
            // 
            // pnlSettings
            // 
            pnlSettings.BackColor = Color.FromArgb(35, 35, 35);
            pnlSettings.BorderStyle = BorderStyle.FixedSingle;
            pnlSettings.Controls.Add(lblLnkPath);
            pnlSettings.Controls.Add(txtLnkPath);
            pnlSettings.Controls.Add(btnBrowseLnk);
            pnlSettings.Controls.Add(btnRestoreDefaults);
            pnlSettings.Controls.Add(lblInterval);
            pnlSettings.Controls.Add(chkSkipMonitorCheck);
            pnlSettings.Controls.Add(chkEnforceWindowSize);
            pnlSettings.Controls.Add(chkAutoKillSocketBindings);
            pnlSettings.Controls.Add(lblMinMonitors);
            pnlSettings.Controls.Add(numInterval);
            pnlSettings.Controls.Add(lblDevices);
            pnlSettings.Controls.Add(dataGridDevices);
            pnlSettings.Controls.Add(numMinMonitors);
            pnlSettings.Controls.Add(btnSave);
            pnlSettings.Location = new Point(14, 202);
            pnlSettings.Name = "pnlSettings";
            pnlSettings.Size = new Size(750, 239);
            pnlSettings.TabIndex = 1;
            pnlSettings.Visible = false;
            // 
            // lblLnkPath
            // 
            lblLnkPath.AutoSize = true;
            lblLnkPath.ForeColor = Color.FromArgb(230, 230, 230);
            lblLnkPath.Location = new Point(14, 8);
            lblLnkPath.Name = "lblLnkPath";
            lblLnkPath.Size = new Size(143, 15);
            lblLnkPath.TabIndex = 0;
            lblLnkPath.Text = "Kiosk Shortcut Path (.lnk):";
            lblLnkPath.Click += lblLnkPath_Click;
            // 
            // txtLnkPath
            // 
            txtLnkPath.BackColor = Color.FromArgb(50, 50, 50);
            txtLnkPath.BorderStyle = BorderStyle.FixedSingle;
            txtLnkPath.ForeColor = Color.FromArgb(230, 230, 230);
            txtLnkPath.Location = new Point(14, 26);
            txtLnkPath.Name = "txtLnkPath";
            txtLnkPath.Size = new Size(237, 23);
            txtLnkPath.TabIndex = 1;
            // 
            // btnBrowseLnk
            // 
            btnBrowseLnk.BackColor = Color.FromArgb(50, 50, 50);
            btnBrowseLnk.Cursor = Cursors.Hand;
            btnBrowseLnk.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btnBrowseLnk.FlatStyle = FlatStyle.Flat;
            btnBrowseLnk.ForeColor = Color.FromArgb(230, 230, 230);
            btnBrowseLnk.Location = new Point(257, 26);
            btnBrowseLnk.Name = "btnBrowseLnk";
            btnBrowseLnk.Size = new Size(80, 25);
            btnBrowseLnk.TabIndex = 2;
            btnBrowseLnk.Text = "Browse...";
            btnBrowseLnk.UseVisualStyleBackColor = false;
            // 
            // btnRestoreDefaults
            // 
            btnRestoreDefaults.BackColor = Color.FromArgb(80, 60, 0);
            btnRestoreDefaults.Cursor = Cursors.Hand;
            btnRestoreDefaults.FlatAppearance.BorderColor = Color.FromArgb(120, 90, 0);
            btnRestoreDefaults.FlatStyle = FlatStyle.Flat;
            btnRestoreDefaults.ForeColor = Color.FromArgb(255, 200, 100);
            btnRestoreDefaults.Location = new Point(343, 26);
            btnRestoreDefaults.Name = "btnRestoreDefaults";
            btnRestoreDefaults.Size = new Size(105, 25);
            btnRestoreDefaults.TabIndex = 3;
            btnRestoreDefaults.Text = "↺ Defaults";
            btnRestoreDefaults.UseVisualStyleBackColor = false;
            btnRestoreDefaults.Visible = false;
            // 
            // lblInterval
            // 
            lblInterval.AutoSize = true;
            lblInterval.ForeColor = Color.FromArgb(230, 230, 230);
            lblInterval.Location = new Point(629, 10);
            lblInterval.Name = "lblInterval";
            lblInterval.Size = new Size(65, 15);
            lblInterval.TabIndex = 10;
            lblInterval.Text = "Retry (sec):";
            // 
            // chkSkipMonitorCheck
            // 
            chkSkipMonitorCheck.AutoSize = true;
            chkSkipMonitorCheck.ForeColor = Color.FromArgb(230, 230, 230);
            chkSkipMonitorCheck.Location = new Point(498, 38);
            chkSkipMonitorCheck.Name = "chkSkipMonitorCheck";
            chkSkipMonitorCheck.Size = new Size(184, 19);
            chkSkipMonitorCheck.TabIndex = 12;
            chkSkipMonitorCheck.Text = "Skip monitor check on launch";
            chkSkipMonitorCheck.UseVisualStyleBackColor = true;
            // 
            // chkEnforceWindowSize
            // 
            chkEnforceWindowSize.AutoSize = true;
            chkEnforceWindowSize.ForeColor = Color.FromArgb(230, 230, 230);
            chkEnforceWindowSize.Location = new Point(498, 63);
            chkEnforceWindowSize.Name = "chkEnforceWindowSize";
            chkEnforceWindowSize.Size = new Size(197, 19);
            chkEnforceWindowSize.TabIndex = 16;
            chkEnforceWindowSize.Text = "Enforce window size (3840x1080)";
            chkEnforceWindowSize.UseVisualStyleBackColor = true;
            // 
            // chkAutoKillSocketBindings
            // 
            chkAutoKillSocketBindings.AutoSize = true;
            chkAutoKillSocketBindings.ForeColor = Color.FromArgb(230, 230, 230);
            chkAutoKillSocketBindings.Location = new Point(498, 88);
            chkAutoKillSocketBindings.Name = "chkAutoKillSocketBindings";
            chkAutoKillSocketBindings.Size = new Size(209, 19);
            chkAutoKillSocketBindings.TabIndex = 18;
            chkAutoKillSocketBindings.Text = "Auto-kill offline device socket bindings";
            chkAutoKillSocketBindings.UseVisualStyleBackColor = true;
            // 
            // btnAdjustWindow
            // 
            btnAdjustWindow.BackColor = Color.FromArgb(60, 60, 100);
            btnAdjustWindow.Cursor = Cursors.Hand;
            btnAdjustWindow.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 140);
            btnAdjustWindow.FlatStyle = FlatStyle.Flat;
            btnAdjustWindow.Font = new Font("Segoe UI", 8F);
            btnAdjustWindow.ForeColor = Color.FromArgb(200, 200, 255);
            btnAdjustWindow.Location = new Point(602, 52);
            btnAdjustWindow.Name = "btnAdjustWindow";
            btnAdjustWindow.Size = new Size(115, 24);
            btnAdjustWindow.TabIndex = 17;
            btnAdjustWindow.Text = "⊞ Adjust Window";
            btnAdjustWindow.UseVisualStyleBackColor = false;
            btnAdjustWindow.Click += btnAdjustWindow_Click;
            // 
            // lblMinMonitors
            // 
            lblMinMonitors.AutoSize = true;
            lblMinMonitors.ForeColor = Color.FromArgb(230, 230, 230);
            lblMinMonitors.Location = new Point(498, 10);
            lblMinMonitors.Name = "lblMinMonitors";
            lblMinMonitors.Size = new Size(82, 15);
            lblMinMonitors.TabIndex = 8;
            lblMinMonitors.Text = "Min Monitors:";
            // 
            // numInterval
            // 
            numInterval.BackColor = Color.FromArgb(50, 50, 50);
            numInterval.ForeColor = Color.FromArgb(230, 230, 230);
            numInterval.Location = new Point(700, 8);
            numInterval.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
            numInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numInterval.Name = "numInterval";
            numInterval.Size = new Size(34, 23);
            numInterval.TabIndex = 11;
            numInterval.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // lblDevices
            // 
            lblDevices.AutoSize = true;
            lblDevices.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblDevices.ForeColor = Color.FromArgb(230, 230, 230);
            lblDevices.Location = new Point(14, 84);
            lblDevices.Name = "lblDevices";
            lblDevices.Size = new Size(51, 15);
            lblDevices.TabIndex = 13;
            lblDevices.Text = "Devices";
            // 
            // dataGridDevices
            // 
            dataGridDevices.AllowUserToAddRows = false;
            dataGridDevices.AllowUserToDeleteRows = false;
            dataGridDevices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridDevices.BackgroundColor = Color.FromArgb(45, 45, 45);
            dataGridDevices.BorderStyle = BorderStyle.None;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(30, 30, 30);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = Color.FromArgb(230, 230, 230);
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dataGridDevices.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dataGridDevices.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.FromArgb(45, 45, 45);
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = Color.FromArgb(230, 230, 230);
            dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(139, 0, 0);
            dataGridViewCellStyle2.SelectionForeColor = Color.White;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dataGridDevices.DefaultCellStyle = dataGridViewCellStyle2;
            dataGridDevices.EnableHeadersVisualStyles = false;
            dataGridDevices.GridColor = Color.FromArgb(60, 60, 60);
            dataGridDevices.Location = new Point(14, 104);
            dataGridDevices.Name = "dataGridDevices";
            dataGridDevices.Size = new Size(720, 75);
            dataGridDevices.TabIndex = 14;
            // 
            // numMinMonitors
            // 
            numMinMonitors.BackColor = Color.FromArgb(50, 50, 50);
            numMinMonitors.ForeColor = Color.FromArgb(230, 230, 230);
            numMinMonitors.Location = new Point(586, 8);
            numMinMonitors.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numMinMonitors.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMinMonitors.Name = "numMinMonitors";
            numMinMonitors.Size = new Size(37, 23);
            numMinMonitors.TabIndex = 9;
            numMinMonitors.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // btnSave
            // 
            btnSave.BackColor = Color.FromArgb(0, 100, 0);
            btnSave.Cursor = Cursors.Hand;
            btnSave.FlatAppearance.BorderColor = Color.FromArgb(0, 140, 0);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(300, 193);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(130, 30);
            btnSave.TabIndex = 15;
            btnSave.Text = "💾 Save Settings";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += btnSave_Click;
            // 
            // lblLog
            // 
            lblLog.AutoSize = true;
            lblLog.ForeColor = Color.FromArgb(230, 230, 230);
            lblLog.Location = new Point(14, 186);
            lblLog.Name = "lblLog";
            lblLog.Size = new Size(30, 15);
            lblLog.TabIndex = 6;
            lblLog.Text = "Log:";
            // 
            // txtLog
            // 
            txtLog.BackColor = Color.FromArgb(20, 20, 20);
            txtLog.BorderStyle = BorderStyle.None;
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.ForeColor = Color.FromArgb(200, 200, 200);
            txtLog.Location = new Point(14, 204);
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.Size = new Size(750, 287);
            txtLog.TabIndex = 7;
            txtLog.Text = "";
            // 
            // pnlMonitors
            // 
            pnlMonitors.BackColor = Color.FromArgb(30, 30, 30);
            pnlMonitors.BorderStyle = BorderStyle.FixedSingle;
            pnlMonitors.Controls.Add(pnlMonitorViz);
            pnlMonitors.Location = new Point(14, 90);
            pnlMonitors.Name = "pnlMonitors";
            pnlMonitors.Size = new Size(750, 91);
            pnlMonitors.TabIndex = 4;
            // 
            // pnlMonitorViz
            // 
            pnlMonitorViz.BackColor = Color.FromArgb(25, 25, 25);
            pnlMonitorViz.Controls.Add(btnDisplaySettings);
            pnlMonitorViz.Controls.Add(lblMonitorWarning);
            pnlMonitorViz.Controls.Add(btnAdjustWindow);
            pnlMonitorViz.Location = new Point(14, 3);
            pnlMonitorViz.Name = "pnlMonitorViz";
            pnlMonitorViz.Size = new Size(720, 83);
            pnlMonitorViz.TabIndex = 6;
            // 
            // btnDisplaySettings
            // 
            btnDisplaySettings.BackColor = Color.FromArgb(80, 60, 0);
            btnDisplaySettings.Cursor = Cursors.Hand;
            btnDisplaySettings.FlatAppearance.BorderColor = Color.FromArgb(120, 90, 0);
            btnDisplaySettings.FlatStyle = FlatStyle.Flat;
            btnDisplaySettings.Font = new Font("Segoe UI", 8F);
            btnDisplaySettings.ForeColor = Color.FromArgb(255, 200, 100);
            btnDisplaySettings.Location = new Point(3, 52);
            btnDisplaySettings.Name = "btnDisplaySettings";
            btnDisplaySettings.Size = new Size(125, 28);
            btnDisplaySettings.TabIndex = 5;
            btnDisplaySettings.Text = "Display Settings";
            btnDisplaySettings.UseVisualStyleBackColor = false;
            btnDisplaySettings.Visible = false;
            // 
            // lblMonitorWarning
            // 
            lblMonitorWarning.Font = new Font("Segoe UI", 8F);
            lblMonitorWarning.ForeColor = Color.FromArgb(255, 180, 100);
            lblMonitorWarning.Location = new Point(5, 4);
            lblMonitorWarning.Name = "lblMonitorWarning";
            lblMonitorWarning.Size = new Size(125, 20);
            lblMonitorWarning.TabIndex = 4;
            lblMonitorWarning.Text = "⚠ Primary not on left!";
            lblMonitorWarning.TextAlign = ContentAlignment.MiddleLeft;
            lblMonitorWarning.Visible = false;
            // 
            // notifyIcon
            // 
            notifyIcon.ContextMenuStrip = trayContextMenu;
            notifyIcon.Icon = (Icon)resources.GetObject("notifyIcon.Icon");
            notifyIcon.Text = "Batbox Launcher";
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            // 
            // trayContextMenu
            // 
            trayContextMenu.Name = "trayContextMenu";
            trayContextMenu.Size = new Size(61, 4);
            // 
            // pnlDeviceStatus
            // 
            pnlDeviceStatus.BackColor = Color.FromArgb(30, 30, 30);
            pnlDeviceStatus.BorderStyle = BorderStyle.FixedSingle;
            pnlDeviceStatus.Location = new Point(14, 166);
            pnlDeviceStatus.Name = "pnlDeviceStatus";
            pnlDeviceStatus.Size = new Size(750, 50);
            pnlDeviceStatus.TabIndex = 5;
            pnlDeviceStatus.Visible = false;
            // 
            // grpConfig
            // 
            grpConfig.Location = new Point(0, 0);
            grpConfig.Name = "grpConfig";
            grpConfig.Size = new Size(200, 100);
            grpConfig.TabIndex = 0;
            grpConfig.TabStop = false;
            // 
            // grpDevices
            // 
            grpDevices.Location = new Point(0, 0);
            grpDevices.Name = "grpDevices";
            grpDevices.Size = new Size(200, 100);
            grpDevices.TabIndex = 0;
            grpDevices.TabStop = false;
            // 
            // lblVersion
            // 
            lblVersion.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            lblVersion.AutoSize = true;
            lblVersion.Font = new Font("Segoe UI", 8F);
            lblVersion.ForeColor = Color.FromArgb(120, 120, 120);
            lblVersion.Location = new Point(700, 498);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(60, 13);
            lblVersion.TabIndex = 8;
            lblVersion.Text = "tristam 2.0";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(26, 26, 26);
            ClientSize = new Size(780, 518);
            Controls.Add(pnlSettings);
            Controls.Add(pnlMonitors);
            Controls.Add(lblLog);
            Controls.Add(txtLog);
            Controls.Add(lblVersion);
            Controls.Add(grpControl);
            ForeColor = Color.FromArgb(230, 230, 230);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Batbox Launcher";
            grpControl.ResumeLayout(false);
            grpControl.PerformLayout();
            pnlSettings.ResumeLayout(false);
            pnlSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridDevices).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMinMonitors).EndInit();
            pnlMonitors.ResumeLayout(false);
            pnlMonitorViz.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
