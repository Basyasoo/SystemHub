using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemHub.Services;

namespace SystemHub.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ViewModelBase _currentPageViewModel;

        [ObservableProperty]
        private int _selectedMenuIndex;

        [ObservableProperty]
        private bool _isSidebarCollapsed;

        [ObservableProperty]
        private double _sidebarWidth = 230;

        [ObservableProperty]
        private bool _isAppLoading = true;

        [RelayCommand]
        public void ToggleSidebar()
        {
            IsSidebarCollapsed = !IsSidebarCollapsed;
            SidebarWidth = IsSidebarCollapsed ? 110 : 230;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowMainLayout))]
        private bool _isAppLocked;

        [ObservableProperty]
        private LockScreenViewModel _lockScreenVM;

        public DashboardViewModel DashboardVM { get; } = new();
        public WeatherViewModel WeatherVM { get; } = new();
        public SystemInfoViewModel SystemInfoVM { get; } = new();
        public CleanerViewModel CleanerVM { get; } = new();
        public StartupViewModel StartupVM { get; } = new();
        public InstallerViewModel InstallerVM { get; } = new();
        public AboutViewModel AboutVM { get; } = new();
        public TweaksViewModel TweaksVM { get; } = new();
        public ToolsViewModel ToolsVM { get; } = new();
        
        // Windows current music playback VM
        public MediaPlaybackViewModel PlaybackVM { get; } = new();

        public string Hwid => SystemInfoService.GetHWID();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowMainLayout))]
        private bool _isAuthRequired;

        public bool ShowMainLayout => !IsAppLocked && !IsAuthRequired;

        [ObservableProperty]
        private AuthViewModel _authVM;

        [ObservableProperty]
        private bool _isWelcomeOverlayVisible = true;

        [ObservableProperty]
        private double _welcomeOverlayOpacity = 1.0;

        [ObservableProperty]
        private double _welcomeTextOpacity = 0.0;

        [ObservableProperty]
        private double _welcomeTextYOffset = 25.0;

        [ObservableProperty]
        private bool _animateWelcomeText;

        [ObservableProperty]
        private string _welcomeGreeting = "";

        [ObservableProperty]
        private string _welcomeSubText = "";

        // User profile cabinet page VM
        public ProfileViewModel ProfileVM { get; }

        public MainWindowViewModel()
        {
            _lockScreenVM = new LockScreenViewModel(this);
            IsAppLocked = AppLockService.Instance.IsLocked;

            ProfileVM = new ProfileViewModel(this);
            _authVM = new AuthViewModel { SuccessCallback = OnAuthSuccess };

            _currentPageViewModel = DashboardVM;
            _selectedMenuIndex = 0;

            _ = InitializeStartupSession();
            _ = StartLoadingTimer();
        }

        private async System.Threading.Tasks.Task InitializeStartupSession()
        {
            _ = AboutVM.CheckAndPerformSilentUpdateAsync();
            _ = InstallerService.Instance.AutoUpdateZapretAsync();

            bool isAuthed = await UserService.Instance.AutoLoginAsync();
            if (isAuthed)
            {
                IsAuthRequired = false;
                ProfileVM.Refresh();
                _ = RunWelcomeAnimation();
            }
            else
            {
                IsAuthRequired = true;
                IsWelcomeOverlayVisible = false;
            }
        }

        private void OnAuthSuccess()
        {
            IsAuthRequired = false;
            ProfileVM.Refresh();
            DashboardVM.UpdateClock();
            _ = RunWelcomeAnimation();
        }

        public async System.Threading.Tasks.Task RunWelcomeAnimation()
        {
            var user = UserService.Instance.CurrentUser;
            if (user == null) return;

            int hour = DateTime.Now.Hour;
            string greetStr = hour switch
            {
                >= 5 and < 12 => LocalizationService.Instance.MainGreetingMorning,
                >= 12 and < 18 => LocalizationService.Instance.MainGreetingAfternoon,
                >= 18 and < 23 => LocalizationService.Instance.MainGreetingEvening,
                _ => LocalizationService.Instance.MainGreetingNight
            };

            if (UserService.Instance.Settings.ShowUsernameInGreeting)
            {
                WelcomeGreeting = $"{greetStr}, {user.Username}!";
            }
            else
            {
                WelcomeGreeting = $"{greetStr}!";
            }

            WelcomeSubText = LocalizationService.Instance.MainWelcomeSubtext;

            WelcomeOverlayOpacity = 1.0;
            AnimateWelcomeText = false;
            IsWelcomeOverlayVisible = true;

            // Wait a short delay for layout and bindings to register the initial state
            await System.Threading.Tasks.Task.Delay(50);

            AnimateWelcomeText = true;

            // Keep visible for the display duration
            await System.Threading.Tasks.Task.Delay(2000);

            // Fade out the overlay
            WelcomeOverlayOpacity = 0.0;

            // Wait for the overlay fade out transition to complete
            await System.Threading.Tasks.Task.Delay(600);

            IsWelcomeOverlayVisible = false;
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
                    InstallerVM.RescanInstalled();
                    break;
                case 7:
                    CurrentPageViewModel = TweaksVM;
                    TweaksVM.LoadCurrentStates();
                    break;
                case 8:
                    CurrentPageViewModel = ToolsVM;
                    break;
                case 9:
                    CurrentPageViewModel = AboutVM;
                    break;
                case 10:
                    ProfileVM.Refresh();
                    CurrentPageViewModel = ProfileVM;
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

        [RelayCommand]
        public void NavigateToProfile()
        {
            SelectedMenuIndex = -1;
            ProfileVM.Refresh();
            CurrentPageViewModel = ProfileVM;
        }
    }
}

