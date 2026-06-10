using System;
using System.Collections.Generic;
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

            foreach (var prog in _programs)
            {
                _states[prog.Id] = InstallState.NotInstalled;
                _progress[prog.Id] = 0;
                _statusMessages[prog.Id] = "";
            }
        }

        public List<InstallerProgram> GetPrograms()
        {
            var list = new List<InstallerProgram>();
            foreach (var p in _programs)
            {
                list.Add(new InstallerProgram
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
                });
            }
            return list;
        }

        public void InstallProgram(string id)
        {
            var prog = _programs.Find(p => p.Id == id);
            if (prog == null) return;

            if (_states[id] == InstallState.Installing || _states[id] == InstallState.Queued)
                return;

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
                        // Some exit codes indicate package is already installed (e.g. 0x8a15002b or similar)
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
    }
}
