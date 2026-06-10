using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacStyleHub.Services;

namespace MacStyleHub.ViewModels
{
    public partial class AboutViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _updateStatus = "";

        [ObservableProperty]
        private bool _isChecking;

        [ObservableProperty]
        private string _downloadUrl = "";

        [ObservableProperty]
        private bool _updateAvailable;

        public string AppVersion
        {
            get
            {
                var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return assemblyVersion != null ? $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}" : "0.0.1";
            }
        }

        public string Hwid => SystemInfoService.GetHWID();

        public AboutViewModel()
        {
            ResetStatus();
            // Listen to language changes to update localized initial text if checked/not checked
            LocalizationService.Instance.PropertyChanged += (s, e) =>
            {
                if (!IsChecking && string.IsNullOrEmpty(DownloadUrl) && !UpdateAvailable)
                {
                    ResetStatus();
                }
            };
        }

        private void ResetStatus()
        {
            UpdateStatus = LocalizationService.Instance.CurrentLanguage switch
            {
                "EN" => "Updates not checked",
                "ZH" => "未检查更新",
                _ => "Обновления не проверялись"
            };
        }

        [RelayCommand]
        public async Task CheckForUpdatesAsync()
        {
            if (IsChecking) return;

            IsChecking = true;
            UpdateAvailable = false;
            DownloadUrl = "";

            UpdateStatus = LocalizationService.Instance.CurrentLanguage switch
            {
                "EN" => "Checking for updates...",
                "ZH" => "正在检查更新...",
                _ => "Проверка обновлений..."
            };

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "MacStyleHub-App");
                    var response = await client.GetAsync("https://api.github.com/repos/Basyasoo/SystemHub/releases/latest");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using (var doc = JsonDocument.Parse(json))
                        {
                            var root = doc.RootElement;
                            if (root.TryGetProperty("tag_name", out var tagProp))
                            {
                                var latestVersion = tagProp.GetString()?.Trim().ToLower() ?? "";
                                var currentVersion = $"v{AppVersion}";

                                if (latestVersion != currentVersion && !string.IsNullOrEmpty(latestVersion))
                                {
                                    UpdateAvailable = true;
                                    if (root.TryGetProperty("html_url", out var urlProp))
                                    {
                                        DownloadUrl = urlProp.GetString() ?? "";
                                    }

                                    UpdateStatus = LocalizationService.Instance.CurrentLanguage switch
                                    {
                                        "EN" => $"New version available: {latestVersion}!",
                                        "ZH" => $"发现新版本: {latestVersion}!",
                                        _ => $"Доступна новая версия: {latestVersion}!"
                                    };
                                }
                                else
                                {
                                    UpdateStatus = LocalizationService.Instance.CurrentLanguage switch
                                    {
                                        "EN" => $"You are running the latest version (v{AppVersion}).",
                                        "ZH" => $"您已安装最新版本 (v{AppVersion})。",
                                        _ => $"У вас установлена последняя версия (v{AppVersion})."
                                    };
                                }
                            }
                        }
                    }
                    else
                    {
                        UpdateStatus = LocalizationService.Instance.CurrentLanguage switch
                        {
                            "EN" => "No releases found on GitHub.",
                            "ZH" => "未在 GitHub 上找到发布版本。",
                            _ => "Релизы не найдены на GitHub."
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Update check error: " + ex.Message);
                UpdateStatus = LocalizationService.Instance.CurrentLanguage switch
                {
                    "EN" => "Failed to check updates.",
                    "ZH" => "检查更新失败。",
                    _ => "Не удалось проверить обновления."
                };
            }
            finally
            {
                IsChecking = false;
            }
        }

        [RelayCommand]
        public void DownloadUpdate()
        {
            if (string.IsNullOrEmpty(DownloadUrl)) return;
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = DownloadUrl,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch { }
        }
    }
}
