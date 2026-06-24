using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace SystemHub.Services
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

        private readonly ConcurrentDictionary<string, InstallState> _states = new();
        private readonly ConcurrentDictionary<string, int> _progress = new();
        private readonly ConcurrentDictionary<string, string> _statusMessages = new();
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
                Id = "yandexbrowser",
                Name = "Yandex Browser",
                Category = "Браузеры",
                WingetId = "Yandex.YandexBrowser",
                Description = "Быстрый и безопасный браузер с голосовым помощником Алисой.",
                IconKey = "IconHome"
            });
            _programs.Add(new InstallerProgram
            {
                Id = "firefox",
                Name = "Mozilla Firefox",
                Category = "Браузеры",
                WingetId = "Mozilla.Firefox",
                Description = "Веб-браузер от Mozilla. Быстрый, приватный и независимый.",
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
                Id = "yandexmusic",
                Name = "Яндекс Музыка",
                Category = "Плееры",
                WingetId = "Yandex.Music",
                Description = "Стриминговый сервис Яндекс Музыка для Windows.",
                IconKey = "IconMusic"
            });
            _programs.Add(new InstallerProgram
            {
                Id = "zapret",
                Name = "Zapret (YouTube/Discord)",
                Category = "Утилиты",
                WingetId = "",
                Description = "Обход ограничений и замедления YouTube и Discord в России.",
                IconKey = "IconFlash"
            });

            foreach (var prog in _programs)
            {
                if (prog.Id == "telegram" || prog.Id == "spotify" || prog.Id == "yandexmusic")
                {
                    _states[prog.Id + "_reg"] = InstallState.NotInstalled;
                    _progress[prog.Id + "_reg"] = 0;
                    _statusMessages[prog.Id + "_reg"] = "";

                    _states[prog.Id + "_mod"] = InstallState.NotInstalled;
                    _progress[prog.Id + "_mod"] = 0;
                    _statusMessages[prog.Id + "_mod"] = "";
                }
                else
                {
                    _states[prog.Id] = InstallState.NotInstalled;
                    _progress[prog.Id] = 0;
                    _statusMessages[prog.Id] = "";
                }
            }

            // Run initial scan asynchronously in the background to prevent UI lag on startup/first load
            Task.Run(() => ScanInstalledApps());
        }

        public List<InstallerProgram> GetPrograms()
        {
            var list = new List<InstallerProgram>();
            foreach (var p in _programs)
            {
                bool isUnified = (p.Id == "telegram" || p.Id == "spotify" || p.Id == "yandexmusic");
                string key = isUnified ? (p.Id + "_reg") : p.Id;

                var prog = new InstallerProgram
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.Category,
                    WingetId = p.WingetId,
                    Description = p.Description,
                    IconKey = p.IconKey,
                    State = _states[key],
                    Progress = _progress[key],
                    StatusMessage = _statusMessages[key]
                };

                // Map category translations dynamically
                prog.Category = p.Category switch
                {
                    "Браузеры" => LocalizationService.Instance.CategoryBrowsers,
                    "Мессенджеры" => LocalizationService.Instance.CategoryMessengers,
                    "Игры" => LocalizationService.Instance.CategoryGames,
                    "Плееры" => LocalizationService.Instance.CategoryPlayers,
                    "Утилиты" => LocalizationService.Instance.CategoryUtilities,
                    _ => p.Category
                };

                // Map description translations dynamically
                prog.Description = p.Id switch
                {
                    "chrome" => LocalizationService.Instance.DescChrome,
                    "yandexbrowser" => LocalizationService.Instance.DescYandexBrowser,
                    "firefox" => LocalizationService.Instance.DescFirefox,
                    "discord" => LocalizationService.Instance.DescDiscord,
                    "steam" => LocalizationService.Instance.DescSteam,
                    "vlc" => LocalizationService.Instance.DescVlc,
                    "telegram" => LocalizationService.Instance.DescTelegram,
                    "spotify" => LocalizationService.Instance.DescSpotify,
                    "7zip" => LocalizationService.Instance.Desc7Zip,
                    "yandexmusic" => LocalizationService.Instance.DescYandexMusic,
                    "zapret" => LocalizationService.Instance.ZapretDesc,
                    _ => p.Description
                };

                // Special overrides
                if (p.Id == "yandexmusic")
                {
                    prog.Name = LocalizationService.Instance.YandexMusicName;
                }
                else if (p.Id == "zapret")
                {
                    prog.Name = LocalizationService.Instance.ZapretName;
                    prog.Category = LocalizationService.Instance.CategoryUtilities;
                }

                list.Add(prog);
            }
            return list;
        }

        public void ScanInstalledApps()
        {
            foreach (var prog in _programs)
            {
                if (prog.Id == "telegram" || prog.Id == "spotify" || prog.Id == "yandexmusic")
                {
                    // Scan regular
                    bool isRegInstalled = IsInstalledOnComputer(prog.Id, false);
                    string regKey = prog.Id + "_reg";
                    _states[regKey] = isRegInstalled ? InstallState.Installed : InstallState.NotInstalled;
                    _statusMessages[regKey] = isRegInstalled ? "Установлено" : "";
                    _progress[regKey] = isRegInstalled ? 100 : 0;
                    ProgramStateChanged?.Invoke(regKey, _states[regKey], _progress[regKey], _statusMessages[regKey]);

                    // Scan mod
                    bool isModInstalled = IsInstalledOnComputer(prog.Id, true);
                    string modKey = prog.Id + "_mod";
                    _states[modKey] = isModInstalled ? InstallState.Installed : InstallState.NotInstalled;
                    _statusMessages[modKey] = isModInstalled ? "Установлено" : "";
                    _progress[modKey] = isModInstalled ? 100 : 0;
                    ProgramStateChanged?.Invoke(modKey, _states[modKey], _progress[modKey], _statusMessages[modKey]);

                    if (prog.Id == "telegram" && isModInstalled)
                    {
                        TryCreateAyuGramShortcut();
                    }
                }
                else
                {
                    bool isInstalled = IsInstalledOnComputer(prog.Id, false);
                    _states[prog.Id] = isInstalled ? InstallState.Installed : InstallState.NotInstalled;
                    _statusMessages[prog.Id] = isInstalled ? "Установлено" : "";
                    _progress[prog.Id] = isInstalled ? 100 : 0;
                    ProgramStateChanged?.Invoke(prog.Id, _states[prog.Id], _progress[prog.Id], _statusMessages[prog.Id]);
                }
            }
        }

        private bool IsInstalledOnComputer(string id, bool isMod)
        {
            if (OperatingSystem.IsWindows())
            {
                switch (id)
                {
                    case "chrome":
                        return File.Exists(@"C:\Program Files\Google\Chrome\Application\chrome.exe") ||
                               File.Exists(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe") ||
                               File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe")) ||
                               CheckRegistryForDisplayName("Google Chrome");

                    case "yandexbrowser":
                        string localYandex = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Yandex\YandexBrowser\Application\browser.exe");
                        return File.Exists(@"C:\Program Files\Yandex\YandexBrowser\Application\browser.exe") ||
                               File.Exists(@"C:\Program Files (x86)\Yandex\YandexBrowser\Application\browser.exe") ||
                               File.Exists(localYandex) ||
                               CheckRegistryForDisplayName("Yandex Browser") ||
                               CheckRegistryForDisplayName("Яндекс.Браузер") ||
                               CheckRegistryForDisplayName("YandexBrowser");

                    case "firefox":
                        return File.Exists(@"C:\Program Files\Mozilla Firefox\firefox.exe") ||
                               File.Exists(@"C:\Program Files (x86)\Mozilla Firefox\firefox.exe") ||
                               CheckRegistryForDisplayName("Mozilla Firefox") ||
                               CheckRegistryForDisplayName("Firefox");

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
                        if (isMod)
                        {
                            return CheckRegistryForDisplayName("AyuGram") ||
                                   CheckRegistryForDisplayName("AyuGram Desktop") ||
                                   File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"AyuGram\AyuGram.exe")) ||
                                   File.Exists(@"C:\Program Files\AyuGram\AyuGram.exe");
                        }
                        else
                        {
                            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                            return File.Exists(Path.Combine(appData, @"Telegram Desktop\Telegram.exe")) ||
                                   File.Exists(@"C:\Program Files\Telegram Desktop\Telegram.exe") ||
                                   CheckRegistryForDisplayName("Telegram Desktop") ||
                                   CheckRegistryForDisplayName("Telegram");
                        }

                    case "spotify":
                        string localAppData2 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        string appData2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        bool isSpotiInstalled = File.Exists(Path.Combine(localAppData2, @"Microsoft\WindowsApps\Spotify.exe")) ||
                                               File.Exists(Path.Combine(appData2, @"Spotify\Spotify.exe")) ||
                                               CheckRegistryForDisplayName("Spotify");
                        if (isMod)
                        {
                            string spotXBackupPath1 = Path.Combine(appData2, @"Spotify\Apps\xpui.spa.bak");
                            string spotXBackupPath2 = Path.Combine(appData2, @"Spotify\xpui.spa.bak");
                            return isSpotiInstalled && (File.Exists(spotXBackupPath1) || File.Exists(spotXBackupPath2));
                        }
                        return isSpotiInstalled;

                    case "7zip":
                        return File.Exists(@"C:\Program Files\7-Zip\7zFM.exe") ||
                               File.Exists(@"C:\Program Files (x86)\7-Zip\7zFM.exe") ||
                               CheckRegistryForDisplayName("7-Zip");

                    case "yandexmusic":
                        if (isMod)
                        {
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
                        else
                        {
                            return CheckRegistryForDisplayName("Yandex Music") ||
                                   CheckRegistryForDisplayName("Yandex.Music") ||
                                   File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Yandex.Music\Yandex.Music.exe")) ||
                                   File.Exists(@"C:\Program Files\Yandex.Music\Yandex.Music.exe") ||
                                   File.Exists(@"C:\Program Files (x86)\Yandex.Music\Yandex.Music.exe");
                        }

                    case "zapret":
                        return !string.IsNullOrEmpty(GetInstalledZapretVersion(out _));
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

        public InstallState GetState(string id, bool isMod)
        {
            string key = (id == "telegram" || id == "spotify" || id == "yandexmusic") 
                ? (id + (isMod ? "_mod" : "_reg")) 
                : id;
            return _states.TryGetValue(key, out var state) ? state : InstallState.NotInstalled;
        }

        public int GetProgress(string id, bool isMod)
        {
            string key = (id == "telegram" || id == "spotify" || id == "yandexmusic") 
                ? (id + (isMod ? "_mod" : "_reg")) 
                : id;
            return _progress.TryGetValue(key, out var prog) ? prog : 0;
        }

        public string GetStatusMessage(string id, bool isMod)
        {
            string key = (id == "telegram" || id == "spotify" || id == "yandexmusic") 
                ? (id + (isMod ? "_mod" : "_reg")) 
                : id;
            return _statusMessages.TryGetValue(key, out var msg) ? msg : "";
        }

        public void InstallProgram(string id, bool isMod)
        {
            var prog = _programs.Find(p => p.Id == id);
            if (prog == null) return;

            string key = (id == "telegram" || id == "spotify" || id == "yandexmusic")
                ? (id + (isMod ? "_mod" : "_reg"))
                : id;

            if (_states[key] == InstallState.Installing || _states[key] == InstallState.Queued)
                return;

            if (id == "yandexmusic" && isMod)
            {
                InstallYandexMusicMod(key);
                return;
            }

            if (id == "spotify" && isMod)
            {
                InstallSpotX(key);
                return;
            }

            if (id == "zapret")
            {
                InstallZapret(id);
                return;
            }

            _states[key] = InstallState.Installing;
            _progress[key] = 10;
            _statusMessages[key] = "Запуск winget...";
            ProgramStateChanged?.Invoke(key, InstallState.Installing, 10, _statusMessages[key]);

            Task.Run(async () =>
            {
                try
                {
                    string wingetId = prog.WingetId;
                    if (id == "telegram")
                    {
                        wingetId = isMod ? "RadolynLabs.AyuGramDesktop" : "Telegram.TelegramDesktop";
                    }
                    else if (id == "spotify")
                    {
                        wingetId = "Spotify.Spotify";
                    }
                    else if (id == "yandexmusic")
                    {
                        wingetId = "Yandex.Music";
                    }

                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = $"install --id {wingetId} --silent --accept-source-agreements --accept-package-agreements",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false
                    };

                    _progress[key] = 30;
                    _statusMessages[key] = "Скачивание и подготовка...";
                    ProgramStateChanged?.Invoke(key, InstallState.Installing, 30, _statusMessages[key]);

                    using var process = System.Diagnostics.Process.Start(startInfo);
                    if (process == null)
                    {
                        _states[key] = InstallState.Failed;
                        _progress[key] = 0;
                        _statusMessages[key] = "Не удалось запустить winget.";
                        ProgramStateChanged?.Invoke(key, InstallState.Failed, 0, _statusMessages[key]);
                        return;
                    }

                    _progress[key] = 60;
                    _statusMessages[key] = "Установка приложения...";
                    ProgramStateChanged?.Invoke(key, InstallState.Installing, 60, _statusMessages[key]);

                    await process.WaitForExitAsync();

                    bool installedCheck = IsInstalledOnComputer(id, isMod);
                    if (process.ExitCode == 0 || process.ExitCode == -1978335189 || process.ExitCode == -1978335185 || installedCheck)
                    {
                        _states[key] = InstallState.Installed;
                        _progress[key] = 100;
                        _statusMessages[key] = (process.ExitCode == -1978335189 || process.ExitCode == -1978335185) ? "Уже установлено!" : "Успешно установлено!";
                        ProgramStateChanged?.Invoke(key, InstallState.Installed, 100, _statusMessages[key]);

                        if (id == "telegram" && isMod)
                        {
                            TryCreateAyuGramShortcut();
                        }
                    }
                    else
                    {
                        _states[key] = InstallState.Failed;
                        _progress[key] = 0;
                        _statusMessages[key] = $"Код ошибки: {process.ExitCode}";
                        ProgramStateChanged?.Invoke(key, InstallState.Failed, 0, _statusMessages[key]);
                    }
                }
                catch (Exception ex)
                {
                    _states[key] = InstallState.Failed;
                    _progress[key] = 0;
                    _statusMessages[key] = ex.Message;
                    ProgramStateChanged?.Invoke(key, InstallState.Failed, 0, _statusMessages[key]);
                }
            });
        }

        private void InstallSpotX(string key)
        {
            _states[key] = InstallState.Installing;
            _progress[key] = 10;
            _statusMessages[key] = "Запуск установщика SpotX...";
            ProgramStateChanged?.Invoke(key, InstallState.Installing, 10, _statusMessages[key]);

            Task.Run(async () =>
            {
                string tempZip = Path.Combine(Path.GetTempPath(), "SpotX_" + Guid.NewGuid().ToString("N") + ".zip");
                string tempExtractPath = Path.Combine(Path.GetTempPath(), "SpotX_" + Guid.NewGuid().ToString("N"));
                try
                {
                    _progress[key] = 20;
                    _statusMessages[key] = "Скачивание SpotX...";
                    ProgramStateChanged?.Invoke(key, InstallState.Installing, 20, _statusMessages[key]);

                    var downloadUrls = new[]
                    {
                        "https://github.com/SpotX-Official/SpotX/archive/refs/heads/main.zip",
                        "https://ghproxy.com/https://github.com/SpotX-Official/SpotX/archive/refs/heads/main.zip",
                        "https://ghproxy.net/https://github.com/SpotX-Official/SpotX/archive/refs/heads/main.zip",
                        "https://github.moeyy.xyz/https://github.com/SpotX-Official/SpotX/archive/refs/heads/main.zip",
                        "https://kkgithub.com/SpotX-Official/SpotX/archive/refs/heads/main.zip"
                    };

                    bool downloadSuccess = false;
                    Exception? lastException = null;

                    for (int i = 0; i < downloadUrls.Length; i++)
                    {
                        string url = downloadUrls[i];
                        string host = new Uri(url).Host;
                        _statusMessages[key] = $"Скачивание SpotX ({host})...";
                        ProgramStateChanged?.Invoke(key, InstallState.Installing, 20, _statusMessages[key]);

                        try
                        {
                            using (var client = new HttpClient())
                            {
                                client.Timeout = TimeSpan.FromSeconds(30);
                                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                                using (var response = await client.GetAsync(url))
                                {
                                    response.EnsureSuccessStatusCode();
                                    using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
                                    {
                                        await response.Content.CopyToAsync(fs);
                                    }
                                }
                            }
                            downloadSuccess = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch {}
                        }
                    }

                    if (!downloadSuccess)
                    {
                        throw new Exception($"Не удалось скачать SpotX. Последняя ошибка: {lastException?.Message}", lastException);
                    }

                    _progress[key] = 50;
                    _statusMessages[key] = "Распаковка установщика...";
                    ProgramStateChanged?.Invoke(key, InstallState.Installing, 50, _statusMessages[key]);

                    if (Directory.Exists(tempExtractPath))
                    {
                        Directory.Delete(tempExtractPath, true);
                    }
                    Directory.CreateDirectory(tempExtractPath);

                    ZipFile.ExtractToDirectory(tempZip, tempExtractPath);

                    // Find run.ps1 inside the extracted archive
                    var ps1Files = Directory.GetFiles(tempExtractPath, "run.ps1", SearchOption.AllDirectories);
                    if (ps1Files.Length == 0)
                    {
                        throw new FileNotFoundException("Файл run.ps1 не найден в архиве SpotX.");
                    }
                    string runPs1Path = ps1Files[0];

                    // Kill any running Spotify processes to unlock files before installation (avoids Error 18)
                    try
                    {
                        foreach (var proc in System.Diagnostics.Process.GetProcessesByName("Spotify"))
                        {
                            proc.Kill(true);
                        }
                    }
                    catch { }



                    _progress[key] = 60;
                    _statusMessages[key] = "Установка SpotX...";
                    ProgramStateChanged?.Invoke(key, InstallState.Installing, 60, _statusMessages[key]);

                    // Run the local script in a visible window to allow interactive choices.
                    // Omit -confirm_spoti_recomended_over to prevent downloading Spotify from blocked workers.dev.
                    string arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{runPs1Path}\" -m -confirm_uninstall_ms_spoti -block_update_on -new_theme";

                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = arguments,
                        WorkingDirectory = Path.GetDirectoryName(runPs1Path),
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                    using var process = System.Diagnostics.Process.Start(startInfo);
                    if (process == null)
                    {
                        _states[key] = InstallState.Failed;
                        _progress[key] = 0;
                        _statusMessages[key] = "Не удалось запустить PowerShell.";
                        ProgramStateChanged?.Invoke(key, InstallState.Failed, 0, _statusMessages[key]);
                        return;
                    }

                    await process.WaitForExitAsync();

                    bool installedCheck = IsInstalledOnComputer("spotify", true);
                    if (process.ExitCode == 0 || installedCheck)
                    {
                        _states[key] = InstallState.Installed;
                        _progress[key] = 100;
                        _statusMessages[key] = "Успешно установлено!";
                        ProgramStateChanged?.Invoke(key, InstallState.Installed, 100, _statusMessages[key]);
                    }
                    else
                    {
                        _states[key] = InstallState.Failed;
                        _progress[key] = 0;
                        _statusMessages[key] = $"Код ошибки: {process.ExitCode}";
                        ProgramStateChanged?.Invoke(key, InstallState.Failed, 0, _statusMessages[key]);
                    }
                }
                catch (Exception ex)
                {
                    _states[key] = InstallState.Failed;
                    _progress[key] = 0;
                    _statusMessages[key] = $"Ошибка: {ex.Message}";
                    ProgramStateChanged?.Invoke(key, InstallState.Failed, 0, _statusMessages[key]);
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempZip))
                            File.Delete(tempZip);
                        if (Directory.Exists(tempExtractPath))
                            Directory.Delete(tempExtractPath, true);
                    }
                    catch { }
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
                        if (File.Exists(targetDesktopExe))
                            File.Delete(targetDesktopExe);
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
                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string targetDesktopExe = Path.Combine(desktopPath, "Яндекс Музыка Setup 5.86.0.exe");
                        if (File.Exists(targetDesktopExe))
                            File.Delete(targetDesktopExe);
                    }
                    catch { }

                    _states[id] = InstallState.Failed;
                    _progress[id] = 0;
                    _statusMessages[id] = $"Ошибка: {ex.Message}";
                    ProgramStateChanged?.Invoke(id, InstallState.Failed, 0, _statusMessages[id]);
                }
            });
        }

        private void InstallZapret(string id)
        {
            _states[id] = InstallState.Installing;
            _progress[id] = 10;
            _statusMessages[id] = "Скачивание Zapret...";
            ProgramStateChanged?.Invoke(id, InstallState.Installing, 10, _statusMessages[id]);

            Task.Run(async () =>
            {
                string tempZip = Path.Combine(Path.GetTempPath(), "zapret.zip");
                string tempExtractPath = Path.Combine(Path.GetTempPath(), "zapret_extract");
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                try
                {
                    // 1. Check latest version from GitHub
                    string versionToDownload = "1.9.9a";
                    using (var client = new HttpClient())
                    {
                        try
                        {
                            client.DefaultRequestHeaders.Add("User-Agent", "SystemHub-App");
                            using var response = await client.GetAsync("https://api.github.com/repos/Flowseal/zapret-discord-youtube/releases/latest");
                            if (response.IsSuccessStatusCode)
                            {
                                var json = await response.Content.ReadAsStringAsync();
                                using var doc = System.Text.Json.JsonDocument.Parse(json);
                                if (doc.RootElement.TryGetProperty("tag_name", out var tagProp))
                                {
                                    versionToDownload = tagProp.GetString()?.Trim() ?? "1.9.9a";
                                }
                            }
                        }
                        catch { }
                    }

                    string finalInstallPath = Path.Combine(desktopPath, $"zapret-discord-youtube-{versionToDownload}");

                    // 2. Download ZIP
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        string downloadUrl = $"https://github.com/Flowseal/zapret-discord-youtube/releases/download/{versionToDownload}/zapret-discord-youtube-{versionToDownload}.zip";
                        using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
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

                    // 3. Extract ZIP
                    _progress[id] = 70;
                    _statusMessages[id] = "Распаковка архива...";
                    ProgramStateChanged?.Invoke(id, InstallState.Installing, 70, _statusMessages[id]);

                    if (Directory.Exists(tempExtractPath))
                    {
                        Directory.Delete(tempExtractPath, true);
                    }
                    Directory.CreateDirectory(tempExtractPath);

                    ZipFile.ExtractToDirectory(tempZip, tempExtractPath);

                    // Move to Desktop
                    _progress[id] = 85;
                    _statusMessages[id] = "Копирование на рабочий стол...";
                    ProgramStateChanged?.Invoke(id, InstallState.Installing, 85, _statusMessages[id]);

                    string sourceFolder = tempExtractPath;
                    var subdirs = Directory.GetDirectories(tempExtractPath);
                    var files = Directory.GetFiles(tempExtractPath);
                    if (subdirs.Length == 1 && files.Length == 0)
                    {
                        sourceFolder = subdirs[0];
                    }

                    if (Directory.Exists(finalInstallPath))
                    {
                        Directory.Delete(finalInstallPath, true);
                    }
                    Directory.Move(sourceFolder, finalInstallPath);

                    // Clean up temp files
                    try
                    {
                        if (File.Exists(tempZip))
                            File.Delete(tempZip);
                        if (Directory.Exists(tempExtractPath))
                            Directory.Delete(tempExtractPath, true);
                    }
                    catch { }

                    // Copy Faceit files automatically from downloads if available
                    CopyFaceitFilesToZapret();

                    // 4. Find service.bat and run as Administrator
                    string serviceBat = Path.Combine(finalInstallPath, "service.bat");
                    if (!File.Exists(serviceBat))
                    {
                        var filesFound = Directory.GetFiles(finalInstallPath, "service.bat", SearchOption.AllDirectories);
                        if (filesFound.Length > 0)
                        {
                            serviceBat = filesFound[0];
                        }
                    }

                    if (!File.Exists(serviceBat))
                    {
                        throw new FileNotFoundException("Файл service.bat не найден в архиве.");
                    }

                    _progress[id] = 95;
                    _statusMessages[id] = "Запуск установщика...";
                    ProgramStateChanged?.Invoke(id, InstallState.Installing, 95, _statusMessages[id]);

                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{serviceBat}\"",
                        WorkingDirectory = Path.GetDirectoryName(serviceBat),
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                    System.Diagnostics.Process.Start(psi);

                    // Also start Faceit bypass if files are there
                    string csBat = Path.Combine(finalInstallPath, "cs.bat");
                    if (File.Exists(csBat))
                    {
                        try
                        {
                            var psiCs = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c \"{csBat}\"",
                                WorkingDirectory = finalInstallPath,
                                UseShellExecute = true,
                                Verb = "runas",
                                CreateNoWindow = true,
                                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                            };
                            System.Diagnostics.Process.Start(psiCs);
                        }
                        catch { }
                    }

                    _states[id] = InstallState.Installed;
                    _progress[id] = 100;
                    _statusMessages[id] = "Успешно распаковано!";
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

        private string? GetAyuGramPath()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string path1 = Path.Combine(localAppData, @"AyuGram\AyuGram.exe");
                if (File.Exists(path1)) return path1;

                string path2 = @"C:\Program Files\AyuGram\AyuGram.exe";
                if (File.Exists(path2)) return path2;

                string path3 = @"C:\Program Files (x86)\AyuGram\AyuGram.exe";
                if (File.Exists(path3)) return path3;

                string[] registryPaths = new string[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (var path in registryPaths)
                {
                    foreach (var hive in new[] { Microsoft.Win32.Registry.CurrentUser, Microsoft.Win32.Registry.LocalMachine })
                    {
                        using (var key = hive.OpenSubKey(path))
                        {
                            if (key != null)
                            {
                                foreach (var subkeyName in key.GetSubKeyNames())
                                {
                                    using (var subkey = key.OpenSubKey(subkeyName))
                                    {
                                        var displayName = subkey?.GetValue("DisplayName")?.ToString();
                                        if (displayName != null && displayName.Contains("AyuGram", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var installLoc = subkey?.GetValue("InstallLocation")?.ToString();
                                            if (!string.IsNullOrEmpty(installLoc))
                                            {
                                                string exe = Path.Combine(installLoc, "AyuGram.exe");
                                                if (File.Exists(exe)) return exe;
                                            }
                                            var uninstallStr = subkey?.GetValue("UninstallString")?.ToString();
                                            if (!string.IsNullOrEmpty(uninstallStr))
                                            {
                                                string clean = uninstallStr.Replace("\"", "").Trim();
                                                if (clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    string? dir = Path.GetDirectoryName(clean);
                                                    if (!string.IsNullOrEmpty(dir))
                                                    {
                                                        string exe = Path.Combine(dir, "AyuGram.exe");
                                                        if (File.Exists(exe)) return exe;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private void CreateDesktopShortcut(string name, string targetExePath)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutPath = Path.Combine(desktopPath, $"{name}.lnk");
                if (File.Exists(shortcutPath))
                {
                    return; // Shortcut already exists
                }

                string workDir = Path.GetDirectoryName(targetExePath) ?? "";
                string psCommand = $"$s = New-Object -ComObject WScript.Shell; $g = $s.CreateShortcut('{shortcutPath}'); $g.TargetPath = '{targetExePath}'; $g.WorkingDirectory = '{workDir}'; $g.Save()";

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psCommand}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                process?.WaitForExit();
            }
            catch { }
        }

        public void TryCreateAyuGramShortcut()
        {
            try
            {
                string? ayuPath = GetAyuGramPath();
                if (!string.IsNullOrEmpty(ayuPath))
                {
                    CreateDesktopShortcut("AyuGram Desktop", ayuPath);
                }
            }
            catch { }
        }

        public static string GetInstalledZapretVersion(out string folderPath)
        {
            folderPath = "";
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (Directory.Exists(desktop))
                {
                    var dirs = Directory.GetDirectories(desktop, "zapret-discord-youtube-*")
                                    .OrderByDescending(d => d)
                                    .ToList();
                    var validDir = dirs.FirstOrDefault(d => File.Exists(Path.Combine(d, @"bin\winws.exe"))) ?? dirs.FirstOrDefault();
                    if (validDir != null)
                    {
                        string dirName = Path.GetFileName(validDir);
                        string ver = dirName.Replace("zapret-discord-youtube-", "");
                        folderPath = validDir;
                        return ver;
                    }
                }
            }
            catch { }
            return "";
        }

        public async Task AutoUpdateZapretAsync()
        {
            try
            {
                string installedVer = GetInstalledZapretVersion(out string installedFolder);
                if (string.IsNullOrEmpty(installedVer)) return;

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "SystemHub-App");
                    var response = await client.GetAsync("https://api.github.com/repos/Flowseal/zapret-discord-youtube/releases/latest");
                    if (!response.IsSuccessStatusCode) return;

                    var json = await response.Content.ReadAsStringAsync();
                    using (var doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("tag_name", out var tagProp))
                        {
                            string latestVersion = tagProp.GetString()?.Trim() ?? "";
                            if (string.IsNullOrEmpty(latestVersion)) return;

                            if (latestVersion != installedVer)
                            {
                                string downloadUrl = "";
                                if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var asset in assetsProp.EnumerateArray())
                                    {
                                        if (asset.TryGetProperty("name", out var nameProp) && 
                                            asset.TryGetProperty("browser_download_url", out var downloadProp))
                                        {
                                            var name = nameProp.GetString();
                                            if (name != null && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                            {
                                                downloadUrl = downloadProp.GetString() ?? "";
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (string.IsNullOrEmpty(downloadUrl))
                                {
                                    downloadUrl = $"https://github.com/Flowseal/zapret-discord-youtube/releases/download/{latestVersion}/zapret-discord-youtube-{latestVersion}.zip";
                                }

                                await PerformZapretUpdateAsync(latestVersion, downloadUrl, installedFolder);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Zapret auto-update error: " + ex.Message);
            }
        }

        private async Task PerformZapretUpdateAsync(string newVersion, string downloadUrl, string oldFolderPath)
        {
            try
            {
                string tempZip = Path.Combine(Path.GetTempPath(), $"zapret_{newVersion}.zip");
                string tempExtractPath = Path.Combine(Path.GetTempPath(), $"zapret_extract_{newVersion}");
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string finalInstallPath = Path.Combine(desktopPath, $"zapret-discord-youtube-{newVersion}");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "SystemHub-App");
                    using (var response = await client.GetAsync(downloadUrl))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                    }
                }

                if (Directory.Exists(tempExtractPath))
                {
                    Directory.Delete(tempExtractPath, true);
                }
                Directory.CreateDirectory(tempExtractPath);
                ZipFile.ExtractToDirectory(tempZip, tempExtractPath);

                string sourceFolder = tempExtractPath;
                var subdirs = Directory.GetDirectories(tempExtractPath);
                var files = Directory.GetFiles(tempExtractPath);
                if (subdirs.Length == 1 && files.Length == 0)
                {
                    sourceFolder = subdirs[0];
                }

                string oldCsBat = Path.Combine(oldFolderPath, "cs.bat");
                string oldCsTxt = Path.Combine(oldFolderPath, "cs.txt");
                bool hasFaceitFiles = File.Exists(oldCsBat) && File.Exists(oldCsTxt);

                bool wasRunning = IsFaceitBypassRunning();
                if (wasRunning)
                {
                    StopFaceitBypass();
                }

                if (Directory.Exists(oldFolderPath))
                {
                    Directory.Delete(oldFolderPath, true);
                }

                if (Directory.Exists(finalInstallPath))
                {
                    Directory.Delete(finalInstallPath, true);
                }
                Directory.Move(sourceFolder, finalInstallPath);

                if (hasFaceitFiles)
                {
                    File.Copy(oldCsBat, Path.Combine(finalInstallPath, "cs.bat"), true);
                    File.Copy(oldCsTxt, Path.Combine(finalInstallPath, "cs.txt"), true);

                    if (wasRunning)
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c \"{Path.Combine(finalInstallPath, "cs.bat")}\"",
                            WorkingDirectory = finalInstallPath,
                            UseShellExecute = true,
                            Verb = "runas",
                            CreateNoWindow = true,
                            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                        };
                        System.Diagnostics.Process.Start(psi);
                    }
                }

                try
                {
                    if (File.Exists(tempZip)) File.Delete(tempZip);
                    if (Directory.Exists(tempExtractPath)) Directory.Delete(tempExtractPath, true);
                }
                catch { }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PerformZapretUpdateAsync error: " + ex.Message);
            }
        }

        public static void CopyFaceitFilesToZapret()
        {
            try
            {
                string sourceDir = @"C:\Users\Basyasoo\Downloads\cs";
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                // If the source directory or files don't exist, create them
                if (!Directory.Exists(sourceDir))
                {
                    Directory.CreateDirectory(sourceDir);
                }

                string sourceBat = Path.Combine(sourceDir, "cs.bat");
                string sourceTxt = Path.Combine(sourceDir, "cs.txt");

                if (!File.Exists(sourceTxt))
                {
                    string txtContent = "  outbound and ip and\r\n  udp.DstPort>=10000 and\r\n  udp.PayloadLength=512 and\r\n  udp.Payload32[0]=0x2010000d and\r\n  udp.Payload32[4]=0x00200C00 and\r\n  udp.Payload32[5]=0 and\r\n  udp.Payload32[6]=0";
                    File.WriteAllText(sourceTxt, txtContent);
                }

                if (!File.Exists(sourceBat))
                {
                    string batContent = "@echo off\r\nchcp 65001 > nul\r\n\r\ncd /d \"%~dp0\"\r\n\r\nset \"BIN=%~dp0bin\\\"\r\ncd /d %BIN%\r\n\r\nstart \"zapret: %~n0\" /min \"%BIN%winws.exe\" --debug=1 --wf-raw-part=@\"%~dp0cs.txt\" ^\r\n--filter-udp=10000-65535 --dpi-desync=fake --dpi-desync-repeats=12 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp=\"%BIN%quic_initial_dbankcloud_ru.bin\"";
                    File.WriteAllText(sourceBat, batContent);
                }

                var dirs = Directory.GetDirectories(desktop, "zapret-discord-youtube-*");
                if (dirs.Length == 0)
                {
                    string defaultDir = Path.Combine(desktop, "zapret-discord-youtube-1.9.9a");
                    Directory.CreateDirectory(defaultDir);
                    dirs = new[] { defaultDir };
                }

                foreach (var zapretDir in dirs)
                {
                    try
                    {
                        File.Copy(sourceBat, Path.Combine(zapretDir, "cs.bat"), true);
                        File.Copy(sourceTxt, Path.Combine(zapretDir, "cs.txt"), true);

                        // Copy any missing .bin payload files to the target bin folder
                        string targetBinDir = Path.Combine(zapretDir, "bin");
                        if (Directory.Exists(targetBinDir))
                        {
                            string dbankBin = Path.Combine(targetBinDir, "quic_initial_dbankcloud_ru.bin");
                            if (!File.Exists(dbankBin))
                            {
                                string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                                if (Directory.Exists(downloads))
                                {
                                    var zapDirs = Directory.GetDirectories(downloads, "zapret-discord-youtube-*");
                                    foreach (var zDir in zapDirs)
                                    {
                                        string sourceBin = Path.Combine(zDir, "bin");
                                        if (Directory.Exists(sourceBin))
                                        {
                                            foreach (var file in Directory.GetFiles(sourceBin, "*.bin"))
                                            {
                                                try
                                                {
                                                    string destFile = Path.Combine(targetBinDir, Path.GetFileName(file));
                                                    if (!File.Exists(destFile))
                                                    {
                                                        File.Copy(file, destFile, true);
                                                    }
                                                }
                                                catch { }
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error copying Faceit files: " + ex.Message);
            }
        }

        public static bool IsFaceitBypassRunning()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-CimInstance Win32_Process -Filter 'name = ''winws.exe'' and CommandLine like ''%cs.txt%''' | Select-Object -ExpandProperty ProcessId\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    return !string.IsNullOrWhiteSpace(output);
                }
            }
            catch { }
            return false;
        }

        public static void StopFaceitBypass()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"$p = (Get-CimInstance Win32_Process -Filter 'name = ''winws.exe'' and CommandLine like ''%cs.txt%''').ProcessId; if ($p) { Stop-Process -Id $p -Force }\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                proc?.WaitForExit();
            }
            catch { }
        }
    }
}

