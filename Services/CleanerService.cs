using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MacStyleHub.Services
{
    public class ScanResult
    {
        public long UserTempSize { get; set; }
        public long SystemTempSize { get; set; }
        public long PrefetchSize { get; set; }
        public long RecycleBinSize { get; set; }
        public long TotalSize => UserTempSize + SystemTempSize + PrefetchSize + RecycleBinSize;

        public string FormattedUserTemp => FormatBytes(UserTempSize);
        public string FormattedSystemTemp => FormatBytes(SystemTempSize);
        public string FormattedPrefetch => FormatBytes(PrefetchSize);
        public string FormattedRecycleBin => FormatBytes(RecycleBinSize);
        public string FormattedTotal => FormatBytes(TotalSize);

        private string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }
            return $"{dblSByte:F1} {suffix[i]}";
        }
    }

    public class CleanerService
    {
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryBInfo);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI = 0x00000002;
        private const uint SHERB_NOSOUND = 0x00000004;

        public async Task<ScanResult> ScanAsync()
        {
            return await Task.Run(() =>
            {
                var result = new ScanResult();

                // 1. User Temp
                try
                {
                    string userTemp = Path.GetTempPath();
                    result.UserTempSize = GetDirectorySize(userTemp);
                }
                catch { }

                // 2. System Temp
                try
                {
                    string systemTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
                    if (Directory.Exists(systemTemp))
                    {
                        result.SystemTempSize = GetDirectorySize(systemTemp);
                    }
                }
                catch { }

                // 3. Prefetch
                try
                {
                    string prefetch = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                    if (Directory.Exists(prefetch))
                    {
                        result.PrefetchSize = GetDirectorySize(prefetch);
                    }
                }
                catch { }

                // 4. Recycle Bin (Manual user-specific scan for precise size reporting)
                try
                {
                    string? userSid = System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value;
                    if (!string.IsNullOrEmpty(userSid))
                    {
                        long rbSize = 0;
                        foreach (var drive in DriveInfo.GetDrives())
                        {
                            if (!drive.IsReady) continue;
                            try
                            {
                                string rbPath = Path.Combine(drive.Name, "$Recycle.Bin", userSid);
                                if (Directory.Exists(rbPath))
                                {
                                    rbSize += GetDirectorySize(rbPath);
                                }
                            }
                            catch { }
                        }
                        result.RecycleBinSize = rbSize;
                    }
                }
                catch { }

                return result;
            });
        }

        public async Task CleanAsync()
        {
            await Task.Run(() =>
            {
                // 1. Clean User Temp
                try
                {
                    string userTemp = Path.GetTempPath();
                    CleanDirectoryContents(userTemp);
                }
                catch { }

                // 2. Clean System Temp
                try
                {
                    string systemTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
                    if (Directory.Exists(systemTemp))
                    {
                        CleanDirectoryContents(systemTemp);
                    }
                }
                catch { }

                // 3. Clean Prefetch
                try
                {
                    string prefetch = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                    if (Directory.Exists(prefetch))
                    {
                        CleanDirectoryContents(prefetch);
                    }
                }
                catch { }

                // 4. Empty Recycle Bin
                try
                {
                    SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                }
                catch { }
            });
        }

        private long GetDirectorySize(string path)
        {
            long size = 0;
            try
            {
                var dir = new DirectoryInfo(path);
                foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += file.Length;
                    }
                    catch { } // Ignore locked files
                }
            }
            catch { }
            return size;
        }

        private void CleanDirectoryContents(string path)
        {
            try
            {
                var dir = new DirectoryInfo(path);
                foreach (var file in dir.EnumerateFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch { } // Skip open/locked files
                }
                foreach (var subDir in dir.EnumerateDirectories())
                {
                    try
                    {
                        subDir.Delete(true);
                    }
                    catch { } // Skip locked folders
                }
            }
            catch { }
        }
    }
}
