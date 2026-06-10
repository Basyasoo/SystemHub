using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacStyleHub.Services;

namespace MacStyleHub.ViewModels
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
        private string _searchQuery = "";

        [ObservableProperty]
        private string _coordinatesInput = "";

        [ObservableProperty]
        private bool _isInvalidInputError;

        [ObservableProperty]
        private ObservableCollection<SearchResult> _searchResults = new();

        [ObservableProperty]
        private bool _hasSearchResults;

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
                var info = await _weatherService.GetWeatherAsync(_customLat, _customLon);
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
        public async Task SearchCityAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;
            var lang = LocalizationService.Instance.CurrentLanguage;
            var list = await _weatherService.SearchCityAsync(SearchQuery, lang);
            SearchResults.Clear();
            foreach (var item in list)
            {
                SearchResults.Add(item);
            }
            HasSearchResults = SearchResults.Count > 0;
        }

        [RelayCommand]
        public async Task SelectCityAsync(SearchResult result)
        {
            if (result == null) return;
            _customLat = result.Lat;
            _customLon = result.Lon;
            _useAutoLocation = false;

            // Save to settings
            WeatherService.SaveSettings(new WeatherSettings
            {
                Latitude = _customLat,
                Longitude = _customLon,
                UseAutoLocation = false,
                CustomCityName = result.DisplayName
            });

            // Clear search UI
            SearchQuery = "";
            SearchResults.Clear();
            HasSearchResults = false;

            await LoadWeatherAsync();
        }


        [RelayCommand]
        public async Task SetCoordinatesAsync()
        {
            IsInvalidInputError = false;
            if (string.IsNullOrWhiteSpace(CoordinatesInput)) return;

            var coords = WeatherService.ParseCoordinates(CoordinatesInput);
            if (coords == null)
            {
                IsInvalidInputError = true;
                return;
            }

            _customLat = coords.Value.Lat;
            _customLon = coords.Value.Lon;
            _useAutoLocation = false;

            await LoadWeatherAsync();

            // Save to settings
            WeatherService.SaveSettings(new WeatherSettings
            {
                Latitude = _customLat,
                Longitude = _customLon,
                UseAutoLocation = false,
                CustomCityName = City
            });

            CoordinatesInput = "";
        }

        [RelayCommand]
        public async Task ResetLocationAsync()
        {
            _customLat = null;
            _customLon = null;
            _useAutoLocation = true;

            // Save settings
            WeatherService.SaveSettings(new WeatherSettings
            {
                UseAutoLocation = true
            });

            // Clear search UI
            SearchQuery = "";
            SearchResults.Clear();
            HasSearchResults = false;
            CoordinatesInput = "";
            IsInvalidInputError = false;

            await LoadWeatherAsync();
        }
    }
}

