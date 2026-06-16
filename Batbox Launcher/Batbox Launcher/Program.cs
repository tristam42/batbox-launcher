namespace BatboxLauncher
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Global exception handlers
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show($"An error occurred:\n\n{e.Exception.Message}\n\nDetails logged to AppData.",
                    "Batbox Launcher Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogException(e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show($"A critical error occurred:\n\n{ex?.Message}\n\nThe application will close.",
                    "Batbox Launcher Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (ex != null) LogException(ex);
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new Form1());
        }

        private static void LogException(Exception ex)
        {
            try
            {
                var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BatboxLauncher", "Logs");
                Directory.CreateDirectory(logDir);
                var logPath = Path.Combine(logDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                File.WriteAllText(logPath, $"{DateTime.Now}\n{ex}\n\nStack Trace:\n{ex.StackTrace}");
            }
            catch { }
        }
    }
}