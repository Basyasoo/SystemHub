using System;
using System.Collections.Generic;
using System.IO;
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

    public class SearchResult
    {
        public string DisplayName { get; set; } = "";
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public class WeatherSettings
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string CustomCityName { get; set; } = "";
        public bool UseAutoLocation { get; set; } = true;
    }

    public class WeatherService
    {
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        private static string GetSettingsPath()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
            Directory.CreateDirectory(appData);
            return Path.Combine(appData, "weather_settings.json");
        }

        public static WeatherSettings LoadSettings()
        {
            try
            {
                var path = GetSettingsPath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<WeatherSettings>(json) ?? new WeatherSettings();
                }
            }
            catch { }
            return new WeatherSettings();
        }

        public static void SaveSettings(WeatherSettings settings)
        {
            try
            {
                var path = GetSettingsPath();
                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        public async Task<List<SearchResult>> SearchCityAsync(string query, string lang)
        {
            var results = new List<SearchResult>();
            if (string.IsNullOrWhiteSpace(query)) return results;

            try
            {
                string encodedQuery = Uri.EscapeDataString(query);
                string url = $"https://nominatim.openstreetmap.org/search?q={encodedQuery}&format=json&addressdetails=1&limit=6&accept-language={lang.ToLower()}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "SystemHub/1.0 (contact@example.com)");
                using var response = await HttpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        string displayName = "";
                        string settlement = "";
                        string state = "";

                        if (item.TryGetProperty("address", out var address))
                        {
                            displayName = FormatCityName(address, lang.ToLower(), item.GetProperty("display_name").GetString() ?? "");
                        }
                        else
                        {
                            displayName = item.GetProperty("display_name").GetString() ?? "";
                        }

                        double resLat = double.Parse(item.GetProperty("lat").GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                        double resLon = double.Parse(item.GetProperty("lon").GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture);

                        results.Add(new SearchResult
                        {
                            DisplayName = displayName,
                            Lat = resLat,
                            Lon = resLon
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SearchCityAsync error: " + ex.Message);
            }
            return results;
        }

        public async Task<WeatherInfo> GetWeatherAsync(double? customLat = null, double? customLon = null)
        {
            var info = new WeatherInfo();
            double lat = 55.7558;
            double lon = 37.6173;
            string city = "Москва";
            bool gotCoords = false;
            string langCode = LocalizationService.Instance.CurrentLanguage.ToLower();

            if (customLat.HasValue && customLon.HasValue)
            {
                lat = customLat.Value;
                lon = customLon.Value;
                gotCoords = true;
            }
            else
            {
                // 1. Try to query native Windows Geolocation
                try
                {
                    var geolocator = new Geolocator { DesiredAccuracyInMeters = 100 };
                    // Specify maximum age (10 minutes) and timeout (5 seconds) to avoid hanging
                    var pos = await geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(5));
                    lat = pos.Coordinate.Point.Position.Latitude;
                    lon = pos.Coordinate.Point.Position.Longitude;
                    gotCoords = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Windows native location service error: " + ex.Message);
                }

                // 2. Fallback to IP geolocation if Windows Geolocation fails
                if (!gotCoords)
                {
                    string ipLang = langCode;
                    if (ipLang == "zh") ipLang = "zh-CN";

                    // Try Provider 1: ipwho.is (very accurate database for Russian regional pool allocations)
                    try
                    {
                        using var response = await HttpClient.GetAsync($"https://ipwho.is/?lang={ipLang}");
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("success", out var success) && success.GetBoolean())
                            {
                                lat = root.GetProperty("latitude").GetDouble();
                                lon = root.GetProperty("longitude").GetDouble();
                                city = root.GetProperty("city").GetString() ?? city;
                                
                                string regionName = root.TryGetProperty("region", out var regProp) ? regProp.GetString() : null;
                                if (!string.IsNullOrEmpty(regionName))
                                {
                                    string cleanRegion = regionName;
                                    if (ipLang == "ru")
                                    {
                                        if (cleanRegion.EndsWith(" область", StringComparison.OrdinalIgnoreCase))
                                            cleanRegion = cleanRegion.Substring(0, cleanRegion.Length - 8) + " обл.";
                                        else if (cleanRegion.EndsWith(" край", StringComparison.OrdinalIgnoreCase))
                                            cleanRegion = cleanRegion.Substring(0, cleanRegion.Length - 5) + " кр.";
                                        else if (cleanRegion.EndsWith(" республика", StringComparison.OrdinalIgnoreCase))
                                            cleanRegion = "Респ. " + cleanRegion.Substring(0, cleanRegion.Length - 11);
                                        else if (cleanRegion.StartsWith("республика ", StringComparison.OrdinalIgnoreCase))
                                            cleanRegion = "Респ. " + cleanRegion.Substring(11);
                                    }
                                    
                                    if (!city.Contains(cleanRegion))
                                    {
                                        city += $", {cleanRegion}";
                                    }
                                }
                                gotCoords = true;
                            }
                        }
                    }
                    catch { }

                    // Try Provider 2: ip-api.com (if ipwho.is failed)
                    if (!gotCoords)
                    {
                        try
                        {
                            using var response = await HttpClient.GetAsync($"http://ip-api.com/json/?lang={ipLang}");
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
                                    
                                    string regionName = root.TryGetProperty("regionName", out var regProp) ? regProp.GetString() : null;
                                    if (!string.IsNullOrEmpty(regionName))
                                    {
                                        string cleanRegion = regionName;
                                        if (ipLang == "ru")
                                        {
                                            if (cleanRegion.EndsWith(" область", StringComparison.OrdinalIgnoreCase))
                                                cleanRegion = cleanRegion.Substring(0, cleanRegion.Length - 8) + " обл.";
                                            else if (cleanRegion.EndsWith(" край", StringComparison.OrdinalIgnoreCase))
                                                cleanRegion = cleanRegion.Substring(0, cleanRegion.Length - 5) + " кр.";
                                            else if (cleanRegion.EndsWith(" республика", StringComparison.OrdinalIgnoreCase))
                                                cleanRegion = "Респ. " + cleanRegion.Substring(0, cleanRegion.Length - 11);
                                            else if (cleanRegion.StartsWith("республика ", StringComparison.OrdinalIgnoreCase))
                                                cleanRegion = "Респ. " + cleanRegion.Substring(11);
                                        }
                                        
                                        if (!city.Contains(cleanRegion))
                                        {
                                            city += $", {cleanRegion}";
                                        }
                                    }
                                    gotCoords = true;
                                }
                            }
                        }
                        catch { }
                    }

                    // Try Provider 3: freeipapi.com
                    if (!gotCoords)
                    {
                        try
                        {
                            using var response = await HttpClient.GetAsync("https://freeipapi.com/api/json");
                            if (response.IsSuccessStatusCode)
                            {
                                var json = await response.Content.ReadAsStringAsync();
                                using var doc = JsonDocument.Parse(json);
                                var root = doc.RootElement;
                                lat = root.GetProperty("latitude").GetDouble();
                                lon = root.GetProperty("longitude").GetDouble();
                                city = root.GetProperty("cityName").GetString() ?? city;
                                gotCoords = true;
                            }
                        }
                        catch { }
                    }
                }
            }

            info.City = city;

            // 3. Try reverse geocoding to find precise city + region name
            if (gotCoords)
            {
                bool reverseGeocoded = false;
                string latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                
                string acceptLang = langCode;
                if (acceptLang == "zh") acceptLang = "zh-CN";
                else if (acceptLang == "ru") acceptLang = "ru,ru-RU";
                else acceptLang = "en-US,en";

                // Try Nominatim first
                try
                {
                    string reverseUrl = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latStr}&lon={lonStr}&zoom=14&addressdetails=1&accept-language={acceptLang}";
                    var request = new HttpRequestMessage(HttpMethod.Get, reverseUrl);
                    request.Headers.Add("User-Agent", "SystemHub/1.0 (contact@example.com)");
                    using var response = await HttpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        if (!json.Contains("Access denied"))
                        {
                            using var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("address", out var address))
                            {
                                city = FormatCityName(address, langCode, city);
                                reverseGeocoded = true;
                            }
                        }
                    }
                }
                catch { }

                // Fallback to BigDataCloud reverse geocoding
                if (!reverseGeocoded)
                {
                    try
                    {
                        string bdcUrl = $"https://api.bigdatacloud.net/data/reverse-geocode-client?latitude={latStr}&longitude={lonStr}&localityLanguage={langCode}";
                        using var response = await HttpClient.GetAsync(bdcUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;
                            string tempCity = "";

                            if (root.TryGetProperty("city", out var cityProp) && !string.IsNullOrEmpty(cityProp.GetString()))
                            {
                                tempCity = cityProp.GetString()!;
                            }
                            else if (root.TryGetProperty("locality", out var locProp) && !string.IsNullOrEmpty(locProp.GetString()))
                            {
                                tempCity = locProp.GetString()!;
                            }

                            if (langCode == "ru" && !string.IsNullOrEmpty(tempCity))
                            {
                                string[] prefixes = new[] {
                                    "Городской округ ЗАТО город ",
                                    "Городской округ город ",
                                    "Городской округ ",
                                    "городской округ ",
                                    "город ",
                                    "городское поселение ",
                                    "сельское поселение "
                                };
                                foreach (var prefix in prefixes)
                                {
                                    if (tempCity.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                    {
                                        tempCity = "г. " + tempCity.Substring(prefix.Length);
                                        break;
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(tempCity))
                            {
                                city = tempCity;

                                if (root.TryGetProperty("principalSubdivision", out var subdivisionProp))
                                {
                                    var subName = subdivisionProp.GetString();
                                    if (!string.IsNullOrEmpty(subName) && !string.Equals(subName, tempCity, StringComparison.OrdinalIgnoreCase))
                                    {
                                        string cleanSub = subName;
                                        if (langCode == "ru")
                                        {
                                            if (cleanSub.EndsWith(" область", StringComparison.OrdinalIgnoreCase))
                                                cleanSub = cleanSub.Substring(0, cleanSub.Length - 8) + " обл.";
                                            else if (cleanSub.EndsWith(" край", StringComparison.OrdinalIgnoreCase))
                                                cleanSub = cleanSub.Substring(0, cleanSub.Length - 5) + " кр.";
                                            else if (cleanSub.EndsWith(" республика", StringComparison.OrdinalIgnoreCase))
                                                cleanSub = "Респ. " + cleanSub.Substring(0, cleanSub.Length - 11);
                                            else if (cleanSub.StartsWith("республика ", StringComparison.OrdinalIgnoreCase))
                                                cleanSub = "Респ. " + cleanSub.Substring(11);
                                        }

                                        if (!city.Contains(cleanSub))
                                        {
                                            city += $", {cleanSub}";
                                        }
                                    }
                                }
                                reverseGeocoded = true;
                            }
                        }
                    }
                    catch { }
                }
            }

            info.City = city;

            // 4. Query Open-Meteo weather
            try
            {
                string latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string url = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=7";
                using var response = await HttpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("current", out var current))
                    {
                        info.Temperature = current.GetProperty("temperature_2m").GetRawText().Contains('.')
                            ? current.GetProperty("temperature_2m").GetDouble()
                            : current.GetProperty("temperature_2m").GetInt32();
                        info.WindSpeed = current.GetProperty("wind_speed_10m").GetRawText().Contains('.')
                            ? current.GetProperty("wind_speed_10m").GetDouble()
                            : current.GetProperty("wind_speed_10m").GetInt32();
                        info.Humidity = current.GetProperty("relative_humidity_2m").GetInt32();

                        int code = current.GetProperty("weather_code").GetInt32();
                        (info.Condition, info.Icon) = GetConditionByWmoCode(code);
                    }

                    if (root.TryGetProperty("daily", out var daily))
                    {
                        var timeArray = daily.GetProperty("time");
                        var maxTempArray = daily.GetProperty("temperature_2m_max");
                        var minTempArray = daily.GetProperty("temperature_2m_min");
                        var codeArray = daily.GetProperty("weather_code");

                        int count = timeArray.GetArrayLength();
                        info.Forecast.Clear();
                        for (int i = 0; i < count; i++)
                        {
                            var dateStr = timeArray[i].GetString() ?? "";
                            var date = DateTime.TryParse(dateStr, out var d) ? d : DateTime.Now.AddDays(i);

                            var culture = LocalizationService.Instance.CurrentLanguage switch
                            {
                                "EN" => System.Globalization.CultureInfo.GetCultureInfo("en-US"),
                                "ZH" => System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),
                                _ => System.Globalization.CultureInfo.GetCultureInfo("ru-RU")
                            };
                            var dayName = i == 0 ? "Сегодня" : date.ToString("ddd", culture);

                            double maxTemp = maxTempArray[i].GetRawText().Contains('.') ? maxTempArray[i].GetDouble() : maxTempArray[i].GetInt32();
                            double minTemp = minTempArray[i].GetRawText().Contains('.') ? minTempArray[i].GetDouble() : minTempArray[i].GetInt32();
                            int code = codeArray[i].GetInt32();
                            var (cond, icon) = GetConditionByWmoCode(code);

                            info.Forecast.Add(new ForecastDay
                            {
                                Day = dayName,
                                MaxTemp = maxTemp,
                                MinTemp = minTemp,
                                Condition = cond,
                                Icon = icon
                            });
                        }
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
                    if (info.Condition == "Переменная облачность")
                        info.Condition = "Переменная облачность (ночь)";
                }
            }

            return info;
        }

        private (string text, string icon) GetConditionByWmoCode(int code)
        {
            return code switch
            {
                0 => ("Ясно", "sun"),
                1 or 2 => ("Переменная облачность", "cloud-sun"),
                3 => ("Пасмурно", "cloud"),
                45 or 48 => ("Туман", "fog"),
                51 or 53 or 55 => ("Морось", "rain"),
                56 or 57 => ("Морось", "rain"), // Freezing drizzle
                61 or 63 or 65 => ("Дождь", "rain"),
                66 or 67 => ("Дождь", "rain"), // Freezing rain
                71 or 73 or 75 => ("Снегопад", "snow"),
                77 => ("Снегопад", "snow"),
                80 or 81 or 82 => ("Ливень", "rain"),
                85 or 86 => ("Снегопад", "snow"),
                95 => ("Гроза", "thunderstorm"),
                96 or 99 => ("Гроза", "thunderstorm"),
                _ => ("Ясно", "sun")
            };
        }

        private static string FormatCityName(JsonElement address, string langCode, string defaultCityName)
        {
            string settlement = null;
            string typePrefix = "";

            if (address.TryGetProperty("isolated_dwelling", out var idProp))
            {
                settlement = idProp.GetString();
                typePrefix = langCode == "ru" ? "х. " : "";
            }
            else if (address.TryGetProperty("hamlet", out var hamletProp))
            {
                settlement = hamletProp.GetString();
                typePrefix = langCode == "ru" ? "д. " : "";
            }
            else if (address.TryGetProperty("village", out var villageProp))
            {
                settlement = villageProp.GetString();
                typePrefix = langCode == "ru" ? "с. " : "";
            }
            else if (address.TryGetProperty("suburb", out var suburbProp))
            {
                settlement = suburbProp.GetString();
                typePrefix = langCode == "ru" ? "мкр. " : "";
            }
            else if (address.TryGetProperty("town", out var townProp))
            {
                settlement = townProp.GetString();
                typePrefix = langCode == "ru" ? "г. " : "";
            }
            else if (address.TryGetProperty("city", out var cityProp))
            {
                settlement = cityProp.GetString();
                typePrefix = langCode == "ru" ? "г. " : "";
            }
            else if (address.TryGetProperty("allotments", out var allotmentsProp))
            {
                settlement = allotmentsProp.GetString();
                typePrefix = langCode == "ru" ? "снт. " : "";
            }
            else if (address.TryGetProperty("croft", out var croftProp))
            {
                settlement = croftProp.GetString();
                typePrefix = langCode == "ru" ? "х. " : "";
            }
            else if (address.TryGetProperty("farm", out var farmProp))
            {
                settlement = farmProp.GetString();
                typePrefix = langCode == "ru" ? "х. " : "";
            }
            else if (address.TryGetProperty("locality", out var localityProp))
            {
                settlement = localityProp.GetString();
                typePrefix = "";
            }
            else if (address.TryGetProperty("municipality", out var municipalityProp))
            {
                settlement = municipalityProp.GetString();
                typePrefix = "";
            }
            else if (address.TryGetProperty("county", out var countyProp))
            {
                settlement = countyProp.GetString();
                typePrefix = "";
            }
            else if (address.TryGetProperty("district", out var districtProp))
            {
                settlement = districtProp.GetString();
                typePrefix = "";
            }
            else if (address.TryGetProperty("state_district", out var sdProp))
            {
                settlement = sdProp.GetString();
                typePrefix = "";
            }

            if (string.IsNullOrEmpty(settlement))
            {
                return defaultCityName;
            }

            string cleanSettlement = settlement;
            if (langCode == "ru")
            {
                if (cleanSettlement.StartsWith("рабочий поселок ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(16); typePrefix = "р.п. "; }
                else if (cleanSettlement.StartsWith("рабочий посёлок ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(16); typePrefix = "р.п. "; }
                else if (cleanSettlement.StartsWith("р.п. ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(5); typePrefix = "р.п. "; }
                else if (cleanSettlement.StartsWith("р.п ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(4); typePrefix = "р.п. "; }
                else if (cleanSettlement.StartsWith("курортный поселок ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(18); typePrefix = "к.п. "; }
                else if (cleanSettlement.StartsWith("курортный посёлок ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(18); typePrefix = "к.п. "; }
                else if (cleanSettlement.StartsWith("дачный поселок ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(15); typePrefix = "д.п. "; }
                else if (cleanSettlement.StartsWith("дачный посёлок ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(15); typePrefix = "д.п. "; }
                else if (cleanSettlement.StartsWith("поселок городского типа ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(24); typePrefix = "пгт. "; }
                else if (cleanSettlement.StartsWith("посёлок городского типа ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(24); typePrefix = "пгт. "; }
                else if (cleanSettlement.StartsWith("город ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(6); typePrefix = "г. "; }
                else if (cleanSettlement.StartsWith("г. ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(3); typePrefix = "г. "; }
                else if (cleanSettlement.StartsWith("г ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(2); typePrefix = "г. "; }
                else if (cleanSettlement.StartsWith("село ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(5); typePrefix = "с. "; }
                else if (cleanSettlement.StartsWith("с. ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(3); typePrefix = "с. "; }
                else if (cleanSettlement.StartsWith("с ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(2); typePrefix = "с. "; }
                else if (cleanSettlement.StartsWith("деревня ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(8); typePrefix = "д. "; }
                else if (cleanSettlement.StartsWith("д. ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(3); typePrefix = "д. "; }
                else if (cleanSettlement.StartsWith("д ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(2); typePrefix = "д. "; }
                else if (cleanSettlement.StartsWith("поселок ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(8); typePrefix = "п. "; }
                else if (cleanSettlement.StartsWith("посёлок ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(8); typePrefix = "п. "; }
                else if (cleanSettlement.StartsWith("п. ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(3); typePrefix = "п. "; }
                else if (cleanSettlement.StartsWith("п ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(2); typePrefix = "п. "; }
                else if (cleanSettlement.StartsWith("пгт ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(4); typePrefix = "пгт. "; }
                else if (cleanSettlement.StartsWith("пгт. ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(5); typePrefix = "пгт. "; }
                else if (cleanSettlement.StartsWith("хутор ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(6); typePrefix = "х. "; }
                else if (cleanSettlement.StartsWith("х. ", StringComparison.OrdinalIgnoreCase)) { cleanSettlement = cleanSettlement.Substring(3); typePrefix = "х. "; }
            }

            string formattedCity = typePrefix + cleanSettlement;

            string? parent = null;
            if (address.TryGetProperty("town", out var tProp) && !string.Equals(tProp.GetString(), settlement, StringComparison.OrdinalIgnoreCase))
                parent = tProp.GetString();
            else if (address.TryGetProperty("city", out var cProp) && !string.Equals(cProp.GetString(), settlement, StringComparison.OrdinalIgnoreCase))
                parent = cProp.GetString();
            else if (address.TryGetProperty("county", out var coProp) && !string.Equals(coProp.GetString(), settlement, StringComparison.OrdinalIgnoreCase))
                parent = coProp.GetString();

            if (!string.IsNullOrEmpty(parent))
            {
                formattedCity += $" ({parent})";
            }

            if (address.TryGetProperty("state", out var stateProp))
            {
                string? state = stateProp.GetString();
                if (!string.IsNullOrEmpty(state) && 
                    !string.Equals(state, settlement, StringComparison.OrdinalIgnoreCase) && 
                    (parent == null || !string.Equals(state, parent, StringComparison.OrdinalIgnoreCase)))
                {
                    string cleanState = state;
                    if (langCode == "ru")
                    {
                        if (cleanState.EndsWith(" область", StringComparison.OrdinalIgnoreCase))
                            cleanState = cleanState.Substring(0, cleanState.Length - 8) + " обл.";
                        else if (cleanState.EndsWith(" край", StringComparison.OrdinalIgnoreCase))
                            cleanState = cleanState.Substring(0, cleanState.Length - 5) + " кр.";
                        else if (cleanState.EndsWith(" республика", StringComparison.OrdinalIgnoreCase))
                            cleanState = "Респ. " + cleanState.Substring(0, cleanState.Length - 11);
                        else if (cleanState.StartsWith("республика ", StringComparison.OrdinalIgnoreCase))
                            cleanState = "Респ. " + cleanState.Substring(11);
                    }

                    if (!formattedCity.Contains(cleanState))
                    {
                        formattedCity += $", {cleanState}";
                    }
                }
            }

            return formattedCity;
        }

        public static (double Lat, double Lon)? ParseCoordinates(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            // 1. Direct Lat, Lon numbers (e.g. "54.9231, 43.3427")
            var directMatch = System.Text.RegularExpressions.Regex.Match(input, @"^\s*(-?\d+(?:\.\d+)?)[,\s;]+(-?\d+(?:\.\d+)?)\s*$");
            if (directMatch.Success)
            {
                if (double.TryParse(directMatch.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(directMatch.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lon))
                {
                    return (lat, lon);
                }
            }

            // 2. Google Maps @lat,lon format
            var googleAtMatch = System.Text.RegularExpressions.Regex.Match(input, @"@(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)");
            if (googleAtMatch.Success)
            {
                if (double.TryParse(googleAtMatch.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(googleAtMatch.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lon))
                {
                    return (lat, lon);
                }
            }

            // 3. Google Maps q=lat,lon format
            var googleQMatch = System.Text.RegularExpressions.Regex.Match(input, @"[?&]q=(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)");
            if (googleQMatch.Success)
            {
                if (double.TryParse(googleQMatch.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(googleQMatch.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lon))
                {
                    return (lat, lon);
                }
            }

            // 4. Yandex Maps ll=lon,lat format (note: longitude first!)
            var yandexLlMatch = System.Text.RegularExpressions.Regex.Match(input, @"[?&]ll=(-?\d+(?:\.\d+)?)(?:%2C|,)(-?\d+(?:\.\d+)?)");
            if (yandexLlMatch.Success)
            {
                if (double.TryParse(yandexLlMatch.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(yandexLlMatch.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lon))
                {
                    return (lat, lon);
                }
            }

            // 5. 2GIS /geo/lat,lon format
            var dgisGeoMatch = System.Text.RegularExpressions.Regex.Match(input, @"/geo/(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)");
            if (dgisGeoMatch.Success)
            {
                if (double.TryParse(dgisGeoMatch.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(dgisGeoMatch.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lon))
                {
                    return (lat, lon);
                }
            }

            // 6. 2GIS /center/lon,lat format (note: longitude first!)
            var dgisCenterMatch = System.Text.RegularExpressions.Regex.Match(input, @"/center/(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)");
            if (dgisCenterMatch.Success)
            {
                if (double.TryParse(dgisCenterMatch.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(dgisCenterMatch.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lon))
                {
                    return (lat, lon);
                }
            }

            return null;
        }
    }
}
