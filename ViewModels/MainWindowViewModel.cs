using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacStyleHub.Services;

namespace MacStyleHub.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ViewModelBase _currentPageViewModel;

        [ObservableProperty]
        private int _selectedMenuIndex;

        [ObservableProperty]
        private bool _isAppLoading = true;

        public DashboardViewModel DashboardVM { get; } = new();
        public WeatherViewModel WeatherVM { get; } = new();
        public SystemInfoViewModel SystemInfoVM { get; } = new();
        public CleanerViewModel CleanerVM { get; } = new();
        public StartupViewModel StartupVM { get; } = new();
        public InstallerViewModel InstallerVM { get; } = new();
        public AboutViewModel AboutVM { get; } = new();
        
        // Windows current music playback VM
        public MediaPlaybackViewModel PlaybackVM { get; } = new();

        public string Hwid => SystemInfoService.GetHWID();

        public MainWindowViewModel()
        {
            _currentPageViewModel = DashboardVM;
            _selectedMenuIndex = 0;
            _ = StartLoadingTimer();
        }

        private async System.Threading.Tasks.Task StartLoadingTimer()
        {
            await System.Threading.Tasks.Task.Delay(1800);
            IsAppLoading = false;
        }

        partial void OnSelectedMenuIndexChanged(int value)
        {
            switch (value)
            {
                case 0:
                    CurrentPageViewModel = DashboardVM;
                    break;
                case 1:
                    CurrentPageViewModel = WeatherVM;
                    break;
                case 2:
                    CurrentPageViewModel = SystemInfoVM;
                    break;
                case 3:
                    CurrentPageViewModel = CleanerVM;
                    break;
                case 4:
                    CurrentPageViewModel = StartupVM;
                    break;
                case 5:
                    CurrentPageViewModel = PlaybackVM;
                    break;
                case 6:
                    CurrentPageViewModel = InstallerVM;
                    break;
                case 7:
                    CurrentPageViewModel = AboutVM;
                    break;
            }
        }

        [RelayCommand]
        public void NavigateTo(string indexStr)
        {
            if (int.TryParse(indexStr, out int index))
            {
                SelectedMenuIndex = index;
            }
        }
    }
}
