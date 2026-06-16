using System.Text.Json;

namespace BatboxLauncher
{
    public class DeviceConfig
    {
        public string Name { get; set; } = "";
        public string Ip { get; set; } = "";
        public bool Skip { get; set; } = false;
    }

    public class AppConfig
    {
        public string LnkPath { get; set; } = @"C:\Users\Batbox\Desktop\Kiosk.lnk";
        public string KioskExeName { get; set; } = "Kiosk.exe";
        public string BaseballExeName { get; set; } = "Baseball.exe";
        public int MinMonitors { get; set; } = 2;
        public int IntervalSeconds { get; set; } = 10;

        public bool SkipMonitorCheck { get; set; } = false;
        public bool AutoKillCameraSocketBindings { get; set; } = false;

        // Window size enforcement settings
        public bool EnforceWindowSize { get; set; } = false;
        public string TargetWindowTitle { get; set; } = "Baseball";
        public int TargetWindowX { get; set; } = 0;
        public int TargetWindowY { get; set; } = 0;
        public int TargetWindowWidth { get; set; } = 3840;
        public int TargetWindowHeight { get; set; } = 1080;

        public List<DeviceConfig> Devices { get; set; } = new()
        {
            new DeviceConfig { Name = "Pitching Machine", Ip = "192.168.0.150", Skip = false },
            new DeviceConfig { Name = "LeftCam", Ip = "10.10.1.1", Skip = false },
            new DeviceConfig { Name = "RightCam", Ip = "10.11.1.1", Skip = false },
        };

        public static string ConfigPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BatboxLauncher", "config.json");

        private static readonly Dictionary<string, string> LegacyDeviceIpMigrations = new()
        {
            ["10.10.10.5"] = "10.10.1.1",
            ["10.11.1.5"] = "10.11.1.1",
        };

        private bool MigrateLegacyDeviceIps()
        {
            bool changed = false;
            foreach (var device in Devices)
            {
                if (LegacyDeviceIpMigrations.TryGetValue(device.Ip, out var newIp))
                {
                    device.Ip = newIp;
                    changed = true;
                }
            }

            return changed;
        }

        public static AppConfig LoadOrDefault()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return new AppConfig();
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                if (config.MigrateLegacyDeviceIps())
                    config.Save();
                return config;
            }
            catch
            {
                return new AppConfig();
            }
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(ConfigPath)!;
            Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
    }
}
