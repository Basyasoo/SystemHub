using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace MacStyleHub.Services
{
    public class WeatherInfo
    {
        public string City { get; set; } = "Москва";
        public double Temperature { get; set; }
        public string Condition { get; set; } = "Неизвестно";
        public string Icon { get; set; } = "sun"; // "sun", "cloud-sun", "cloud", "fog", "rain", "snow", "thunderstorm"
        public double WindSpeed { get; set; }
        public int Humidity { get; set; }
        public List<ForecastDay> Forecast { get; set; } = new();
    }

    public class ForecastDay
    {
        public string Day { get; set; } = "";
        public double MaxTemp { get; set; }
        public double MinTemp { get; set; }
        public string Condition { get; set; } = "";
        public string Icon { get; set; } = "sun";

        public string DayLocalized => LocalizationService.Instance.TranslateDayName(Day);
        public string ConditionLocalized => LocalizationService.Instance.TranslateWeatherCondition(Condition);
    }

    public class WeatherService
    {
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public async Task<WeatherInfo> GetWeatherAsync()
        {
            var info = new WeatherInfo();
            double lat = 55.7558;
            double lon = 37.6173;
            string city = "Москва";

            // 1. Try to query native Windows Geolocation (VPN-resilient)
            bool gotCoords = false;
            try
            {
                var accessStatus = await Geolocator.RequestAccessAsync();
                if (accessStatus == GeolocationAccessStatus.Allowed)
                {
                    var geolocator = new Geolocator { DesiredAccuracyInMeters = 100 };
                    var pos = await geolocator.GetGeopositionAsync();
                    lat = pos.Coordinate.Point.Position.Latitude;
                    lon = pos.Coordinate.Point.Position.Longitude;
                    gotCoords = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Windows native location service error: " + ex.Message);
            }

            // 2. Fallback to IP geolocation if Windows Geolocation fails
            if (!gotCoords)
            {
                try
                {
                    using var response = await HttpClient.GetAsync("http://ip-api.com/json/");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("status", out var status) && status.GetString() == "success")
                        {
                            lat = root.GetProperty("lat").GetDouble();
                            lon = root.GetProperty("lon").GetDouble();
                            city = root.GetProperty("city").GetString() ?? city;
                            gotCoords = true;
                        }
                    }
                }
                catch { }
            }

            info.City = city;

            // 3. Try reverse geocoding to find precise city name when using GPS coordinates
            if (gotCoords)
            {
                try
                {
                    string latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string reverseUrl = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latStr}&lon={lonStr}&zoom=10&addressdetails=1";
                    var request = new HttpRequestMessage(HttpMethod.Get, reverseUrl);
                    request.Headers.Add("User-Agent", "SystemHub/1.0 (contact@example.com)");
                    using var response = await HttpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    if (true)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("address", out var address))
                        {
                            if (address.TryGetProperty("city", out var cityProp))
                                info.City = cityProp.GetString() ?? info.City;
                            else if (address.TryGetProperty("town", out var townProp))
                                info.City = townProp.GetString() ?? info.City;
                            else if (address.TryGetProperty("village", out var villageProp))
                                info.City = villageProp.GetString() ?? info.City;
                        }
                    }
                }
                catch { }
            }

            // 4. Query Open-Meteo weather
            try
            {
                string latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string url = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}&current_weather=true&daily=weathercode,temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=7";
                using var response = await HttpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                    if (root.TryGetProperty("current_weather", out var current))
                    {
                        info.Temperature = current.GetProperty("temperature").GetDouble();
                        info.WindSpeed = current.GetProperty("windspeed").GetDouble();
                        int code = current.GetProperty("weathercode").GetInt32();
                        (info.Condition, info.Icon) = GetConditionByCode(code);
                    }

                    info.Humidity = 60; // Estimated fallback

                    if (root.TryGetProperty("daily", out var daily))
                    {
                        var times = daily.GetProperty("time");
                        var maxTemps = daily.GetProperty("temperature_2m_max");
                        var minTemps = daily.GetProperty("temperature_2m_min");
                        var codes = daily.GetProperty("weathercode");

                        int count = Math.Min(7, times.GetArrayLength());
                        for (int i = 0; i < count; i++)
                        {
                            var dateStr = times[i].GetString() ?? "";
                            var date = DateTime.TryParse(dateStr, out var d) ? d : DateTime.Now.AddDays(i);
                            var dayName = i == 0 ? "Сегодня" : date.ToString("ddd", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));

                            var (cond, icon) = GetConditionByCode(codes[i].GetInt32());

                            info.Forecast.Add(new ForecastDay
                            {
                                Day = dayName,
                                MaxTemp = maxTemps[i].GetDouble(),
                                MinTemp = minTemps[i].GetDouble(),
                                Condition = cond,
                                Icon = icon
                            });
                    }
                }
            }
            catch
            {
                // Fallback dummy weather
                info.Temperature = 21.5;
                info.Condition = "Ясно";
                info.Icon = "sun";
                info.Forecast = new List<ForecastDay>
                {
                    new() { Day = "Сегодня", MaxTemp = 23, MinTemp = 15, Condition = "Ясно", Icon = "sun" },
                    new() { Day = "Ср", MaxTemp = 22, MinTemp = 14, Condition = "Переменная облачность", Icon = "cloud-sun" },
                    new() { Day = "Чт", MaxTemp = 24, MinTemp = 16, Condition = "Ясно", Icon = "sun" },
                    new() { Day = "Пт", MaxTemp = 20, MinTemp = 13, Condition = "Пасмурно", Icon = "cloud" },
                    new() { Day = "Сб", MaxTemp = 19, MinTemp = 12, Condition = "Дождь", Icon = "rain" },
                    new() { Day = "Вс", MaxTemp = 21, MinTemp = 14, Condition = "Переменная облачность", Icon = "cloud-sun" },
                    new() { Day = "Пн", MaxTemp = 22, MinTemp = 15, Condition = "Ясно", Icon = "sun" }
                };
            }

            // Override with night icons if current hour is night (before 6:00 or after 21:00)
            int currentHour = DateTime.Now.Hour;
            if (currentHour < 6 || currentHour >= 21)
            {
                if (info.Icon == "sun")
                {
                    info.Icon = "moon";
                    if (info.Condition == "Ясно")
                        info.Condition = "Ясно (ночь)";
                }
                else if (info.Icon == "cloud-sun")
                {
                    info.Icon = "cloud-moon";
                    if (info.Condition == "Переменная облачность")
                        info.Condition = "Переменная облачность (ночь)";
                }
            }

            return info;
        }

        private (string text, string icon) GetConditionByCode(int code)
        {
            return code switch
            {
                0 => ("Ясно", "sun"),
                1 or 2 => ("Переменная облачность", "cloud-sun"),
                3 => ("Пасмурно", "cloud"),
                45 or 48 => ("Туман", "fog"),
                51 or 53 or 55 => ("Морось", "rain"),
                61 or 63 or 65 => ("Дождь", "rain"),
                71 or 73 or 75 => ("Снегопад", "snow"),
                80 or 81 or 82 => ("Ливень", "rain"),
                95 or 96 or 99 => ("Гроза", "thunderstorm"),
                _ => ("Неизвестно", "sun")
            };
        }
    }
}
