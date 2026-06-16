using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemHub.Services;

namespace SystemHub.ViewModels
{
    public enum AuthMode
    {
        Login,
        Register,
        VerifyEmail,
        ForgotPassword,
        ResetPassword
    }

    public partial class AuthViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _username = "";

        [ObservableProperty]
        private string _email = "";

        [ObservableProperty]
        private string _password = "";

        [ObservableProperty]
        private string _verificationCode = "";

        [ObservableProperty]
        private string _newPassword = "";

        [ObservableProperty]
        private string _confirmPassword = "";

        [ObservableProperty]
        private string _errorMessage = "";

        [ObservableProperty]
        private string _debugCodeMessage = "";

        [ObservableProperty]
        private AuthMode _currentMode = AuthMode.Login;

        [ObservableProperty]
        private bool _rememberMe;

        [ObservableProperty]
        private bool _revealPassword;

        public char PasswordCharChar => RevealPassword ? '\0' : '●';

        public Action? SuccessCallback { get; set; }

        public bool IsLoginMode => CurrentMode == AuthMode.Login;
        public bool IsRegisterMode => CurrentMode == AuthMode.Register;
        public bool IsVerifyEmailMode => CurrentMode == AuthMode.VerifyEmail;
        public bool IsForgotPasswordMode => CurrentMode == AuthMode.ForgotPassword;
        public bool IsResetPasswordMode => CurrentMode == AuthMode.ResetPassword;
        public bool IsLoginOrRegisterMode => CurrentMode == AuthMode.Login || CurrentMode == AuthMode.Register;
        public bool IsBackVisible => CurrentMode != AuthMode.Login && CurrentMode != AuthMode.Register;

        public AuthViewModel()
        {
            RememberMe = UserService.Instance.Settings.RememberMe;
            
            if (!UserService.Instance.HasUsers)
            {
                CurrentMode = AuthMode.Register;
            }
        }

        partial void OnRevealPasswordChanged(bool value)
        {
            OnPropertyChanged(nameof(PasswordCharChar));
        }

        partial void OnCurrentModeChanged(AuthMode value)
        {
            OnPropertyChanged(nameof(IsLoginMode));
            OnPropertyChanged(nameof(IsRegisterMode));
            OnPropertyChanged(nameof(IsVerifyEmailMode));
            OnPropertyChanged(nameof(IsForgotPasswordMode));
            OnPropertyChanged(nameof(IsResetPasswordMode));
            OnPropertyChanged(nameof(IsLoginOrRegisterMode));
            OnPropertyChanged(nameof(IsBackVisible));
            
            ErrorMessage = "";
            DebugCodeMessage = "";
            VerificationCode = "";
            Password = "";
            NewPassword = "";
            ConfirmPassword = "";
        }

        [RelayCommand]
        public void ToggleMode()
        {
            if (CurrentMode == AuthMode.Register && !UserService.Instance.HasUsers)
            {
                ErrorMessage = "Сначала необходимо зарегистрировать пользователя";
                return;
            }

            CurrentMode = CurrentMode == AuthMode.Login ? AuthMode.Register : AuthMode.Login;
        }

        [RelayCommand]
        public void GotoForgotPassword()
        {
            CurrentMode = AuthMode.ForgotPassword;
        }

        [RelayCommand]
        public void GotoLogin()
        {
            if (!UserService.Instance.HasUsers)
            {
                CurrentMode = AuthMode.Register;
            }
            else
            {
                CurrentMode = AuthMode.Login;
            }
        }

        partial void OnRememberMeChanged(bool value)
        {
            UserService.Instance.Settings.RememberMe = value;
            UserService.Instance.SaveSettings();
        }

