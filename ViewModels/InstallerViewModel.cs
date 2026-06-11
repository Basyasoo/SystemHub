using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacStyleHub.Services;
using Avalonia.Threading;

namespace MacStyleHub.ViewModels
{
    public partial class ProgramInstallItemViewModel : ObservableObject
    {
        public string Id { get; set; } = "";

        [ObservableProperty]
        private string _name = "";

        [ObservableProperty]
        private string _category = "";

        public string WingetId { get; set; } = "";

        [ObservableProperty]
        private string _description = "";

        public string IconKey { get; set; } = "";

        [ObservableProperty]
        private InstallState _state;

        [ObservableProperty]
        private int _progress;

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private bool _isSelected;

        public bool IsNotInstalled => State == InstallState.NotInstalled || State == InstallState.Failed;
        public bool IsInstalling => State == InstallState.Installing || State == InstallState.Queued;
        public bool IsInstalled => State == InstallState.Installed;

        public string StateText => State switch
        {
            InstallState.Queued => LocalizationService.Instance.InstallerStatusQueued,
            InstallState.Installing => LocalizationService.Instance.InstallerStatusInstalling,
            InstallState.Installed => LocalizationService.Instance.InstallerStatusInstalled,
            InstallState.Failed => LocalizationService.Instance.InstallerStatusFailed,
            _ => LocalizationService.Instance.InstallerStatusNotInstalled
        };

        public string ActionText => IsInstalling ? StateText : LocalizationService.Instance.InstallerBtnInstall;

        public string LocalizedStatusMessage
        {
            get
            {
                if (string.IsNullOrEmpty(StatusMessage)) return "";

                if (StatusMessage.StartsWith("Скачивание: "))
                {
                    string pct = StatusMessage.Replace("Скачивание: ", "");
                    return LocalizationService.Instance.CurrentLanguage switch
                    {
                        "EN" => $"Downloading: {pct}",
                        "ZH" => $"正在下载: {pct}",
                        _ => StatusMessage
                    };
                }
                if (StatusMessage.StartsWith("Скачивание... "))
                {
                    string size = StatusMessage.Replace("Скачивание... ", "");
                    return LocalizationService.Instance.CurrentLanguage switch
                    {
                        "EN" => $"Downloading... {size}",
                        "ZH" => $"正在下载... {size}",
                        _ => StatusMessage
                    };
                }
                if (StatusMessage.StartsWith("Ошибка: "))
                {
                    string err = StatusMessage.Replace("Ошибка: ", "");
                    return LocalizationService.Instance.CurrentLanguage switch
                    {
                        "EN" => $"Error: {err}",
                        "ZH" => $"错误: {err}",
                        _ => StatusMessage
                    };
                }
                if (StatusMessage.StartsWith("Код ошибки: "))
                {
                    string code = StatusMessage.Replace("Код ошибки: ", "");
                    return LocalizationService.Instance.CurrentLanguage switch
                    {
                        "EN" => $"Error code: {code}",
                        "ZH" => $"错误代码: {code}",
                        _ => StatusMessage
                    };
                }

                return StatusMessage switch
                {
                    "Установлено" => LocalizationService.Instance.InstallerStatusInstalled,
                    "Запуск winget..." => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Launching winget...", "ZH" => "启动 winget...", _ => "Запуск winget..." },
                    "Скачивание и подготовка..." => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Downloading & preparing...", "ZH" => "下载并准备...", _ => "Скачивание и подготовка..." },
                    "Установка приложения..." => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Installing application...", "ZH" => "正在安装应用...", _ => "Установка приложения..." },
                    "Успешно установлено!" => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Installed successfully!", "ZH" => "安装成功！", _ => "Успешно установлено!" },
                    "Уже установлено!" => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Already installed!", "ZH" => "已安装！", _ => "Уже установлено!" },
                    "Не удалось запустить winget." => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Failed to launch winget.", "ZH" => "无法启动 winget。", _ => "Не удалось запустить winget." },
                    "Скачивание мода..." => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Downloading mod...", "ZH" => "正在下载模组...", _ => "Скачивание мода..." },
                    "Распаковка архива..." => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Extracting archive...", "ZH" => "正在解压归档...", _ => "Распаковка архива..." },
                    "Копирование на рабочий стол..." => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Copying to desktop...", "ZH" => "正在复制到桌面...", _ => "Копирование на рабочий стол..." },
                    "Распаковка установщика..." => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Unpacking installer...", "ZH" => "正在释放安装程序...", _ => "Распаковка установщика..." },
                    "Скачивание Zapret..." => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Downloading Zapret...", "ZH" => "正在下载 Zapret...", _ => "Скачивание Zapret..." },
                    "Запуск установщика..." => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Launching installer...", "ZH" => "启动安装程序...", _ => "Запуск установщика..." },
                    "Успешно распаковано!" => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Extracted successfully!", "ZH" => "解压成功！", _ => "Успешно распаковано!" },
                    _ => StatusMessage
                };
            }
        }

