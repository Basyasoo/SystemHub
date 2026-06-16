using System;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SystemHub.Services
{
    public class User
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string AvatarPath { get; set; } = "";
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }

    public class UserSettings
    {
        public bool ShowUsernameInGreeting { get; set; } = true;
        public bool RememberMe { get; set; } = true;
        public string LastLoggedUser { get; set; } = "";
        public string SupabaseAccessToken { get; set; } = "";
        public string SupabaseRefreshToken { get; set; } = "";
    }

    public class UserService
    {
        private static UserService? _instance;
        public static UserService Instance => _instance ??= new UserService();

        private User? _currentUser;
        private UserSettings _settings = new();

        public User? CurrentUser => _currentUser;
        public UserSettings Settings => _settings;

        public static string LastDebugMessage { get; set; } = "";

        private UserService()
        {
            LoadSettings();
        }

        public bool HasUsers => true; // Always allow Login/Register switch since it's a shared Supabase DB

        public async Task<(bool Success, bool RequiresVerification, string Error)> StartRegistrationAsync(string username, string email, string password)
        {
            var (session, err) = await SupabaseService.Instance.SignUpAsync(email.Trim(), password, username.Trim());
            if (session != null)
            {
                // If access token is returned immediately, it means email confirmation is disabled on Supabase
                if (!string.IsNullOrEmpty(session.AccessToken))
                {
                    _settings.SupabaseAccessToken = session.AccessToken;
                    _settings.SupabaseRefreshToken = session.RefreshToken;
                    _settings.RememberMe = true;
                    _settings.LastLoggedUser = email.Trim();
                    SaveSettings();

                    if (session.User != null)
                    {
                        _currentUser = new User
                        {
                            Username = session.User.Username,
                            Email = session.User.Email,
                            AvatarPath = session.User.AvatarPath,
                            RegistrationDate = session.User.RegistrationDate
                        };
                        // Create profile table row
                        await SupabaseService.Instance.CreateOrUpdateProfileAsync(session.AccessToken, session.User.Id, session.User.Username, session.User.Email);
                    }
                    return (true, false, "");
                }
                return (true, true, "");
            }
            return (false, false, err);
        }

        public async Task<(bool Success, string Error)> CompleteRegistrationAsync(string email, string code)
        {
            var (session, err) = await SupabaseService.Instance.VerifyOtpAsync(email.Trim(), code.Trim(), "signup");
            if (session != null)
            {
                _settings.SupabaseAccessToken = session.AccessToken;
                _settings.SupabaseRefreshToken = session.RefreshToken;
                _settings.RememberMe = true;
                _settings.LastLoggedUser = email.Trim();
                SaveSettings();

                if (session.User != null)
                {
                    _currentUser = new User
                    {
                        Username = session.User.Username,
                        Email = session.User.Email,
                        AvatarPath = session.User.AvatarPath,
                        RegistrationDate = session.User.RegistrationDate
                    };
                    // Create/Update profile
                    await SupabaseService.Instance.CreateOrUpdateProfileAsync(session.AccessToken, session.User.Id, session.User.Username, session.User.Email);
                }
                return (true, "");
            }
            return (false, err);
        }

        public async Task<(bool Success, string Error)> StartPasswordRecoveryAsync(string email)
        {
            var (success, err) = await SupabaseService.Instance.RecoverPasswordAsync(email.Trim());
            return (success, err);
        }

        public async Task<(bool Success, string Error)> CompletePasswordRecoveryAsync(string email, string code, string newPassword)
        {
            // Step 1: verify recovery OTP to establish a session
            var (session, err) = await SupabaseService.Instance.VerifyOtpAsync(email.Trim(), code.Trim(), "recovery");
            if (session == null)
            {
                return (false, err);
            }

            // Step 2: update password using the session's access token
            var (user, updateErr) = await SupabaseService.Instance.UpdateUserAsync(session.AccessToken, newPassword, null, null);
            if (user != null)
            {
                _settings.SupabaseAccessToken = session.AccessToken;
                _settings.SupabaseRefreshToken = session.RefreshToken;
                _settings.RememberMe = true;
                _settings.LastLoggedUser = email.Trim();
                SaveSettings();

                _currentUser = new User
                {
                    Username = user.Username,
                    Email = user.Email,
                    AvatarPath = user.AvatarPath,
                    RegistrationDate = user.RegistrationDate
                };
                return (true, "");
            }

            return (false, updateErr);
        }

        public async Task<(bool Success, string ResolvedEmail, string Error)> LoginAsync(string identifier, string password)
        {
            string email = identifier.Trim();
            if (!email.Contains("@"))
            {
                // Resolve username to email
                var (resolvedEmail, resolveErr) = await SupabaseService.Instance.GetEmailByUsernameAsync(email);
                if (string.IsNullOrEmpty(resolvedEmail))
                {
                    return (false, "", string.IsNullOrEmpty(resolveErr) ? "Пользователь с таким именем не найден" : resolveErr);
                }
                email = resolvedEmail;
            }

            var (session, err) = await SupabaseService.Instance.SignInAsync(email, password);
            if (session != null)
            {
                _settings.SupabaseAccessToken = session.AccessToken;
                _settings.SupabaseRefreshToken = session.RefreshToken;
                if (_settings.RememberMe)
                {
                    _settings.LastLoggedUser = identifier.Trim();
                }
                SaveSettings();

                if (session.User != null)
                {
                    _currentUser = new User
                    {
                        Username = session.User.Username,
                        Email = session.User.Email,
                        AvatarPath = session.User.AvatarPath,
                        RegistrationDate = session.User.RegistrationDate
                    };
                }
                return (true, email, "");
            }
            return (false, email, err);
        }

        public async Task<bool> AutoLoginAsync()
        {
            if (string.IsNullOrEmpty(_settings.SupabaseAccessToken))
            {
                return false;
            }

            // Try to authenticate with the existing access token
            var (user, err) = await SupabaseService.Instance.GetUserAsync(_settings.SupabaseAccessToken);
            if (user != null)
            {
                _currentUser = new User
                {
                    Username = user.Username,
                    Email = user.Email,
                    AvatarPath = user.AvatarPath,
                    RegistrationDate = user.RegistrationDate
                };
                return true;
            }

            // If token expired, try to refresh
            if (!string.IsNullOrEmpty(_settings.SupabaseRefreshToken))
            {
                var (session, refreshErr) = await SupabaseService.Instance.RefreshTokenAsync(_settings.SupabaseRefreshToken);
                if (session != null)
                {
                    _settings.SupabaseAccessToken = session.AccessToken;
                    _settings.SupabaseRefreshToken = session.RefreshToken;
                    SaveSettings();

                    if (session.User != null)
                    {
                        _currentUser = new User
                        {
                            Username = session.User.Username,
                            Email = session.User.Email,
                            AvatarPath = session.User.AvatarPath,
                            RegistrationDate = session.User.RegistrationDate
                        };
                    }
                    return true;
                }
            }

            // Clear invalid session
            Logout();
            return false;
        }

        public void Logout()
        {
            _currentUser = null;
            _settings.SupabaseAccessToken = "";
            _settings.SupabaseRefreshToken = "";
            _settings.LastLoggedUser = "";
            SaveSettings();
        }

        public async Task<(bool Success, string Error)> UpdateUserAsync(string newUsername, string newAvatarPath)
        {
            if (string.IsNullOrEmpty(_settings.SupabaseAccessToken))
            {
                return (false, "Сессия истекла. Пожалуйста, войдите снова.");
            }

            var (user, err) = await SupabaseService.Instance.UpdateUserAsync(_settings.SupabaseAccessToken, null, newUsername.Trim(), newAvatarPath);
            if (user != null)
            {
                if (_currentUser != null)
                {
                    _currentUser.Username = user.Username;
                    _currentUser.AvatarPath = user.AvatarPath;
                }
                return (true, "");
            }
            return (false, err);
        }

        public async Task<(bool Success, string Error)> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            if (string.IsNullOrEmpty(_settings.SupabaseAccessToken))
            {
                return (false, "Сессия истекла. Пожалуйста, войдите снова.");
            }

            // To change password, first verify current password by logging in again
            if (_currentUser != null)
            {
                var (session, loginErr) = await SupabaseService.Instance.SignInAsync(_currentUser.Email, oldPassword);
                if (session == null)
                {
                    return (false, "Неверный текущий пароль");
                }
            }

            // Update user password
            var (user, err) = await SupabaseService.Instance.UpdateUserAsync(_settings.SupabaseAccessToken, newPassword, null, null);
            if (user != null)
            {
                return (true, "");
            }
            return (false, err);
        }

        public void SaveSettings()
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);
                string configPath = Path.Combine(appData, "users_settings.json");
                string json = JsonSerializer.Serialize(_settings);
                File.WriteAllText(configPath, json);
            }
            catch { }
        }

        private void LoadSettings()
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                string configPath = Path.Combine(appData, "users_settings.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var settings = JsonSerializer.Deserialize<UserSettings>(json);
                    if (settings != null)
                    {
                        _settings = settings;
                    }
                }
            }
            catch { }
        }
    }
}

