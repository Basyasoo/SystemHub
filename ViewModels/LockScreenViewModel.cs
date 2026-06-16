using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemHub.Services;

namespace SystemHub.ViewModels
{
    public partial class LockScreenViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainViewModel;

        [ObservableProperty]
        private string _passwordInput = "";

        [ObservableProperty]
        private string _errorMessage = "";

        public LockScreenViewModel(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        [RelayCommand]
        public void Unlock()
        {
            if (AppLockService.Instance.VerifyPassword(PasswordInput))
            {
                ErrorMessage = "";
                PasswordInput = "";
                _mainViewModel.IsAppLocked = false;
                _mainViewModel.CurrentPageViewModel = _mainViewModel.DashboardVM;
            }
            else
            {
                ErrorMessage = LocalizationService.Instance.AppLockInvalidPassword;
                PasswordInput = "";
            }
        }
    }
}

