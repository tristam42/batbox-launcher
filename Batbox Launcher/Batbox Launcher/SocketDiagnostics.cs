using System.Diagnostics;

namespace BatboxLauncher
{
    internal static class SocketDiagnostics
    {
        internal sealed class SocketBindingInfo
        {
            public string Protocol { get; set; } = "";
            public string LocalAddress { get; set; } = "";
            public string ForeignAddress { get; set; } = "";
            public int LocalPort { get; set; }
            public int Pid { get; set; }
            public string ProcessName { get; set; } = "Unknown";
        }

        public static List<SocketBindingInfo> GetBindingsForIp(string ip)
        {
            var rows = RunNetstat();
            var matches = rows
                .Where(r => AddressContainsIp(r.LocalAddress, ip) || AddressContainsIp(r.ForeignAddress, ip))
                .ToList();

            foreach (var row in matches)
            {
                row.ProcessName = GetProcessName(row.Pid);
            }

            return matches
                .OrderBy(r => r.ProcessName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Pid)
                .ThenBy(r => r.LocalPort)
                .ToList();
        }

        private static List<SocketBindingInfo> RunNetstat()
        {
            var result = new List<SocketBindingInfo>();
            var psi = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return result;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.StartsWith("Proto", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;

                if (!TryParseRow(parts, out var row)) continue;
                result.Add(row);
            }

            return result;
        }

        private static bool TryParseRow(string[] parts, out SocketBindingInfo row)
        {
            row = new SocketBindingInfo();
            string protocol = parts[0];

            // TCP row shape: Proto Local Foreign State PID
            // UDP row shape: Proto Local Foreign PID
            if (protocol.Equals("TCP", StringComparison.OrdinalIgnoreCase))
            {
                if (parts.Length < 5) return false;
                if (!int.TryParse(parts[4], out int pid)) return false;

                row = new SocketBindingInfo
                {
                    Protocol = protocol,
                    LocalAddress = parts[1],
                    ForeignAddress = parts[2],
                    LocalPort = ParsePort(parts[1]),
                    Pid = pid
                };
                return true;
            }

            if (protocol.Equals("UDP", StringComparison.OrdinalIgnoreCase))
            {
                if (parts.Length < 4) return false;
                if (!int.TryParse(parts[3], out int pid)) return false;

                row = new SocketBindingInfo
                {
                    Protocol = protocol,
                    LocalAddress = parts[1],
                    ForeignAddress = parts[2],
                    LocalPort = ParsePort(parts[1]),
                    Pid = pid
                };
                return true;
            }

            return false;
        }

        private static int ParsePort(string endpoint)
        {
            int lastColon = endpoint.LastIndexOf(':');
            if (lastColon < 0 || lastColon == endpoint.Length - 1) return -1;
            string portText = endpoint[(lastColon + 1)..];
            return int.TryParse(portText, out int port) ? port : -1;
        }

        private static bool AddressContainsIp(string endpoint, string ip)
        {
            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(ip))
                return false;

            return endpoint.Contains(ip, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetProcessName(int pid)
        {
            try
            {
                return Process.GetProcessById(pid).ProcessName;
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