        public void Update(InstallState state, int progress, string message)
        {
            State = state;
            Progress = progress;
            StatusMessage = message;

            if (Id == "yandexmusicmod")
            {
                Name = LocalizationService.Instance.YandexMusicModName;
                Description = LocalizationService.Instance.YandexMusicModDesc;
                Category = LocalizationService.Instance.SidebarPlayer;
            }
            else if (Id == "zapret")
            {
                Name = LocalizationService.Instance.ZapretName;
                Description = LocalizationService.Instance.ZapretDesc;
                Category = LocalizationService.Instance.CategoryUtilities;
            }
            else
            {
                Category = Category switch
                {
                    "Браузеры" or "Browsers" or "浏览器" => LocalizationService.Instance.CategoryBrowsers,
                    "Мессенджеры" or "Messengers" or "即时通讯" => LocalizationService.Instance.CategoryMessengers,
                    "Игры" or "Games" or "游戏" => LocalizationService.Instance.CategoryGames,
                    "Плееры" or "Players" or "播放器" => LocalizationService.Instance.CategoryPlayers,
                    "Утилиты" or "Utilities" or "实用工具" => LocalizationService.Instance.CategoryUtilities,
                    _ => Category
                };

                Description = Id switch
                {
                    "chrome" => LocalizationService.Instance.DescChrome,
                    "discord" => LocalizationService.Instance.DescDiscord,
                    "steam" => LocalizationService.Instance.DescSteam,
                    "vlc" => LocalizationService.Instance.DescVlc,
                    "telegram" => LocalizationService.Instance.DescTelegram,
                    "spotify" => LocalizationService.Instance.DescSpotify,
                    "7zip" => LocalizationService.Instance.Desc7Zip,
                    _ => Description
                };
            }

            OnPropertyChanged(nameof(IsNotInstalled));
            OnPropertyChanged(nameof(IsInstalling));
            OnPropertyChanged(nameof(IsInstalled));
            OnPropertyChanged(nameof(StateText));
            OnPropertyChanged(nameof(ActionText));
            OnPropertyChanged(nameof(LocalizedStatusMessage));
        }
    }

    public partial class InstallerViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<ProgramInstallItemViewModel> _programs = new();

        [ObservableProperty]
        private bool _isZapretInstructionsVisible;

        public InstallerViewModel()
        {
            var serviceProgs = InstallerService.Instance.GetPrograms();
            foreach (var p in serviceProgs)
            {
                Programs.Add(new ProgramInstallItemViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.Category,
                    WingetId = p.WingetId,
                    Description = p.Description,
                    IconKey = p.IconKey,
                    State = p.State,
                    Progress = p.Progress,
                    StatusMessage = p.StatusMessage
                });
            }

            InstallerService.Instance.ProgramStateChanged += OnProgramStateChanged;
            
            LocalizationService.Instance.PropertyChanged += (sender, args) =>
            {
                foreach (var p in Programs)
                {
                    p.Update(p.State, p.Progress, p.StatusMessage);
                }
                OnPropertyChanged(nameof(InstallerHeader));
                OnPropertyChanged(nameof(InstallerDesc));
                OnPropertyChanged(nameof(InstallerBtnScan));
                OnPropertyChanged(nameof(ZapretModalTitle));
                OnPropertyChanged(nameof(ZapretModalDesc));
                OnPropertyChanged(nameof(ZapretModalCancel));
                OnPropertyChanged(nameof(ZapretModalInstall));
            };
        }

        public string InstallerHeader => LocalizationService.Instance.InstallerHeader;
        public string InstallerDesc => LocalizationService.Instance.InstallerDesc;
        public string InstallerBtnScan => LocalizationService.Instance.InstallerBtnScan;

        private void OnProgramStateChanged(string id, InstallState state, int progress, string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var prog = Programs.FirstOrDefault(p => p.Id == id);
                if (prog != null)
                {
                    prog.Update(state, progress, message);
                }
            });
        }

        [RelayCommand]
        public void RescanInstalled()
        {
            InstallerService.Instance.ScanInstalledApps();
        }

        [RelayCommand]
        public void InstallProgram(string id)
        {
            if (id == "zapret")
            {
                IsZapretInstructionsVisible = true;
                return;
            }
            InstallerService.Instance.InstallProgram(id);
        }

        [RelayCommand]
        public void InstallSelected()
        {
            var zapretProg = Programs.FirstOrDefault(p => p.Id == "zapret");
            if (zapretProg != null && zapretProg.IsSelected && zapretProg.IsNotInstalled)
            {
                IsZapretInstructionsVisible = true;
                return;
            }

            foreach (var prog in Programs)
            {
                if (prog.IsSelected && prog.IsNotInstalled)
                {
                    InstallerService.Instance.InstallProgram(prog.Id);
                }
            }
        }

        [RelayCommand]
        public void ConfirmZapretInstall()
        {
            IsZapretInstructionsVisible = false;
            InstallerService.Instance.InstallProgram("zapret");

            // Also install other selected programs
            foreach (var prog in Programs)
            {
                if (prog.Id != "zapret" && prog.IsSelected && prog.IsNotInstalled)
                {
                    InstallerService.Instance.InstallProgram(prog.Id);
                }
            }
        }

        [RelayCommand]
        public void CancelZapretInstall()
        {
            IsZapretInstructionsVisible = false;
        }

        public string ZapretModalTitle => LocalizationService.Instance.ZapretModalTitle;
        public string ZapretModalDesc => LocalizationService.Instance.ZapretModalDesc;
        public string ZapretModalCancel => LocalizationService.Instance.WeatherLocationDialogCancel;
        public string ZapretModalInstall => LocalizationService.Instance.InstallerBtnInstall;
    }
}
