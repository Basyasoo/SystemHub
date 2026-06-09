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

        public WeatherViewModel()
        {
            LoadWeatherCommand = new AsyncRelayCommand(LoadWeatherAsync);
            _ = LoadWeatherAsync();

            // Observe language changes to refresh the localized properties and the forecast items
            LocalizationService.Instance.PropertyChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(CityLocalized));
                OnPropertyChanged(nameof(ConditionLocalized));
                OnPropertyChanged(nameof(WindSpeedLocalized));
                OnPropertyChanged(nameof(Forecast)); // Forces ItemsControl to re-bind ForecastDay elements
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
                var info = await _weatherService.GetWeatherAsync();
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
    }
}
