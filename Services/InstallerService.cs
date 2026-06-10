using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace MacStyleHub.Services
{
    public enum InstallState
    {
        NotInstalled,
        Queued,
        Installing,
        Installed,
        Failed
    }

    public class InstallerProgram
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string WingetId { get; set; } = "";
        public string Description { get; set; } = "";
        public string IconKey { get; set; } = "";
        public InstallState State { get; set; } = InstallState.NotInstalled;
        public int Progress { get; set; } = 0;
        public string StatusMessage { get; set; } = "";
    }

    public class InstallerService
    {
        private static InstallerService? _instance;
        public static InstallerService Instance => _instance ??= new InstallerService();

        public event Action<string, InstallState, int, string>? ProgramStateChanged;

        private readonly Dictionary<string, InstallState> _states = new();
        private readonly Dictionary<string, int> _progress = new();
        private readonly Dictionary<string, string> _statusMessages = new();
        private readonly List<InstallerProgram> _programs = new();

        private InstallerService()
        {
            // Initialize list of essential programs
            _programs.Add(new InstallerProgram
            {
                Id = "chrome",
                Name = "Google Chrome",
                Category = "Браузеры",
                WingetId = "Google.Chrome",
                Description = "Быстрый, безопасный и популярный веб-браузер от компании Google.",
                IconKey = "IconHome"
            });
            _programs.Add(new InstallerProgram
            {
                Id = "discord",
                Name = "Discord",
                Category = "Мессенджеры",
                WingetId = "Discord.Discord",
                Description = "Голосовой, видео- и текстовый чат для геймеров и создателей контента.",
                IconKey = "IconMusic"
            });
            _programs.Add(new InstallerProgram
            {
                Id = "steam",
                Name = "Steam",
                Category = "Игры",
                WingetId = "Valve.Steam",
                Description = "Популярная игровая платформа для запуска игр, общения и творчества.",
                IconKey = "IconSettings"
            });
            _programs.Add(new InstallerProgram
            {
                Id = "vlc",
                Name = "VLC Media Player",
                Category = "Плееры",
                WingetId = "VideoLAN.VLC",
                Description = "Бесплатный медиаплеер с открытым исходным кодом, воспроизводящий большинство форматов.",
                IconKey = "IconMusic"
            });
            _programs.Add(new InstallerProgram
            {
                Id = "telegram",
                Name = "Telegram Desktop",
                Category = "Мессенджеры",
                WingetId = "Telegram.TelegramDesktop",
                Description = "Быстрый и безопасный мессенджер с облачной синхронизацией сообщений.",
                IconKey = "IconLocation"
            });
            _programs.Add(new InstallerProgram
            {
                Id = "spotify",
                Name = "Spotify",
                Category = "Плееры",
                WingetId = "Spotify.Spotify",
                Description = "Стриминговый сервис, предоставляющий доступ к миллионам музыкальных треков.",
                IconKey = "IconMusic"
            });
            _programs.Add(new InstallerProgram
            {
                Id = "7zip",
                Name = "7-Zip",
                Category = "Утилиты",
                WingetId = "7zip.7zip",
                Description = "Популярный архиватор с высокой степенью сжатия файлов и шифрованием AES-256.",
                IconKey = "IconCleaner"
            });
            _programs.Add(new InstallerProgram
            {
                Id = "yandexmusicmod",
                Name = "Яндекс Музыка (Mod)",
                Category = "Плееры",
                WingetId = "",
                Description = "Модифицированная версия Яндекс Музыки без рекламы и ограничений.",
                IconKey = "IconMusic"
            });

            foreach (var prog in _programs)
            {
                _states[prog.Id] = InstallState.NotInstalled;
                _progress[prog.Id] = 0;
                _statusMessages[prog.Id] = "";
            }

            // Run initial scan asynchronously
            Task.Run(() => ScanInstalledApps());
        }

        public List<InstallerProgram> GetPrograms()
        {
            var list = new List<InstallerProgram>();
            foreach (var p in _programs)
            {
                var prog = new InstallerProgram
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.Category,
                    WingetId = p.WingetId,
                    Description = p.Description,
                    IconKey = p.IconKey,
                    State = _states[p.Id],
                    Progress = _progress[p.Id],
                    StatusMessage = _statusMessages[p.Id]
                };

                // Dynamic translation mapping for Yandex Music Mod
                if (p.Id == "yandexmusicmod")
                {
                    prog.Name = LocalizationService.Instance.YandexMusicModName;
                    prog.Description = LocalizationService.Instance.YandexMusicModDesc;
                    prog.Category = LocalizationService.Instance.SidebarPlayer;
                }

                list.Add(prog);
            }
            return list;
        }

        public void ScanInstalledApps()
        {
            foreach (var prog in _programs)
            {
                bool isInstalled = IsInstalledOnComputer(prog.Id, prog.WingetId);
                _states[prog.Id] = isInstalled ? InstallState.Installed : InstallState.NotInstalled;
                _statusMessages[prog.Id] = isInstalled ? "Установлено" : "";
                _progress[prog.Id] = isInstalled ? 100 : 0;
                ProgramStateChanged?.Invoke(prog.Id, _states[prog.Id], _progress[prog.Id], _statusMessages[prog.Id]);
            }
        }

        private bool IsInstalledOnComputer(string id, string wingetId)
        {
            if (OperatingSystem.IsWindows())
            {
                switch (id)
                {
                    case "chrome":
                        return File.Exists(@"C:\Program Files\Google\Chrome\Application\chrome.exe") ||
                               File.Exists(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe") ||
                               CheckRegistryForDisplayName("Google Chrome");

                    case "discord":
                        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        string discordFolder = Path.Combine(localAppData, "Discord");
                        if (Directory.Exists(discordFolder))
                        {
                            if (Directory.GetFiles(discordFolder, "Discord.exe", SearchOption.AllDirectories).Length > 0)
                                return true;
                        }
                        return CheckRegistryForDisplayName("Discord");

                    case "steam":
                        return File.Exists(@"C:\Program Files (x86)\Steam\steam.exe") ||
                               File.Exists(@"C:\Program Files\Steam\steam.exe") ||
                               CheckRegistryForDisplayName("Steam") ||
                               CheckRegistryKey(@"Software\Valve\Steam", "SteamPath");

                    case "vlc":
                        return File.Exists(@"C:\Program Files\VideoLAN\VLC\vlc.exe") ||
                               File.Exists(@"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe") ||
                               CheckRegistryForDisplayName("VLC media player");

                    case "telegram":
                        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        return File.Exists(Path.Combine(appData, @"Telegram Desktop\Telegram.exe")) ||
                               File.Exists(@"C:\Program Files\Telegram Desktop\Telegram.exe") ||
                               CheckRegistryForDisplayName("Telegram Desktop") ||
                               CheckRegistryForDisplayName("Telegram");

                    case "spotify":
                        string localAppData2 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        string appData2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        return File.Exists(Path.Combine(localAppData2, @"Microsoft\WindowsApps\Spotify.exe")) ||
                               File.Exists(Path.Combine(appData2, @"Spotify\Spotify.exe")) ||
                               CheckRegistryForDisplayName("Spotify");

                    case "7zip":
                        return File.Exists(@"C:\Program Files\7-Zip\7zFM.exe") ||
                               File.Exists(@"C:\Program Files (x86)\7-Zip\7zFM.exe") ||
                               CheckRegistryForDisplayName("7-Zip");

                    case "yandexmusicmod":
                        string localAppDataMod = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        string yandexModPath = Path.Combine(localAppDataMod, @"YandexMusicMod");
                        if (Directory.Exists(yandexModPath))
                        {
                            if (Directory.GetFiles(yandexModPath, "*.exe", SearchOption.AllDirectories).Length > 0)
                                return true;
                        }
                        string programsFolder = Path.Combine(localAppDataMod, "Programs");
                        if (Directory.Exists(programsFolder))
                        {
                            if (Directory.GetDirectories(programsFolder, "*yandexmusic*", SearchOption.AllDirectories).Length > 0)
                                return true;
                        }
                        return CheckRegistryForDisplayName("YandexMusicMod") ||
                               CheckRegistryForDisplayName("Yandex Music Mod") ||
                               CheckRegistryForDisplayName("Yandex.Music");
                }
            }
            return false;
        }

        private bool CheckRegistryKey(string keyPath, string valueName)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    if (key?.GetValue(valueName) != null)
                        return true;
                }
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key?.GetValue(valueName) != null)
                        return true;
                }
            }
            catch { }
            return false;
        }

        private bool CheckRegistryForDisplayName(string nameToFind)
        {
            try
            {
                string[] registryPaths = new string[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (var path in registryPaths)
                {
                    // Check LocalMachine
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key != null)
                        {
                            foreach (var subkeyName in key.GetSubKeyNames())
                            {
                                using (var subkey = key.OpenSubKey(subkeyName))
                                {
                                    var displayName = subkey?.GetValue("DisplayName")?.ToString();
                                    if (displayName != null && displayName.Contains(nameToFind, StringComparison.OrdinalIgnoreCase))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }

                    // Check CurrentUser
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path))
                    {
                        if (key != null)
                        {
                            foreach (var subkeyName in key.GetSubKeyNames())
                            {
                                using (var subkey = key.OpenSubKey(subkeyName))
                                {
                                    var displayName = subkey?.GetValue("DisplayName")?.ToString();
                                    if (displayName != null && displayName.Contains(nameToFind, StringComparison.OrdinalIgnoreCase))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public void InstallProgram(string id)
        {
            var prog = _programs.Find(p => p.Id == id);
            if (prog == null) return;

            if (_states[id] == InstallState.Installing || _states[id] == InstallState.Queued)
                return;

            if (id == "yandexmusicmod")
            {
                InstallYandexMusicMod(id);
                return;
            }

            _states[id] = InstallState.Installing;
            _progress[id] = 10;
            _statusMessages[id] = "Запуск winget...";
            ProgramStateChanged?.Invoke(id, InstallState.Installing, 10, _statusMessages[id]);

            Task.Run(async () =>
            {
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = $"install --id {prog.WingetId} --silent --accept-source-agreements --accept-package-agreements",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    _progress[id] = 30;
                    _statusMessages[id] = "Скачивание и подготовка...";
                    ProgramStateChanged?.Invoke(id, InstallState.Installing, 30, _statusMessages[id]);

                    using var process = System.Diagnostics.Process.Start(startInfo);
                    if (process == null)
                    {
                        _states[id] = InstallState.Failed;
                        _progress[id] = 0;
                        _statusMessages[id] = "Не удалось запустить winget.";
                        ProgramStateChanged?.Invoke(id, InstallState.Failed, 0, _statusMessages[id]);
                        return;
                    }

                    _progress[id] = 60;
                    _statusMessages[id] = "Установка приложения...";
                    ProgramStateChanged?.Invoke(id, InstallState.Installing, 60, _statusMessages[id]);

                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        _states[id] = InstallState.Installed;
                        _progress[id] = 100;
                        _statusMessages[id] = "Успешно установлено!";
                        ProgramStateChanged?.Invoke(id, InstallState.Installed, 100, _statusMessages[id]);
                    }
                    else
                    {
                        if (process.ExitCode == -1978335189 || process.ExitCode == -1978335185) // Hex codes for already installed
                        {
                            _states[id] = InstallState.Installed;
                            _progress[id] = 100;
                            _statusMessages[id] = "Уже установлено!";
                            ProgramStateChanged?.Invoke(id, InstallState.Installed, 100, _statusMessages[id]);
                        }
                        else
                        {
                            _states[id] = InstallState.Failed;
                            _progress[id] = 0;
                            _statusMessages[id] = $"Код ошибки: {process.ExitCode}";
                            ProgramStateChanged?.Invoke(id, InstallState.Failed, 0, _statusMessages[id]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _states[id] = InstallState.Failed;
                    _progress[id] = 0;
                    _statusMessages[id] = ex.Message;
                    ProgramStateChanged?.Invoke(id, InstallState.Failed, 0, _statusMessages[id]);
                }
            });
        }

        private void InstallYandexMusicMod(string id)
        {
            _states[id] = InstallState.Installing;
            _progress[id] = 10;
            _statusMessages[id] = "Скачивание мода...";
            ProgramStateChanged?.Invoke(id, InstallState.Installing, 10, _statusMessages[id]);

            Task.Run(async () =>
            {
                string tempZip = Path.Combine(Path.GetTempPath(), "YandexMusicMod.zip");
                string tempExtractPath = Path.Combine(Path.GetTempPath(), "YandexMusicModTemp");
                string finalInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\YandexMusicMod");

                try
                {
                    // 1. Download ZIP
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                        using (var response = await client.GetAsync("https://github.com/Stephanzion/YandexMusicBetaMod/releases/download/v2.2.0/YandexMusicMod-5.86.0-2.2.0.windows.zip", HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();
                            var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                            using (var contentStream = await response.Content.ReadAsStreamAsync())
                            using (var fileStream = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                            {
                                var buffer = new byte[8192];
                                var totalRead = 0L;
                                int read;

                                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, read);
                                    totalRead += read;

                                    if (totalBytes > 0)
                                    {
                                        int progressPct = 10 + (int)((double)totalRead / totalBytes * 50);
                                        if (progressPct != _progress[id])
                                        {
                                            _progress[id] = progressPct;
                                            _statusMessages[id] = $"Скачивание: {progressPct - 10}%";
                                            ProgramStateChanged?.Invoke(id, InstallState.Installing, progressPct, _statusMessages[id]);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // 2. Extract ZIP to temporary staging folder
                    _progress[id] = 70;
                    _statusMessages[id] = "Распаковка архива...";
                    ProgramStateChanged?.Invoke(id, InstallState.Installing, 70, _statusMessages[id]);

                    if (Directory.Exists(tempExtractPath))
                    {
                        Directory.Delete(tempExtractPath, true);
                    }
                    Directory.CreateDirectory(tempExtractPath);

                    ZipFile.ExtractToDirectory(tempZip, tempExtractPath);

                    // 3. Find setup executable in extracted files
                    string foundSetupExe = "";
                    var exes = Directory.GetFiles(tempExtractPath, "*.exe", SearchOption.TopDirectoryOnly);
                    if (exes.Length > 0)
                    {
                        foundSetupExe = exes[0];
                    }
                    else
                    {
                        var allExes = Directory.GetFiles(tempExtractPath, "*.exe", SearchOption.AllDirectories);
                        if (allExes.Length > 0)
                        {
                            foundSetupExe = allExes[0];
                        }
                    }

                    if (string.IsNullOrEmpty(foundSetupExe))
                    {
                        throw new Exception("Не найден исполняемый файл установки в архиве.");
                    }

                    // Copy to desktop of the current user
                    _progress[id] = 80;
                    _statusMessages[id] = "Копирование на рабочий стол...";
                    ProgramStateChanged?.Invoke(id, InstallState.Installing, 80, _statusMessages[id]);

                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string targetDesktopExe = Path.Combine(desktopPath, "Яндекс Музыка Setup 5.86.0.exe");

                    File.Copy(foundSetupExe, targetDesktopExe, true);

                    // 4. Run the setup exe from the Desktop to install/unpack into target folder
                    _progress[id] = 85;
                    _statusMessages[id] = "Распаковка установщика...";
                    ProgramStateChanged?.Invoke(id, InstallState.Installing, 85, _statusMessages[id]);

                    var processStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = targetDesktopExe,
                        Arguments = $"/S /D={finalInstallPath}",
                        UseShellExecute = true
                    };

                    using (var process = System.Diagnostics.Process.Start(processStartInfo))
                    {
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                        }
                    }

                    try
                    {
                        if (File.Exists(tempZip))
                            File.Delete(tempZip);
                        if (Directory.Exists(tempExtractPath))
                            Directory.Delete(tempExtractPath, true);
                    }
                    catch { }

                    _states[id] = InstallState.Installed;
                    _progress[id] = 100;
                    _statusMessages[id] = "Успешно установлено!";
                    ProgramStateChanged?.Invoke(id, InstallState.Installed, 100, _statusMessages[id]);
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (File.Exists(tempZip))
                            File.Delete(tempZip);
                        if (Directory.Exists(tempExtractPath))
                            Directory.Delete(tempExtractPath, true);
                    }
                    catch { }

                    _states[id] = InstallState.Failed;
                    _progress[id] = 0;
                    _statusMessages[id] = $"Ошибка: {ex.Message}";
                    ProgramStateChanged?.Invoke(id, InstallState.Failed, 0, _statusMessages[id]);
                }
            });
        }
    }
}
