using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemHub.Services;

namespace SystemHub.ViewModels
{
    public partial class ProfileViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowVM;

        [ObservableProperty]
        private string _username = "";

        [ObservableProperty]
        private string _avatarPath = "";

        [ObservableProperty]
        private Avalonia.Media.Imaging.Bitmap? _avatarBitmap;

        partial void OnAvatarPathChanged(string value)
        {
            if (AvatarBitmap != null)
            {
                AvatarBitmap.Dispose();
                AvatarBitmap = null;
            }

            if (!string.IsNullOrEmpty(value) && File.Exists(value))
            {
                try
                {
                    AvatarBitmap = new Avalonia.Media.Imaging.Bitmap(value);
                }
                catch
                {
                    AvatarBitmap = null;
                }
            }
        }

        [ObservableProperty]
        private string _registrationDate = "";

        [ObservableProperty]
        private string _oldPassword = "";

        [ObservableProperty]
        private string _newPassword = "";

        [ObservableProperty]
        private string _confirmPassword = "";

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private string _statusColor = "#30D158";

        [ObservableProperty]
        private bool _showUsernameInGreeting;

        [ObservableProperty]
        private bool _rememberMe;

        [ObservableProperty]
        private bool _revealOldPassword;

        [ObservableProperty]
        private bool _revealNewPassword;

        [ObservableProperty]
        private bool _revealConfirmPassword;

        public char PasswordCharOld => RevealOldPassword ? '\0' : '●';
        public char PasswordCharNew => RevealNewPassword ? '\0' : '●';
        public char PasswordCharConfirm => RevealConfirmPassword ? '\0' : '●';

        partial void OnRevealOldPasswordChanged(bool value) => OnPropertyChanged(nameof(PasswordCharOld));
        partial void OnRevealNewPasswordChanged(bool value) => OnPropertyChanged(nameof(PasswordCharNew));
        partial void OnRevealConfirmPasswordChanged(bool value) => OnPropertyChanged(nameof(PasswordCharConfirm));

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Username)) return "?";
                string name = Username.Trim();
                if (name.Length >= 2) return name.Substring(0, 2).ToUpper();
                return name.Substring(0, 1).ToUpper();
            }
        }

        public ProfileViewModel(MainWindowViewModel mainWindowVM)
        {
            _mainWindowVM = mainWindowVM;
            Refresh();
        }

        public void Refresh()
        {
            var user = UserService.Instance.CurrentUser;
            if (user != null)
            {
                Username = user.Username;
                AvatarPath = user.AvatarPath;
                RegistrationDate = user.RegistrationDate.ToString("dd.MM.yyyy HH:mm");
            }
            ShowUsernameInGreeting = UserService.Instance.Settings.ShowUsernameInGreeting;
            RememberMe = UserService.Instance.Settings.RememberMe;
            StatusMessage = "";
            OldPassword = "";
            NewPassword = "";
            ConfirmPassword = "";
            OnPropertyChanged(nameof(Initials));
        }

        partial void OnShowUsernameInGreetingChanged(bool value)
        {
            UserService.Instance.Settings.ShowUsernameInGreeting = value;
            UserService.Instance.SaveSettings();
            _mainWindowVM.DashboardVM.UpdateClock();
        }

        partial void OnRememberMeChanged(bool value)
        {
            UserService.Instance.Settings.RememberMe = value;
            UserService.Instance.SaveSettings();
        }

        [RelayCommand]
        public async Task SelectAvatar()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = LocalizationService.Instance.ProfileSelectAvatarTitle,
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType(LocalizationService.Instance.ProfileImagesFilter)
                        {
                            Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp" }
                        }
                    }
                };

                var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(options);
                if (files.Count > 0)
                {
                    string originalPath = files[0].Path.LocalPath;
                    var croppedPath = await SystemHub.Views.AvatarCropWindow.ShowCropWindowAsync(desktop.MainWindow, originalPath);
                    if (!string.IsNullOrEmpty(croppedPath))
                    {
                        AvatarPath = croppedPath;
                        await SaveProfile();
                    }
                }
            }
        }

        [RelayCommand]
        public async Task ClearAvatar()
        {
            AvatarPath = "";
            await SaveProfile();
        }

        [RelayCommand]
        public async System.Threading.Tasks.Task SaveProfile()
        {
            StatusMessage = "";
            var (success, error) = await UserService.Instance.UpdateUserAsync(Username, AvatarPath);
            if (!success)
            {
                StatusMessage = error;
                StatusColor = "#FF453A";
            }
            else
            {
                StatusMessage = LocalizationService.Instance.ProfileSavedSuccess;
                StatusColor = "#30D158";
                OnPropertyChanged(nameof(Initials));
                _mainWindowVM.DashboardVM.UpdateClock();
            }
        }

        [RelayCommand]
        public async System.Threading.Tasks.Task ChangePassword()
        {
            StatusMessage = "";
            if (string.IsNullOrWhiteSpace(OldPassword) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                StatusMessage = LocalizationService.Instance.ProfileFillAllFields;
                StatusColor = "#FF453A";
                return;
            }
            if (NewPassword != ConfirmPassword)
            {
                StatusMessage = LocalizationService.Instance.ProfilePasswordsMismatch;
                StatusColor = "#FF453A";
                return;
            }

            var (success, error) = await UserService.Instance.ChangePasswordAsync(OldPassword, NewPassword);
            if (success)
            {
                StatusMessage = LocalizationService.Instance.ProfilePasswordChangedSuccess;
                StatusColor = "#30D158";
                OldPassword = "";
                NewPassword = "";
                ConfirmPassword = "";
            }
            else
            {
                StatusMessage = error;
                StatusColor = "#FF453A";
            }
        }

        [RelayCommand]
        public void Logout()
        {
            UserService.Instance.Logout();
            _mainWindowVM.IsAuthRequired = true;
            _mainWindowVM.SelectedMenuIndex = 0;
            // Clear inputs in AuthViewModel
            _mainWindowVM.AuthVM.Username = "";
            _mainWindowVM.AuthVM.Password = "";
            _mainWindowVM.AuthVM.ErrorMessage = "";
            _mainWindowVM.AuthVM.CurrentMode = UserService.Instance.HasUsers ? AuthMode.Login : AuthMode.Register;
        }
    }
}

