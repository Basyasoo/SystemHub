using System;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemHub.Services;
using SystemHub.Views;

namespace SystemHub.ViewModels
{
    public partial class TweaksViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _selectedFont = "Segoe UI";

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private bool _isRestartRequired;

        public ObservableCollection<string> AvailableFonts { get; } = new()
        {
            "Segoe UI",
            "Inter",
            "Roboto",
            "Outfit",
            "Product Sans",
            "Arial",
            "Helvetica"
        };

        public bool IsRunningAsAdmin => TweaksService.IsAdministrator();

        [RelayCommand]
        public void RunAsAdmin()
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                UseShellExecute = true,
                Verb = "runas"
            };
            try
            {
                System.Diagnostics.Process.Start(psi);
                Environment.Exit(0);
            }
            catch { }
        }

        public TweaksViewModel()
        {
            LoadCurrentStates();
            LoadWidgetsConfig();
            InitializeAppLockState();

            LocalizationService.Instance.PropertyChanged += (sender, args) =>
            {
                PopulateScreens();
            };
        }

        public void LoadCurrentStates()
        {
            SelectedFont = TweaksService.GetSystemFont();
            _isWindowsSoundsEnabled = TweaksService.AreWindowsSoundsEnabled();
            OnPropertyChanged(nameof(IsWindowsSoundsEnabled));
            PopulateScreens();
        }

        [RelayCommand]
        public async System.Threading.Tasks.Task SelectAndApplyFont()
        {
            if (!IsRunningAsAdmin)
            {
                ShowStatus(LocalizationService.Instance.TweakStatusAdminRequired);
                return;
            }

            var app = Avalonia.Application.Current;
            if (app?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = LocalizationService.Instance.ToolsFileFontPickerTitle,
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType(LocalizationService.Instance.ToolsFileFontPickerFilter)
                        {
                            Patterns = new[] { "*.ttf", "*.otf" }
                        }
                    }
                };

                try
                {
                    var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(options);
                    if (files != null && files.Count > 0)
                    {
                        string fontPath = files[0].Path.LocalPath;
                        TweaksService.ApplyCustomFont(fontPath);
                        LoadCurrentStates();
                        IsRestartRequired = true;
                        ShowStatus(LocalizationService.Instance.TweakStatusFontApplied);
                    }
                }
                catch (Exception ex)
                {
                    ShowStatus($"{LocalizationService.Instance.TweakStatusFontError}: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        public void RestoreDefaultFont()
        {
            if (!IsRunningAsAdmin)
            {
                ShowStatus(LocalizationService.Instance.TweakStatusAdminRequired);
                return;
            }

            try
            {
                TweaksService.RestoreDefaultSegoeFont();
                LoadCurrentStates();
                IsRestartRequired = true;
                ShowStatus(LocalizationService.Instance.TweakStatusFontRestored);
            }
            catch (Exception ex)
            {
                ShowStatus($"{LocalizationService.Instance.TweakStatusFontRestoreError}: {ex.Message}");
            }
        }

        [RelayCommand]
        public void RestartPc()
        {
            TweaksService.RestartComputer();
        }

        [RelayCommand]
        public void RestartExplorer()
        {
            TweaksService.RestartExplorer();
            ShowStatus(LocalizationService.Instance.TweakRestartExplorerSuccess);
        }

        private void ShowStatus(string message)
        {
            StatusMessage = message;
            System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (StatusMessage == message)
                    {
                        StatusMessage = "";
                    }
                });
            });
        }

        // Dynamic Island State
        [ObservableProperty]
        private bool _isDynamicIslandEnabled = true;

        public ObservableCollection<string> AvailableScreens { get; } = new();

        [ObservableProperty]
        private int _selectedScreenIndex = 0;

        public void PopulateScreens()
        {
            AvailableScreens.Clear();
            try
            {
                var app = Avalonia.Application.Current;
                if (app?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow != null)
                {
                    var screens = desktop.MainWindow.Screens;
                    if (screens != null)
                    {
                        int index = 1;
                        foreach (var s in screens.All)
                        {
                            string primaryText = s.IsPrimary ? LocalizationService.Instance.TweakPrimaryLabel : "";
                            AvailableScreens.Add($"{LocalizationService.Instance.TweakScreenLabel} {index}{primaryText} ({s.Bounds.Width}x{s.Bounds.Height})");
                            index++;
                        }
                    }
                }
            }
            catch { }

            if (AvailableScreens.Count == 0)
            {
                AvailableScreens.Add($"{LocalizationService.Instance.TweakScreenLabel} 1{LocalizationService.Instance.TweakPrimaryLabel}");
            }

            if (SelectedScreenIndex < 0 || SelectedScreenIndex >= AvailableScreens.Count)
            {
                SelectedScreenIndex = 0;
            }
        }

        [ObservableProperty]
        private double _dynamicIslandWidth = 220;

        [ObservableProperty]
        private double _dynamicIslandTopMargin = 10;

        [ObservableProperty]
        private bool _dynamicIslandEnableMusic = true;

        [ObservableProperty]
        private bool _dynamicIslandEnableOverheat = true;

        [ObservableProperty]
        private bool _dynamicIslandEnableFocus = true;

        [ObservableProperty]
        private bool _dynamicIslandEnableScreenshot = true;

        [ObservableProperty]
        private bool _dynamicIslandEnableVpn = true;

        [ObservableProperty]
        private bool _dynamicIslandEnableCamMic = true;

        // App Lock State
        [ObservableProperty]
        private string _appLockPassword = "1234";

        [ObservableProperty]
        private string _newAppLockPassword = "";

        public ObservableCollection<string> ProtectedApps { get; } = new();

        // System Sounds State
        [ObservableProperty]
        private bool _isWindowsSoundsEnabled;



        public void InitializeAppLockState()
        {
            AppLockPassword = AppLockService.Instance.Password;
            RefreshProtectedApps();
        }

        private void LoadWidgetsConfig()
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);
                string path = Path.Combine(appData, "widgets.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var config = System.Text.Json.JsonSerializer.Deserialize<WidgetsConfig>(json);
                    if (config != null)
                    {
                        IsDynamicIslandEnabled = config.IsDynamicIslandEnabled;
                        DynamicIslandWidth = config.DynamicIslandWidth;
                        DynamicIslandTopMargin = config.DynamicIslandTopMargin;
                        DynamicIslandEnableMusic = config.DynamicIslandEnableMusic;
                        DynamicIslandEnableOverheat = config.DynamicIslandEnableOverheat;
                        DynamicIslandEnableFocus = config.DynamicIslandEnableFocus;
                        DynamicIslandEnableScreenshot = config.DynamicIslandEnableScreenshot;
                        DynamicIslandEnableVpn = config.DynamicIslandEnableVpn;
                        DynamicIslandEnableCamMic = config.DynamicIslandEnableCamMic;
                        SelectedScreenIndex = config.SelectedScreenIndex;
                    }
                }
            }
            catch { }
        }

        private void SaveWidgetsConfig()
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);
                string path = Path.Combine(appData, "widgets.json");
                string json = System.Text.Json.JsonSerializer.Serialize(new WidgetsConfig
                {
                    IsMacDockEnabled = false,
                    IsWeatherWidgetEnabled = false,
                    IsFpsOverlayEnabled = false,
                    SelectedFpsTarget = 60,
                    IsDynamicIslandEnabled = IsDynamicIslandEnabled,
                    DynamicIslandWidth = DynamicIslandWidth,
                    DynamicIslandTopMargin = DynamicIslandTopMargin,
                    DynamicIslandEnableMusic = DynamicIslandEnableMusic,
                    DynamicIslandEnableOverheat = DynamicIslandEnableOverheat,
                    DynamicIslandEnableFocus = DynamicIslandEnableFocus,
                    DynamicIslandEnableScreenshot = DynamicIslandEnableScreenshot,
                    DynamicIslandEnableVpn = DynamicIslandEnableVpn,
                    DynamicIslandEnableCamMic = DynamicIslandEnableCamMic,
                    SelectedScreenIndex = SelectedScreenIndex
                });
                File.WriteAllText(path, json);
            }
            catch { }
        }

        partial void OnIsDynamicIslandEnabledChanged(bool value)
        {
            SaveWidgetsConfig();
            ShowStatus(LocalizationService.Instance.TweakApplied);
        }



        partial void OnDynamicIslandWidthChanged(double value) => SaveWidgetsConfig();
        partial void OnDynamicIslandTopMarginChanged(double value) => SaveWidgetsConfig();
        partial void OnDynamicIslandEnableMusicChanged(bool value) => SaveWidgetsConfig();
        partial void OnDynamicIslandEnableOverheatChanged(bool value) => SaveWidgetsConfig();
        partial void OnDynamicIslandEnableFocusChanged(bool value) => SaveWidgetsConfig();
        partial void OnDynamicIslandEnableScreenshotChanged(bool value) => SaveWidgetsConfig();
        partial void OnDynamicIslandEnableVpnChanged(bool value) => SaveWidgetsConfig();
        partial void OnDynamicIslandEnableCamMicChanged(bool value) => SaveWidgetsConfig();
        partial void OnSelectedScreenIndexChanged(int value) => SaveWidgetsConfig();

        partial void OnIsWindowsSoundsEnabledChanged(bool value)
        {
            TweaksService.SetWindowsSounds(value);
            ShowStatus(value ? LocalizationService.Instance.TweakStatusSoundsEnabled : LocalizationService.Instance.TweakStatusSoundsDisabled);
        }

        [RelayCommand]
        public void ChangeAppLockPassword(string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword)) return;
            AppLockPassword = newPassword;
            AppLockService.Instance.Password = newPassword;
            ShowStatus(LocalizationService.Instance.TweakStatusPasswordChanged);
            NewAppLockPassword = "";
        }

        [RelayCommand]
        public void AddProtectedApp(string appPath)
        {
            if (string.IsNullOrWhiteSpace(appPath)) return;
            AppLockService.Instance.AddProtectedApp(appPath);
            RefreshProtectedApps();
            ShowStatus(LocalizationService.Instance.TweakStatusAppAdded);
        }

        [RelayCommand]
        public void RemoveProtectedApp(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName)) return;
            AppLockService.Instance.RemoveProtectedApp(appName);
            RefreshProtectedApps();
            ShowStatus(LocalizationService.Instance.TweakStatusAppRemoved);
        }

        private void RefreshProtectedApps()
        {
            ProtectedApps.Clear();
            foreach (var app in AppLockService.Instance.ProtectedApps)
            {
                ProtectedApps.Add(app);
            }
        }

        public class WidgetsConfig
        {
            public bool IsMacDockEnabled { get; set; }
            public bool IsWeatherWidgetEnabled { get; set; }
            public bool IsFpsOverlayEnabled { get; set; }
            public int SelectedFpsTarget { get; set; } = 60;
            public bool IsDynamicIslandEnabled { get; set; } = true;
            public double DynamicIslandWidth { get; set; } = 220;
            public double DynamicIslandTopMargin { get; set; } = 10;
            public bool DynamicIslandEnableMusic { get; set; } = true;
            public bool DynamicIslandEnableOverheat { get; set; } = true;
            public bool DynamicIslandEnableFocus { get; set; } = true;
            public bool DynamicIslandEnableScreenshot { get; set; } = true;
            public bool DynamicIslandEnableVpn { get; set; } = true;
            public bool DynamicIslandEnableCamMic { get; set; } = true;
            public int SelectedScreenIndex { get; set; } = 0;
        }
    }
}

