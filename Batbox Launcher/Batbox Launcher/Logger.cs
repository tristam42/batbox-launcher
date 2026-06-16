namespace BatboxLauncher
{
    public class Logger
    {
        private readonly string _logDir;
        private readonly Action<string, string> _uiAppend; // (message, level)
        private readonly long _maxSizeBytes;
        private string _logPath;

        public Logger(Action<string, string> uiAppend, long maxSizeMB = 3)
        {
            _logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BatboxLauncher", "Logs");
            Directory.CreateDirectory(_logDir);

            _maxSizeBytes = maxSizeMB * 1024 * 1024;
            _logPath = GetLogPath();
            _uiAppend = uiAppend;
        }

        private string GetLogPath() => Path.Combine(_logDir, $"launcher_{DateTime.Now:yyyyMMdd}.log");

        public void Info(string msg) => Write("INFO", msg);
        public void Warn(string msg) => Write("WARN", msg);
        public void Error(string msg) => Write("ERROR", msg);
        
        /// <summary>
        /// Log to file only, not shown in UI (for internal monitoring messages)
        /// </summary>
        public void Debug(string msg) => WriteFileOnly("DEBUG", msg);

        private void Write(string level, string msg)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {msg}";
            
            try
            {
                // Update log path in case date changed
                _logPath = GetLogPath();

                // Check if log file exceeds max size
                if (File.Exists(_logPath))
                {
                    var fileInfo = new FileInfo(_logPath);
                    if (fileInfo.Length >= _maxSizeBytes)
                    {
                        RotateLog();
                    }
                }

                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch { }

            _uiAppend(line, level);
        }

        private void WriteFileOnly(string level, string msg)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {msg}";
            
            try
            {
                _logPath = GetLogPath();
                if (File.Exists(_logPath))
                {
                    var fileInfo = new FileInfo(_logPath);
                    if (fileInfo.Length >= _maxSizeBytes)
                    {
                        RotateLog();
                    }
                }
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch { }
            // Note: No _uiAppend call - file only
        }

        private void RotateLog()
        {
            try
            {
                // Find next available backup number
                int backupNum = 1;
                string backupPath;
                do
                {
                    backupPath = Path.Combine(_logDir, $"launcher_{DateTime.Now:yyyyMMdd}.{backupNum}.log");
                    backupNum++;
                } while (File.Exists(backupPath) && backupNum < 100);

                // Rename current log to backup
                if (backupNum < 100)
                {
                    File.Move(_logPath, backupPath);
                }
                else
                {
                    // Too many backups, just overwrite the main log
                    File.Delete(_logPath);
                }

                // Clean up old backup files (keep only last 3)
                CleanupOldLogs();
            }
            catch { }
        }

        private void CleanupOldLogs()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logDir, "launcher_*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Skip(4) // Keep current + 3 backups
                    .ToList();

                foreach (var file in logFiles)
                {
                    try { file.Delete(); } catch { }
                }
            }
            catch { }
        }
    }
}
