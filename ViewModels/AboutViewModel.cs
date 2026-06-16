using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemHub.Services;

namespace SystemHub.ViewModels
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
                    client.DefaultRequestHeaders.Add("User-Agent", "SystemHub-App");
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
                                    string directUrl = "";
                                    if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var asset in assetsProp.EnumerateArray())
                                        {
                                            if (asset.TryGetProperty("name", out var nameProp) && 
                                                asset.TryGetProperty("browser_download_url", out var downloadProp))
                                            {
                                                var name = nameProp.GetString();
                                                if (name != null && name.Equals("SystemHubSetup.exe", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    directUrl = downloadProp.GetString() ?? "";
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(directUrl))
                                    {
                                        DownloadUrl = directUrl;
                                    }
                                    else
                                    {
                                        // Fallback to direct raw download from the Git repository tag since we commit it there
                                        DownloadUrl = $"https://github.com/Basyasoo/SystemHub/raw/{latestVersion}/SystemHubSetup.exe";
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

        [ObservableProperty]
        private double _downloadProgress;

        [ObservableProperty]
        private bool _isDownloading;

        [ObservableProperty]
        private string _downloadStatus = "";

        [RelayCommand]
        public async Task DownloadUpdateAsync()
        {
            if (string.IsNullOrEmpty(DownloadUrl)) return;

            // If it is not a direct installer exe link, fallback to opening in browser
            if (!DownloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
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
                return;
            }

            if (IsDownloading) return;

            IsDownloading = true;
            DownloadProgress = 0;
            DownloadStatus = LocalizationService.Instance.CurrentLanguage switch
            {
                "EN" => "Preparing download...",
                "ZH" => "准备下载...",
                _ => "Подготовка к скачиванию..."
            };

            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "SystemHubSetup.exe");
                
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "SystemHub-App");
                    using (var response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            long totalRead = 0;
                            int bytesRead;

                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalRead += bytesRead;

                                if (totalBytes > 0)
                                {
                                    DownloadProgress = (double)totalRead * 100 / totalBytes;
                                    DownloadStatus = LocalizationService.Instance.CurrentLanguage switch
                                    {
                                        "EN" => $"Downloading: {DownloadProgress:F0}% ({totalRead / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB)",
                                        "ZH" => $"正在下载: {DownloadProgress:F0}% ({totalRead / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB)",
                                        _ => $"Скачивание: {DownloadProgress:F0}% ({totalRead / 1024 / 1024}МБ / {totalBytes / 1024 / 1024}МБ)"
                                    };
                                }
                                else
                                {
                                    DownloadStatus = LocalizationService.Instance.CurrentLanguage switch
                                    {
                                        "EN" => $"Downloading... ({totalRead / 1024 / 1024}MB)",
                                        "ZH" => $"正在下载... ({totalRead / 1024 / 1024}MB)",
                                        _ => $"Скачивание... ({totalRead / 1024 / 1024}МБ)"
                                    };
                                }
                            }
                        }
                    }
                }

                DownloadStatus = LocalizationService.Instance.CurrentLanguage switch
                {
                    "EN" => "Download complete. Starting installer...",
                    "ZH" => "下载完成，启动安装...",
                    _ => "Скачивание завершено. Запуск установки..."
                };

                await Task.Delay(1000);

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true,
                    Verb = "runas" // Request administrator privileges explicitly
                };
                System.Diagnostics.Process.Start(psi);
                
                // Close the application to allow the installer to replace the files
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Download failed: " + ex.Message);
                DownloadStatus = LocalizationService.Instance.CurrentLanguage switch
                {
                    "EN" => "Download failed: " + ex.Message,
                    "ZH" => "下载失败: " + ex.Message,
                    _ => "Ошибка скачивания: " + ex.Message
                };
            }
            finally
            {
                IsDownloading = false;
            }
        }
    }
}

