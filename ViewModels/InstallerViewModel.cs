using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemHub.Services;
using Avalonia.Threading;

namespace SystemHub.ViewModels
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

        private Avalonia.Media.Imaging.Bitmap? _cachedIcon;
        private bool _isIconCached;

        public Avalonia.Media.Imaging.Bitmap? AppIcon
        {
            get
            {
                if (!_isIconCached)
                {
                    _cachedIcon = LoadAppIcon();
                    _isIconCached = true;
                }
                return _cachedIcon;
            }
        }

        private Avalonia.Media.Imaging.Bitmap? LoadAppIcon()
        {
            try
            {
                string suffix = IsModSelected ? "_mod" : "";
                var uri = new Uri($"avares://SystemHub/Assets/{Id}{suffix}.png");
                var assets = Avalonia.Platform.AssetLoader.Open(uri);
                return new Avalonia.Media.Imaging.Bitmap(assets);
            }
            catch
            {
                try
                {
                    var uri = new Uri($"avares://SystemHub/Assets/{Id}.png");
                    var assets = Avalonia.Platform.AssetLoader.Open(uri);
                    return new Avalonia.Media.Imaging.Bitmap(assets);
                }
                catch
                {
                    try
                    {
                        var uri = new Uri($"avares://SystemHub/Assets/{Id}.ico");
                        var assets = Avalonia.Platform.AssetLoader.Open(uri);
                        return new Avalonia.Media.Imaging.Bitmap(assets);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }

        [ObservableProperty]
        private InstallState _state;

        [ObservableProperty]
        private int _progress;

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isFaceitBypassRunning;

        public bool IsZapret => Id == "zapret";

        public string FaceitBypassButtonText => IsFaceitBypassRunning 
            ? LocalizationService.Instance.ZapretLockFaceit 
            : LocalizationService.Instance.ZapretUnlockFaceit;

        public string FaceitBypassButtonBg => IsFaceitBypassRunning 
            ? "#FF453A" 
            : "#30D158";

        partial void OnIsFaceitBypassRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(FaceitBypassButtonText));
            OnPropertyChanged(nameof(FaceitBypassButtonBg));
        }

        [ObservableProperty]
        private bool _isModSelected;

        public bool IsNotInstalled => State == InstallState.NotInstalled || State == InstallState.Failed;
        public bool IsInstalling => State == InstallState.Installing || State == InstallState.Queued;
        public bool IsInstalled => State == InstallState.Installed;
        public bool IsUpdateEnabled => IsInstalled && State != InstallState.Installing && State != InstallState.Queued;
        public bool IsActionButtonEnabled => IsNotInstalled || (Id == "zapret" && IsUpdateEnabled);
        public bool ShowStatusMessage => IsInstalling || State == InstallState.Failed;

        public bool HasVersions => Id == "spotify" || Id == "yandexmusic" || Id == "telegram";
        public string VersionLabelText => LocalizationService.Instance.VersionLabel;

        public string RegularVersionText => Id switch
        {
            "telegram" => LocalizationService.Instance.VersionOfficial,
            "spotify" => LocalizationService.Instance.VersionRegular,
            "yandexmusic" => LocalizationService.Instance.VersionRegular,
            _ => ""
        };

        public string ModVersionText => Id switch
        {
            "telegram" => LocalizationService.Instance.VersionAyuGram,
            "spotify" => LocalizationService.Instance.VersionSpotX,
            "yandexmusic" => LocalizationService.Instance.VersionMod,
            _ => ""
        };

        public string StateText => State switch
        {
            InstallState.Queued => LocalizationService.Instance.InstallerStatusQueued,
            InstallState.Installing => LocalizationService.Instance.InstallerStatusInstalling,
            InstallState.Installed => LocalizationService.Instance.InstallerStatusInstalled,
            InstallState.Failed => LocalizationService.Instance.InstallerStatusFailed,
            _ => LocalizationService.Instance.InstallerStatusNotInstalled
        };

        public string ActionText
        {
            get
            {
                if (IsInstalling) return StateText;
                if (Id == "zapret" && IsInstalled) return LocalizationService.Instance.ZapretUpdateBtn;
                return LocalizationService.Instance.InstallerBtnInstall;
            }
        }

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
                    "Запуск установщика SpotX..." => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Launching SpotX installer...", "ZH" => "启动 SpotX 安装程序...", _ => "Запуск установщика SpotX..." },
                    "Скачивание и запуск SpotX..." => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Downloading & running SpotX...", "ZH" => "下载并运行 SpotX...", _ => "Скачивание и запуск SpotX..." },
                    "Установка SpotX..." => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Installing SpotX...", "ZH" => "正在安装 SpotX...", _ => "Установка SpotX..." },
                    _ => StatusMessage
                };
            }
        }

        partial void OnIsModSelectedChanged(bool value)
        {
            _isIconCached = false;
            UpdateInfoAndState();
        }

        [RelayCommand]
        public void SelectRegular()
        {
            IsModSelected = false;
        }

        [RelayCommand]
        public void SelectMod()
        {
            IsModSelected = true;
        }

        private void UpdateInfoAndState()
        {
            if (Id == "spotify")
            {
                if (IsModSelected)
                {
                    Name = LocalizationService.Instance.SpotifyModName;
                    Description = LocalizationService.Instance.DescSpotifyMod;
                    Category = LocalizationService.Instance.CategoryPlayers;
                    WingetId = "";
                }
                else
                {
                    Name = "Spotify";
                    Description = LocalizationService.Instance.DescSpotify;
                    Category = LocalizationService.Instance.CategoryPlayers;
                    WingetId = "Spotify.Spotify";
                }
            }
            else if (Id == "yandexmusic")
            {
                if (IsModSelected)
                {
                    Name = LocalizationService.Instance.YandexMusicModName;
                    Description = LocalizationService.Instance.YandexMusicModDesc;
                    Category = LocalizationService.Instance.CategoryPlayers;
                    WingetId = "";
                }
                else
                {
                    Name = LocalizationService.Instance.YandexMusicName;
                    Description = LocalizationService.Instance.DescYandexMusic;
                    Category = LocalizationService.Instance.CategoryPlayers;
                    WingetId = "Yandex.Music";
                }
            }
            else if (Id == "telegram")
            {
                if (IsModSelected)
                {
                    Name = LocalizationService.Instance.TelegramModName;
                    Description = LocalizationService.Instance.DescTelegramMod;
                    Category = LocalizationService.Instance.CategoryMessengers;
                    WingetId = "RadolynLabs.AyuGramDesktop";
                }
                else
                {
                    Name = "Telegram Desktop";
                    Description = LocalizationService.Instance.DescTelegram;
                    Category = LocalizationService.Instance.CategoryMessengers;
                    WingetId = "Telegram.TelegramDesktop";
                }
            }

            State = InstallerService.Instance.GetState(Id, IsModSelected);
            Progress = InstallerService.Instance.GetProgress(Id, IsModSelected);
            StatusMessage = InstallerService.Instance.GetStatusMessage(Id, IsModSelected);

            OnPropertyChanged(nameof(IsNotInstalled));
            OnPropertyChanged(nameof(IsInstalling));
            OnPropertyChanged(nameof(IsInstalled));
            OnPropertyChanged(nameof(ShowStatusMessage));
            OnPropertyChanged(nameof(StateText));
            OnPropertyChanged(nameof(ActionText));
            OnPropertyChanged(nameof(LocalizedStatusMessage));
            OnPropertyChanged(nameof(AppIcon));
            OnPropertyChanged(nameof(IsUpdateEnabled));
            OnPropertyChanged(nameof(IsActionButtonEnabled));
        }

        public void Update(InstallState state, int progress, string message)
        {
            State = state;
            Progress = progress;
            StatusMessage = message;

            if (Id == "zapret")
            {
                Name = LocalizationService.Instance.ZapretName;
                Description = LocalizationService.Instance.ZapretDesc;
                Category = LocalizationService.Instance.CategoryUtilities;

                System.Threading.Tasks.Task.Run(() =>
                {
                    bool isBypassRunning = InstallerService.IsFaceitBypassRunning();

                    Dispatcher.UIThread.Post(() =>
                    {
#pragma warning disable MVVMTK0034
                        _isFaceitBypassRunning = isBypassRunning;
#pragma warning restore MVVMTK0034
                        OnPropertyChanged(nameof(IsFaceitBypassRunning));
                        OnPropertyChanged(nameof(FaceitBypassButtonText));
                        OnPropertyChanged(nameof(FaceitBypassButtonBg));
                    });
                });
            }
            else if (Id == "chrome" || Id == "yandexbrowser" || Id == "firefox" || Id == "discord" || Id == "steam" || Id == "vlc" || Id == "7zip")
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
                    "yandexbrowser" => LocalizationService.Instance.DescYandexBrowser,
                    "firefox" => LocalizationService.Instance.DescFirefox,
                    "discord" => LocalizationService.Instance.DescDiscord,
                    "steam" => LocalizationService.Instance.DescSteam,
                    "vlc" => LocalizationService.Instance.DescVlc,
                    "7zip" => LocalizationService.Instance.Desc7Zip,
                    _ => Description
                };
            }

            UpdateInfoAndState();

            OnPropertyChanged(nameof(VersionLabelText));
            OnPropertyChanged(nameof(RegularVersionText));
            OnPropertyChanged(nameof(ModVersionText));
        }
    }

    public partial class InstallerViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<ProgramInstallItemViewModel> _programs = new();

        [ObservableProperty]
        private bool _isModalVisible;

        [ObservableProperty]
        private bool _isZapretModalActive;

        [ObservableProperty]
        private bool _isVersionSelectorActive;

        private ProgramInstallItemViewModel? _activeModalProg;
        public ProgramInstallItemViewModel? ActiveModalProg
        {
            get => _activeModalProg;
            set
            {
                if (_activeModalProg != null)
                {
                    _activeModalProg.PropertyChanged -= OnActiveModalProgPropertyChanged;
                }
                if (SetProperty(ref _activeModalProg, value))
                {
                    if (_activeModalProg != null)
                    {
                        _activeModalProg.PropertyChanged += OnActiveModalProgPropertyChanged;
                    }
                    NotifyModalPropertiesChanged();
                }
            }
        }

        private void OnActiveModalProgPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyModalPropertiesChanged();
        }

        private void NotifyModalPropertiesChanged()
        {
            OnPropertyChanged(nameof(ModalTitle));
            OnPropertyChanged(nameof(ActiveModalAppName));
            OnPropertyChanged(nameof(ActiveModalAppDescription));
            OnPropertyChanged(nameof(ActiveModalVersionLabel));
            OnPropertyChanged(nameof(ActiveModalRegularText));
            OnPropertyChanged(nameof(ActiveModalModText));
            OnPropertyChanged(nameof(ActiveModalIsModSelected));
            OnPropertyChanged(nameof(IsSpotXInstructionsActive));
            OnPropertyChanged(nameof(SpotXModalInstructions));
            OnPropertyChanged(nameof(SpotXModalInstructionsHeader));
        }

        public bool IsSpotXInstructionsActive => ActiveModalProg != null && ActiveModalProg.Id == "spotify" && ActiveModalProg.IsModSelected;
        public string SpotXModalInstructions => LocalizationService.Instance.SpotXModalInstructions;
        public string SpotXModalInstructionsHeader => LocalizationService.Instance.SpotXModalInstructionsHeader;

        public string ModalTitle => IsZapretModalActive 
            ? ZapretModalTitle 
            : LocalizationService.Instance.VersionSelectorModalTitle;

        public string ActiveModalAppName => ActiveModalProg?.Name ?? "";
        public string ActiveModalAppDescription => ActiveModalProg?.Description ?? "";
        public string ActiveModalVersionLabel => ActiveModalProg?.VersionLabelText ?? "";
        public string ActiveModalRegularText => ActiveModalProg?.RegularVersionText ?? "";
        public string ActiveModalModText => ActiveModalProg?.ModVersionText ?? "";
        public bool ActiveModalIsModSelected => ActiveModalProg?.IsModSelected ?? false;

        public string ModalCancelText => LocalizationService.Instance.WeatherLocationDialogCancel;
        public string ModalInstallText => LocalizationService.Instance.InstallerBtnInstall;

        public string ZapretModalTitle => LocalizationService.Instance.ZapretModalTitle;
        public string ZapretModalDesc => LocalizationService.Instance.ZapretModalDesc;

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
                OnPropertyChanged(nameof(ModalTitle));
                OnPropertyChanged(nameof(ModalCancelText));
                OnPropertyChanged(nameof(ModalInstallText));
                NotifyModalPropertiesChanged();
            };
        }

        public string InstallerHeader => LocalizationService.Instance.InstallerHeader;
        public string InstallerDesc => LocalizationService.Instance.InstallerDesc;
        public string InstallerBtnScan => LocalizationService.Instance.InstallerBtnScan;

        private void OnProgramStateChanged(string id, InstallState state, int progress, string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                string baseId = id;
                bool isMod = false;
                if (id.EndsWith("_reg"))
                {
                    baseId = id.Substring(0, id.Length - 4);
                    isMod = false;
                }
                else if (id.EndsWith("_mod"))
                {
                    baseId = id.Substring(0, id.Length - 4);
                    isMod = true;
                }

                var prog = Programs.FirstOrDefault(p => p.Id == baseId);
                if (prog != null)
                {
                    if (id == baseId || prog.IsModSelected == isMod)
                    {
                        prog.Update(state, progress, message);
                    }
                }
            });
        }

        [RelayCommand]
        public void RescanInstalled()
        {
            System.Threading.Tasks.Task.Run(() => InstallerService.Instance.ScanInstalledApps());
        }

        [RelayCommand]
        public void InstallProgram(ProgramInstallItemViewModel prog)
        {
            if (prog == null) return;
            ActiveModalProg = prog;
            if (prog.Id == "zapret")
            {
                IsZapretModalActive = true;
                IsVersionSelectorActive = false;
                IsModalVisible = true;
            }
            else if (prog.HasVersions)
            {
                IsZapretModalActive = false;
                IsVersionSelectorActive = true;
                IsModalVisible = true;
            }
            else
            {
                InstallerService.Instance.InstallProgram(prog.Id, prog.IsModSelected);
            }
        }

        [RelayCommand]
        public void InstallSelected()
        {
            var zapretProg = Programs.FirstOrDefault(p => p.Id == "zapret");
            if (zapretProg != null && zapretProg.IsSelected && zapretProg.IsNotInstalled)
            {
                InstallProgram(zapretProg);
                return;
            }

            foreach (var prog in Programs)
            {
                if (prog.IsSelected && prog.IsNotInstalled)
                {
                    InstallerService.Instance.InstallProgram(prog.Id, prog.IsModSelected);
                }
            }
        }

        [RelayCommand]
        public void ConfirmModalInstall()
        {
            IsModalVisible = false;
            if (ActiveModalProg != null)
            {
                InstallerService.Instance.InstallProgram(ActiveModalProg.Id, ActiveModalProg.IsModSelected);

                if (ActiveModalProg.Id == "zapret")
                {
                    foreach (var prog in Programs)
                    {
                        if (prog.Id != "zapret" && prog.IsSelected && prog.IsNotInstalled)
                        {
                            InstallerService.Instance.InstallProgram(prog.Id, prog.IsModSelected);
                        }
                    }
                }
            }
        }

        [RelayCommand]
        public void CancelModal()
        {
            IsModalVisible = false;
        }

        [RelayCommand]
        public void SelectModalRegular()
        {
            if (ActiveModalProg != null)
            {
                ActiveModalProg.IsModSelected = false;
            }
        }

        [RelayCommand]
        public void SelectModalMod()
        {
            if (ActiveModalProg != null)
            {
                ActiveModalProg.IsModSelected = true;
            }
        }

        [RelayCommand]
        public void ToggleFaceitBypass(ProgramInstallItemViewModel prog)
        {
            if (prog == null || prog.Id != "zapret") return;

            if (prog.IsFaceitBypassRunning)
            {
                InstallerService.StopFaceitBypass();
                prog.IsFaceitBypassRunning = false;
                prog.StatusMessage = "Обход Faceit отключен.";
            }
            else
            {
                InstallerService.CopyFaceitFilesToZapret();

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string zapretDir = "";
                if (System.IO.Directory.Exists(desktop))
                {
                    var dirs = System.IO.Directory.GetDirectories(desktop, "zapret-discord-youtube-*")
                                    .OrderByDescending(d => d)
                                    .ToList();
                    var validDir = dirs.FirstOrDefault(d => System.IO.File.Exists(System.IO.Path.Combine(d, @"bin\winws.exe")));
                    if (validDir != null)
                    {
                        zapretDir = validDir;
                    }
                    else if (dirs.Count > 0)
                    {
                        zapretDir = dirs[0];
                    }
                }
                if (string.IsNullOrEmpty(zapretDir))
                {
                    zapretDir = System.IO.Path.Combine(desktop, "zapret-discord-youtube-1.9.9a");
                }
                string csBatPath = System.IO.Path.Combine(zapretDir, "cs.bat");

                if (!System.IO.File.Exists(csBatPath))
                {
                    prog.StatusMessage = "Файлы cs.bat/cs.txt не найдены в Downloads/cs!";
                    return;
                }

                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{csBatPath}\"",
                        WorkingDirectory = System.IO.Path.GetDirectoryName(csBatPath),
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                    };
                    System.Diagnostics.Process.Start(psi);
                    prog.IsFaceitBypassRunning = true;
                    prog.StatusMessage = "Сервера Faceit разблокированы!";
                }
                catch (Exception ex)
                {
                    prog.StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        public void UpdateZapret(ProgramInstallItemViewModel prog)
        {
            if (prog == null || prog.Id != "zapret") return;

            // Trigger the installation process, which queries GitHub and downloads/installs the latest version
            InstallerService.Instance.InstallProgram("zapret", false);
        }

        [RelayCommand]
        public void ExecuteAction(ProgramInstallItemViewModel prog)
        {
            if (prog == null) return;
            if (prog.Id == "zapret" && prog.IsInstalled)
            {
                UpdateZapret(prog);
            }
            else
            {
                InstallProgram(prog);
            }
        }
    }
}

