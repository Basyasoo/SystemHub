using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemHub.Services;

namespace SystemHub.ViewModels
{
    public partial class CleanerViewModel : ViewModelBase
    {
        private readonly CleanerService _cleanerService = new();

        [ObservableProperty]
        private string _userTempSize = "0 B";

        [ObservableProperty]
        private string _systemTempSize = "0 B";

        [ObservableProperty]
        private string _prefetchSize = "0 B";

        [ObservableProperty]
        private string _recycleBinSize = "0 B";

        [ObservableProperty]
        private string _browserCacheSize = "0 B";

        [ObservableProperty]
        private string _totalSize = "0 B";

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private bool _isCleaning;

        [ObservableProperty]
        private bool _scanCompleted;

        [ObservableProperty]
        private bool _cleanCompleted;

        public CleanerViewModel()
        {
            ScanCommand = new AsyncRelayCommand(ScanAsync);
            CleanCommand = new AsyncRelayCommand(CleanAsync);
        }

        public IAsyncRelayCommand ScanCommand { get; }
        public IAsyncRelayCommand CleanCommand { get; }

        private async Task ScanAsync()
        {
            IsScanning = true;
            ScanCompleted = false;
            CleanCompleted = false;

            try
            {
                // Simulate slight delay for beautiful scanning animation
                await Task.Delay(1200);
                var result = await _cleanerService.ScanAsync();

                UserTempSize = result.FormattedUserTemp;
                SystemTempSize = result.FormattedSystemTemp;
                PrefetchSize = result.FormattedPrefetch;
                RecycleBinSize = result.FormattedRecycleBin;
                BrowserCacheSize = result.FormattedBrowserCache;
                TotalSize = result.FormattedTotal;
                ScanCompleted = true;
            }
            finally
            {
                IsScanning = false;
            }
        }

        private async Task CleanAsync()
        {
            IsCleaning = true;
            CleanCompleted = false;

            try
            {
                // Simulate cleaning animation delay
                await Task.Delay(1500);
                await _cleanerService.CleanAsync();

                // Re-scan to update sizes
                var result = await _cleanerService.ScanAsync();
                UserTempSize = result.FormattedUserTemp;
                SystemTempSize = result.FormattedSystemTemp;
                PrefetchSize = result.FormattedPrefetch;
                RecycleBinSize = result.FormattedRecycleBin;
                BrowserCacheSize = result.FormattedBrowserCache;
                TotalSize = result.FormattedTotal;

                CleanCompleted = true;
                ScanCompleted = false;
            }
            finally
            {
                IsCleaning = false;
            }
        }
    }
}

