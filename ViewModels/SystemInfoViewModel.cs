using System;
using System.IO;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MacStyleHub.Services;

namespace MacStyleHub.ViewModels
{
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

        public ObservableCollection<string> SubFolders { get; } = new();

        public DiskViewModel(DiskInfo info)
        {
            Info = info;
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
