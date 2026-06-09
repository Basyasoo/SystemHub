using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacStyleHub.Services;

namespace MacStyleHub.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly SystemInfoService _sysInfoService = new();
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _statsTimer;

        [ObservableProperty]
        private string _timeString = "";

        [ObservableProperty]
        private string _secondsString = "";

        [ObservableProperty]
        private string _dateString = "";

        [ObservableProperty]
        private string _greeting = "";

        [ObservableProperty]
        private double _cpuUsage;

        [ObservableProperty]
        private double _ramUsagePercent;

        [ObservableProperty]
        private string _ramUsedText = "";

        [ObservableProperty]
        private double _cpuSweepAngle;

        [ObservableProperty]
        private double _ramSweepAngle;

        public DashboardViewModel()
        {
            UpdateClock();
            UpdateStats();

            // Clock update (500ms)
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();

            // Stats update (1500ms)
            _statsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1500)
            };
            _statsTimer.Tick += (s, e) => UpdateStats();
            _statsTimer.Start();
        }

        [ObservableProperty]
        private int _selectedLanguageIndex = 0; // 0 = RU, 1 = EN, 2 = ZH

        [ObservableProperty]
        private bool _isDarkTheme = true; // true = Dark, false = Light

        public bool IsLangRu => SelectedLanguageIndex == 0;
        public bool IsLangEn => SelectedLanguageIndex == 1;
        public bool IsLangZh => SelectedLanguageIndex == 2;

        [RelayCommand]
        public void SetTheme(string theme)
        {
            IsDarkTheme = theme == "dark";
        }

        [RelayCommand]
        public void SetLanguage(string lang)
        {
            SelectedLanguageIndex = lang switch
            {
                "EN" => 1,
                "ZH" => 2,
                _ => 0
            };
        }

        partial void OnSelectedLanguageIndexChanged(int value)
        {
            string lang = value switch
            {
                1 => "EN",
                2 => "ZH",
                _ => "RU"
            };
            LocalizationService.Instance.SetLanguage(lang);
            UpdateClock();

            OnPropertyChanged(nameof(IsLangRu));
            OnPropertyChanged(nameof(IsLangEn));
            OnPropertyChanged(nameof(IsLangZh));
        }

        partial void OnIsDarkThemeChanged(bool value)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var app = Avalonia.Application.Current;
                if (app != null)
                {
                    app.RequestedThemeVariant = value 
                        ? Avalonia.Styling.ThemeVariant.Dark 
                        : Avalonia.Styling.ThemeVariant.Light;
                }
            });
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            TimeString = now.ToString("HH:mm");
            SecondsString = now.ToString(":ss");
            
            var culture = LocalizationService.Instance.CurrentLanguage switch
            {
                "EN" => System.Globalization.CultureInfo.GetCultureInfo("en-US"),
                "ZH" => System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),
                _ => System.Globalization.CultureInfo.GetCultureInfo("ru-RU")
            };
            DateString = now.ToString("dddd, d MMMM yyyy", culture);

            int hour = now.Hour;
            Greeting = hour switch
            {
                >= 5 and < 12 => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Good morning", "ZH" => "早上好", _ => "Доброе утро" },
                >= 12 and < 18 => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Good afternoon", "ZH" => "下午好", _ => "Добрый день" },
                >= 18 and < 23 => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Good evening", "ZH" => "晚上好", _ => "Добрый вечер" },
                _ => LocalizationService.Instance.CurrentLanguage switch { "EN" => "Good night", "ZH" => "晚安", _ => "Доброй ночи" }
            };
        }

        private void UpdateStats()
        {
            CpuUsage = Math.Round(_sysInfoService.GetCPUUsage(), 0);
            CpuSweepAngle = (CpuUsage / 100.0) * 360.0;

            var (total, used, percent) = _sysInfoService.GetRAMUsage();
            RamUsagePercent = Math.Round(percent, 0);
            RamSweepAngle = (RamUsagePercent / 100.0) * 360.0;
            
            string gbText = LocalizationService.Instance.CurrentLanguage switch
            {
                "EN" => "GB of",
                "ZH" => "GB /",
                _ => "ГБ из"
            };
            RamUsedText = $"{used:F1} {gbText} {total:F1} GB";
        }

        public void StopTimers()
        {
            _clockTimer.Stop();
            _statsTimer.Stop();
        }
    }
}
