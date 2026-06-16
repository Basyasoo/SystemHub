using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemHub.Services;

namespace SystemHub.ViewModels
{
    public partial class WeatherViewModel : ViewModelBase
    {
        private readonly WeatherService _weatherService = new();

        [ObservableProperty]
        private string _city = "Загрузка...";

        [ObservableProperty]
        private double _temperature;

        [ObservableProperty]
        private string _condition = "";

        [ObservableProperty]
        private string _icon = "☀️";

        [ObservableProperty]
        private double _windSpeed;

        [ObservableProperty]
        private int _humidity;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ObservableCollection<ForecastDay> _forecast = new();

        [ObservableProperty]
        private string _clipboardText = "";

        [ObservableProperty]
        private bool _isClipboardDialogVisible;

        [ObservableProperty]
        private bool _isClipboardDataValid;

        [ObservableProperty]
        private string _searchQuery = "";

        [ObservableProperty]
        private ObservableCollection<SearchResult> _searchResults = new();

        [ObservableProperty]
        private bool _isSearchResultsVisible;

        private double? _customLat;
        private double? _customLon;
        private bool _useAutoLocation = true;

        public WeatherViewModel()
        {
            // Load saved settings
            var settings = WeatherService.LoadSettings();
            _useAutoLocation = settings.UseAutoLocation;
            _customLat = settings.Latitude;
            _customLon = settings.Longitude;
            if (!_useAutoLocation && !string.IsNullOrEmpty(settings.CustomCityName))
            {
                City = settings.CustomCityName;
            }

            LoadWeatherCommand = new AsyncRelayCommand(LoadWeatherAsync);
            _ = LoadWeatherAsync();

            // Observe language changes to refresh the localized properties and the forecast items
            LocalizationService.Instance.PropertyChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(CityLocalized));
                OnPropertyChanged(nameof(ConditionLocalized));
                OnPropertyChanged(nameof(WindSpeedLocalized));
                OnPropertyChanged(nameof(Forecast)); // Forces ItemsControl to re-bind ForecastDay elements
                
                // If language changes, reload weather to get localized city name from OSM Nominatim!
                _ = LoadWeatherAsync();
            };

        }

        public IAsyncRelayCommand LoadWeatherCommand { get; }

        public string CityLocalized => City == "Загрузка..." || City == "Loading..." || City == "加载中..."
            ? (LocalizationService.Instance.CurrentLanguage switch
              {
                  "EN" => "Loading...",
                  "ZH" => "加载中...",
                  _ => "Загрузка..."
              })
            : City;

        public string ConditionLocalized => LocalizationService.Instance.TranslateWeatherCondition(Condition);

        public string WindSpeedLocalized => $"{WindSpeed:F1} {LocalizationService.Instance.WeatherWindUnit}";

        public async Task LoadWeatherAsync()
        {
            IsLoading = true;
            try
            {
                var info = await _weatherService.GetWeatherAsync(_customLat, _customLon, _useAutoLocation ? null : City);
                City = info.City;
                Temperature = info.Temperature;
                Condition = info.Condition;
                Icon = info.Icon;
                WindSpeed = info.WindSpeed;
                Humidity = info.Humidity;

                Forecast.Clear();
                foreach (var day in info.Forecast)
                {
                    Forecast.Add(day);
                }
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(CityLocalized));
                OnPropertyChanged(nameof(ConditionLocalized));
                OnPropertyChanged(nameof(WindSpeedLocalized));
            }
        }

        [RelayCommand]
        public async Task MyLocationAsync()
        {
            // Open Yandex Maps in browser
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://yandex.ru/maps/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error opening Yandex Maps: " + ex.Message);
            }

            IsClipboardDialogVisible = true;

            // Try to auto-populate from clipboard
            try
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var clipboard = desktop.MainWindow?.Clipboard;
                    if (clipboard != null)
                    {
                        var dataTransfer = await clipboard.TryGetDataAsync();
                        if (dataTransfer != null)
                        {
                            foreach (var item in dataTransfer.Items)
                            {
                                var textObj = await item.TryGetRawAsync(Avalonia.Input.DataFormat.Text);
                                string? text = textObj as string;
                                if (!string.IsNullOrEmpty(text))
                                {
                                    var parsed = WeatherService.ParseCoordinates(text);
                                    if (parsed != null)
                                    {
                                        ClipboardText = text;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        [RelayCommand]
        public async Task ApplyClipboardLocationAsync()
        {
            if (string.IsNullOrWhiteSpace(ClipboardText)) return;
            var coords = WeatherService.ParseCoordinates(ClipboardText);
            if (coords == null) return;

            _customLat = coords.Value.Lat;
            _customLon = coords.Value.Lon;
            _useAutoLocation = false;

            IsClipboardDialogVisible = false;

            await LoadWeatherAsync();

            // Save to settings
            WeatherService.SaveSettings(new WeatherSettings
            {
                Latitude = _customLat,
                Longitude = _customLon,
                UseAutoLocation = false,
                CustomCityName = City
            });

            ClipboardText = "";
        }

        [RelayCommand]
        public void CloseClipboardDialog()
        {
            IsClipboardDialogVisible = false;
            ClipboardText = "";
        }

        partial void OnClipboardTextChanged(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                IsClipboardDataValid = false;
                return;
            }
            var parsed = WeatherService.ParseCoordinates(value);
            IsClipboardDataValid = parsed.HasValue;
        }

        [RelayCommand]
        public async Task SearchCityAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;
            IsLoading = true;
            try
            {
                var lang = LocalizationService.Instance.CurrentLanguage;
                var list = await _weatherService.SearchCityAsync(SearchQuery, lang);
                SearchResults.Clear();
                foreach (var item in list)
                {
                    SearchResults.Add(item);
                }
                IsSearchResultsVisible = SearchResults.Count > 0;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task SelectCityAsync(SearchResult result)
        {
            if (result == null) return;
            _customLat = result.Lat;
            _customLon = result.Lon;
            _useAutoLocation = false;
            IsSearchResultsVisible = false;
            SearchQuery = "";
            SearchResults.Clear();

            City = result.DisplayName;

            await LoadWeatherAsync();

            // Save to settings
            WeatherService.SaveSettings(new WeatherSettings
            {
                Latitude = _customLat,
                Longitude = _customLon,
                UseAutoLocation = false,
                CustomCityName = City
            });
        }
    }
}