        [RelayCommand]
        public async System.Threading.Tasks.Task PerformAuth()
        {
            ErrorMessage = "";
            DebugCodeMessage = "";

            if (CurrentMode == AuthMode.Login)
            {
                if (string.IsNullOrWhiteSpace(Email))
                {
                    ErrorMessage = "Введите имя пользователя или Email";
                    return;
                }
                if (string.IsNullOrWhiteSpace(Password))
                {
                    ErrorMessage = "Введите пароль";
                    return;
                }

                var (success, resolvedEmail, err) = await UserService.Instance.LoginAsync(Email, Password);
                if (success)
                {
                    Password = "";
                    SuccessCallback?.Invoke();
                }
                else
                {
                    ErrorMessage = err;
                    if (err.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase) || 
                        err.Contains("подтвержден", StringComparison.OrdinalIgnoreCase) ||
                        err.Contains("confirmation", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(resolvedEmail))
                        {
                            Email = resolvedEmail;
                        }
                        CurrentMode = AuthMode.VerifyEmail;
                    }
                }
            }
            else if (CurrentMode == AuthMode.Register)
            {
                if (string.IsNullOrWhiteSpace(Username))
                {
                    ErrorMessage = "Введите имя пользователя";
                    return;
                }
                if (string.IsNullOrWhiteSpace(Email))
                {
                    ErrorMessage = "Введите почту";
                    return;
                }
                if (!Email.Contains("@") || !Email.Contains("."))
                {
                    ErrorMessage = "Пожалуйста, введите корректный Email.";
                    return;
                }
                if (string.IsNullOrWhiteSpace(Password))
                {
                    ErrorMessage = "Введите пароль";
                    return;
                }

                var (success, requiresVerification, err) = await UserService.Instance.StartRegistrationAsync(Username, Email, Password);
                if (success)
                {
                    if (requiresVerification)
                    {
                        CurrentMode = AuthMode.VerifyEmail;
                        if (!string.IsNullOrEmpty(UserService.LastDebugMessage))
                        {
                            DebugCodeMessage = UserService.LastDebugMessage;
                        }
                    }
                    else
                    {
                        Password = "";
                        SuccessCallback?.Invoke();
                    }
                }
                else
                {
                    ErrorMessage = err;
                }
            }
        }

        [RelayCommand]
        public async System.Threading.Tasks.Task VerifyCode()
        {
            ErrorMessage = "";
            if (string.IsNullOrWhiteSpace(VerificationCode))
            {
                ErrorMessage = "Введите код подтверждения";
                return;
            }

            var (success, err) = await UserService.Instance.CompleteRegistrationAsync(Email, VerificationCode);
            if (success)
            {
                CurrentMode = AuthMode.Login;
                SuccessCallback?.Invoke();
            }
            else
            {
                ErrorMessage = err;
            }
        }

        [RelayCommand]
        public async System.Threading.Tasks.Task SendForgotPasswordCode()
        {
            ErrorMessage = "";
            DebugCodeMessage = "";
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Введите Email";
                return;
            }

            var (success, err) = await UserService.Instance.StartPasswordRecoveryAsync(Email);
            if (success)
            {
                CurrentMode = AuthMode.ResetPassword;
                if (!string.IsNullOrEmpty(UserService.LastDebugMessage))
                {
                    DebugCodeMessage = UserService.LastDebugMessage;
                }
            }
            else
            {
                ErrorMessage = err;
            }
        }

        [RelayCommand]
        public async System.Threading.Tasks.Task ResetPasswordAction()
        {
            ErrorMessage = "";
            if (string.IsNullOrWhiteSpace(VerificationCode))
            {
                ErrorMessage = "Введите код восстановления";
                return;
            }
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ErrorMessage = "Введите новый пароль";
                return;
            }
            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "Пароли не совпадают";
                return;
            }

            var (success, err) = await UserService.Instance.CompletePasswordRecoveryAsync(Email, VerificationCode, NewPassword);
            if (success)
            {
                CurrentMode = AuthMode.Login;
                SuccessCallback?.Invoke();
            }
            else
            {
                ErrorMessage = err;
            }
        }
    }
}

