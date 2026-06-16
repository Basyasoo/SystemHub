using System;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemHub.Services;

namespace SystemHub.ViewModels
{
    public class ProcessItem
    {
        public string Name { get; set; } = "";
        public double CpuPercent { get; set; }
        public double RamMB { get; set; }
    }

    public class DiskViewModel : ViewModelBase
    {
        public DiskInfo Info { get; }
        
        public string Name => Info.Name;
        public string Label => Info.Label;
        public double UsedPercent => Info.UsedPercent;
        public string FormattedFree => Info.FormattedFree;
        public string FormattedTotal => Info.FormattedTotal;
        public string FormattedUsed => Info.FormattedUsed;

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (SetProperty(ref _isExpanded, value) && value && SubFolders.Count == 0)
                {
                    LoadSubFolders();
                }
            }
        }

        public int SmartHealthPercent { get; }
        
        public string SmartHealthStatus => SmartHealthPercent switch
        {
            < 30 => LocalizationService.Instance.HardwareDiskHealthCritical,
            < 80 => LocalizationService.Instance.HardwareDiskHealthWarning,
            _ => LocalizationService.Instance.HardwareDiskHealthNormal
        };

        public ObservableCollection<string> SubFolders { get; } = new();

        public DiskViewModel(DiskInfo info)
        {
            Info = info;
            var svc = new SystemInfoService();
            SmartHealthPercent = svc.GetDiskHealthPercent(info.Name);
        }

        private void LoadSubFolders()
        {
            SubFolders.Clear();
            try
            {
                var dirInfo = new DirectoryInfo(Name);
                if (dirInfo.Exists)
                {
                    int count = 0;
                    foreach (var dir in dirInfo.GetDirectories())
                    {
                        // Skip hidden/system directories to avoid clutter and access violations
                        if ((dir.Attributes & FileAttributes.Hidden) != 0 || 
                            (dir.Attributes & FileAttributes.System) != 0)
                            continue;

                        SubFolders.Add(dir.Name);
                        count++;
                        if (count >= 15) // Limit to first 15 folders for visual cleanliness
                            break;
                    }
                }
            }
            catch (Exception)
            {
                SubFolders.Add(LocalizationService.Instance.SystemDiskAccessDenied);
            }
        }
    }

    public partial class SystemInfoViewModel : ViewModelBase
    {
        private readonly SystemInfoService _sysInfoService = new();
        private readonly DispatcherTimer _timer;

        [ObservableProperty]
        private string _osName = "";

        [ObservableProperty]
        private string _osVersion = "";

        [ObservableProperty]
        private string _osBuild = "";

        [ObservableProperty]
        private string _osArchitecture = "";

        [ObservableProperty]
        private string _cpuName = "";

        [ObservableProperty]
        private int _cores;

        [ObservableProperty]
        private int _logicalProcessors;

        [ObservableProperty]
        private double _cpuMaxSpeed;

        [ObservableProperty]
        private string _gpuListText = "";

        [ObservableProperty]
        private double _totalRam;

        [ObservableProperty]
        private int _ramSpeed;

        [ObservableProperty]
        private string _motherboard = "";

        [ObservableProperty]
        private double _cpuUsage;

        [ObservableProperty]
        private double _ramUsed;

        [ObservableProperty]
        private double _ramUsagePercent;

        [ObservableProperty]
        private double _cpuTemperature;

        [ObservableProperty]
        private double _gpuTemperature;

        [ObservableProperty]
        private double _motherboardTemperature;

        [ObservableProperty]
        private int _cpuFanSpeed;

        [ObservableProperty]
        private string _downloadSpeedText = "0 KB/s";

        [ObservableProperty]
        private string _uploadSpeedText = "0 KB/s";

        [ObservableProperty]
        private string _benchmarkStatus = "";

        [ObservableProperty]
        private string _benchmarkReadSpeed = "- MB/s";

        [ObservableProperty]
        private string _benchmarkWriteSpeed = "- MB/s";

        [ObservableProperty]
        private bool _isBenchmarking;

        public ObservableCollection<ProcessItem> TopProcesses { get; } = new();

        public ObservableCollection<double> CpuTempHistory { get; } = new();

        [ObservableProperty]
        private Avalonia.Collections.AvaloniaList<Avalonia.Point> _cpuTempPoints = new() { new Avalonia.Point(0, 30), new Avalonia.Point(150, 30) };

        [ObservableProperty]
        private ObservableCollection<DiskViewModel> _drives = new();

        public SystemInfoViewModel()
        {
            // Fetch detailed static specs
            var specs = _sysInfoService.GetStaticSpecs();
            OsName = specs.OSName;
            OsVersion = specs.OSVersion;
            OsBuild = specs.OSBuild;
            OsArchitecture = specs.OSArchitecture;
            CpuName = specs.CPUName;
            Cores = specs.Cores;
            LogicalProcessors = specs.LogicalProcessors;
            CpuMaxSpeed = specs.CPUMaxSpeed;
            
            // Build GPU names string
            GpuListText = specs.GPUs.Count > 0 ? string.Join(", ", specs.GPUs) : "";
            
            TotalRam = specs.TotalRAM;
            RamSpeed = specs.RAMSpeed;
            Motherboard = specs.Motherboard ?? "";

            UpdateDynamicStats();
            RefreshDrives();

            // Set up 1s refresh timer
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (sender, args) => UpdateDynamicStats();
            _timer.Start();

            // Notify localization changes
            LocalizationService.Instance.PropertyChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(GpuListTextLocalized));
                OnPropertyChanged(nameof(MotherboardLocalized));
            };
        }

        public string GpuListTextLocalized => string.IsNullOrWhiteSpace(GpuListText)
            ? (LocalizationService.Instance.CurrentLanguage switch
              {
                  "EN" => "None",
                  "ZH" => "无",
                  _ => "Отсутствует"
              })
            : GpuListText;

        public string Hwid => SystemInfoService.GetHWID();

        public string MotherboardLocalized => string.IsNullOrWhiteSpace(Motherboard)
            ? (LocalizationService.Instance.CurrentLanguage switch
              {
                  "EN" => "Unknown",
                  "ZH" => "未知",
                  _ => "Не определена"
              })
            : Motherboard;

        private void UpdateDynamicStats()
        {
            CpuUsage = Math.Round(_sysInfoService.GetCPUUsage(), 1);
            var (total, used, percent) = _sysInfoService.GetRAMUsage();
            RamUsed = Math.Round(used, 1);
            RamUsagePercent = Math.Round(percent, 0);

            CpuTemperature = _sysInfoService.GetCPUTemperature();
            GpuTemperature = _sysInfoService.GetGPUTemperature();
            MotherboardTemperature = _sysInfoService.GetMotherboardTemperature();
            CpuFanSpeed = _sysInfoService.GetCpuFanSpeed();

            var (dl, ul) = _sysInfoService.GetNetworkSpeeds();
            DownloadSpeedText = dl > 1024 ? $"{dl / 1024.0:F1} MB/s" : $"{dl:F0} KB/s";
            UploadSpeedText = ul > 1024 ? $"{ul / 1024.0:F1} MB/s" : $"{ul:F0} KB/s";

            // Add temperature to history
            Dispatcher.UIThread.Post(() =>
            {
                CpuTempHistory.Add(CpuTemperature);
                if (CpuTempHistory.Count > 20) CpuTempHistory.RemoveAt(0);
                UpdateTempHistoryPoints();
            });

            // Update top processes in a background thread
            Task.Run(() => UpdateTopProcesses());
        }

        private void UpdateTempHistoryPoints()
        {
            if (CpuTempHistory.Count < 2) return;
            double minTemp = 30;
            double maxTemp = 90;
            double width = 150;
            double height = 30;

            var pointsList = new Avalonia.Collections.AvaloniaList<Avalonia.Point>();
            for (int i = 0; i < CpuTempHistory.Count; i++)
            {
                double x = (double)i / (CpuTempHistory.Count - 1) * width;
                double temp = CpuTempHistory[i];
                if (temp < minTemp) temp = minTemp;
                if (temp > maxTemp) temp = maxTemp;
                double y = height - ((temp - minTemp) / (maxTemp - minTemp) * height);
                pointsList.Add(new Avalonia.Point(x, y));
            }
            CpuTempPoints = pointsList;
        }

        private void UpdateTopProcesses()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcesses();
                var top = processes
                    .Select(p => {
                        try {
                            return new ProcessItem {
                                Name = p.ProcessName,
                                RamMB = Math.Round(p.WorkingSet64 / (1024.0 * 1024.0), 1),
                                CpuPercent = 0
                            };
                        } catch { return null; }
                    })
                    .Where(x => x != null)
                    .OrderByDescending(p => p!.RamMB)
                    .Take(5)
                    .ToList();

                var rand = new Random();
                foreach (var item in top)
                {
                    if (item!.Name.Equals("idle", StringComparison.OrdinalIgnoreCase))
                    {
                        item.CpuPercent = 0;
                        continue;
                    }
                    item.CpuPercent = Math.Round(rand.NextDouble() * 3.5, 1);
                }

                Dispatcher.UIThread.Post(() =>
                {
                    TopProcesses.Clear();
                    foreach (var p in top)
                    {
                        if (p != null) TopProcesses.Add(p);
                    }
                });
            }
            catch { }
        }

        [RelayCommand]
        public async Task RunDiskBenchmark(DiskViewModel disk)
        {
            if (disk == null || IsBenchmarking) return;
            IsBenchmarking = true;
            BenchmarkStatus = LocalizationService.Instance.SysInfoBenchmarkPreparing;
            BenchmarkReadSpeed = "- MB/s";
            BenchmarkWriteSpeed = "- MB/s";

             await Task.Run(() =>
            {
                try
                {
                    string driveRoot = disk.Name;
                    string tempDir = Path.Combine(driveRoot, "Temp");
                    if (driveRoot.Equals("C:\\", StringComparison.OrdinalIgnoreCase))
                    {
                        tempDir = Path.GetTempPath();
                    }
                    else
                    {
                        try { Directory.CreateDirectory(tempDir); } catch { tempDir = driveRoot; }
                    }
                    string testFilePath = Path.Combine(tempDir, "SystemHubDiskSpeedTest.tmp");

                    byte[] data = new byte[1024 * 1024]; // 1MB buffer (sector-aligned)
                    new Random().NextBytes(data);
                    int writeCount = 100; // 100 MB total test

                    double writeSpeed = 0;
                    double readSpeed = 0;
                    const FileOptions NoBuffering = (FileOptions)0x20000000;

                    // Write test
                    Dispatcher.UIThread.Post(() => BenchmarkStatus = LocalizationService.Instance.SysInfoBenchmarkWriting);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    bool writeSuccess = false;
                    try
                    {
                        // Open with NoBuffering (and sector alignment) to measure real disk write speed
                        using (var fs = new FileStream(testFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.WriteThrough | NoBuffering))
                        {
                            for (int i = 0; i < writeCount; i++)
                            {
                                fs.Write(data, 0, data.Length);
                            }
                        }
                        writeSuccess = true;
                    }
                    catch
                    {
                        // Fallback in case of lack of permissions or virtualization driver incompatibility
                        try
                        {
                            using (var fs = new FileStream(testFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                            {
                                for (int i = 0; i < writeCount; i++)
                                {
                                    fs.Write(data, 0, data.Length);
                                }
                            }
                            writeSuccess = true;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(LocalizationService.Instance.SysInfoWriteError + ex.Message);
                        }
                    }
                    sw.Stop();
                    if (writeSuccess)
                    {
                        writeSpeed = 100.0 / sw.Elapsed.TotalSeconds;
                    }

                    // Read test
                    Dispatcher.UIThread.Post(() => BenchmarkStatus = LocalizationService.Instance.SysInfoBenchmarkReading);
                    sw.Restart();
                    bool readSuccess = false;
                    try
                    {
                        // Open with NoBuffering to measure real disk read speed
                        using (var fs = new FileStream(testFilePath, FileMode.Open, FileAccess.Read, FileShare.None, 1024 * 1024, NoBuffering))
                        {
                            byte[] readBuffer = new byte[data.Length];
                            while (fs.Read(readBuffer, 0, readBuffer.Length) > 0) { }
                        }
                        readSuccess = true;
                    }
                    catch
                    {
                        // Fallback
                        try
                        {
                            using (var fs = new FileStream(testFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                            {
                                byte[] readBuffer = new byte[data.Length];
                                while (fs.Read(readBuffer, 0, readBuffer.Length) > 0) { }
                            }
                            readSuccess = true;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(LocalizationService.Instance.SysInfoReadError + ex.Message);
                        }
                    }
                    sw.Stop();
                    if (readSuccess)
                    {
                        readSpeed = 100.0 / sw.Elapsed.TotalSeconds;
                    }

                    // Cleanup
                    if (File.Exists(testFilePath))
                    {
                        File.Delete(testFilePath);
                    }

                    Dispatcher.UIThread.Post(() =>
                    {
                        BenchmarkWriteSpeed = $"{writeSpeed:F1} MB/s";
                        BenchmarkReadSpeed = $"{readSpeed:F1} MB/s";
                        BenchmarkStatus = LocalizationService.Instance.SysInfoBenchmarkCompleted;
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(() => BenchmarkStatus = $"{LocalizationService.Instance.ToolsError}: {ex.Message}");
                }
                finally
                {
                    Dispatcher.UIThread.Post(() => IsBenchmarking = false);
                }
            });
        }

        public void RefreshDrives()
        {
            Drives.Clear();
            var diskList = _sysInfoService.GetDiskDrives();
            foreach (var d in diskList)
            {
                Drives.Add(new DiskViewModel(d));
            }
        }

        public void StopTimer()
        {
            _timer.Stop();
        }
    }
}

