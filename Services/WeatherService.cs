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
        public bool IsExact { get; set; }
    }

    public class WeatherSettings
    {
        public double? Latitude { get; set; } = 55.7558;
        public double? Longitude { get; set; } = 37.6173;
        public string CustomCityName { get; set; } = "Москва";
        public bool UseAutoLocation { get; set; } = false;
    }

    public class WeatherService
    {
        private static readonly HttpClient HttpClient;

        static WeatherService()
        {
            HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MacStyleHubWeatherClient/1.2 (contact.basyasoo.weather@localmail.net)");
        }

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

            string langCode = lang.ToLower();
            string acceptLang = langCode;
            if (acceptLang == "zh") acceptLang = "zh-CN";
            else if (acceptLang == "ru") acceptLang = "ru,ru-RU";
            else acceptLang = "en-US,en";

            // 1. Try Nominatim search first (with a short timeout of 2.0 seconds) to get rich district and settlement details
            try
            {
                string encodedQuery = Uri.EscapeDataString(query);
                string[] urls = {
                    $"https://nominatim.openstreetmap.org/search?q={encodedQuery}&format=json&accept-language={acceptLang}&addressdetails=1&extratags=1&limit=8",
                    $"http://nominatim.openstreetmap.org/search?q={encodedQuery}&format=json&accept-language={acceptLang}&addressdetails=1&extratags=1&limit=8"
                };

                string? json = null;
                foreach (var url in urls)
                {
                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, url);
                        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2.0));
                        using var response = await HttpClient.SendAsync(request, cts.Token);
                        if (response.IsSuccessStatusCode)
                        {
                            json = await response.Content.ReadAsStringAsync();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Nominatim search failed for {url}: {ex.Message}");
                    }
                }

                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("address", out var address))
                        {
                            string? settlement = null;
                            string typePrefix = "";

                            string? addresstype = item.TryGetProperty("addresstype", out var atProp) ? atProp.GetString() : null;
                            if (string.IsNullOrEmpty(addresstype))
                            {
                                addresstype = item.TryGetProperty("type", out var tProp) ? tProp.GetString() : null;
                            }

                            if (langCode == "ru" && !string.IsNullOrEmpty(addresstype))
                            {
                                if (addresstype == "city" || addresstype == "town") typePrefix = "г. ";
                                else if (addresstype == "village") typePrefix = "с. ";
                                else if (addresstype == "hamlet") typePrefix = "д. ";
                                else if (addresstype == "suburb") typePrefix = "мкр. ";
                                else if (addresstype == "isolated_dwelling" || addresstype == "croft" || addresstype == "farm") typePrefix = "х. ";
                                else if (addresstype == "allotments") typePrefix = "снт. ";
                            }

                            if (!string.IsNullOrEmpty(addresstype) && address.TryGetProperty(addresstype, out var settProp))
                            {
                                settlement = settProp.GetString();
                            }

                            if (string.IsNullOrEmpty(settlement))
                            {
                                settlement = item.TryGetProperty("name", out var nProp) ? nProp.GetString() : query;
                            }

                            string? officialStatus = null;
                            if (item.TryGetProperty("extratags", out var extra) && extra.TryGetProperty("official_status", out var statusProp))
                            {
                                officialStatus = statusProp.GetString();
                            }

                            bool resolvedPrefix = false;
                            if (langCode == "ru" && !string.IsNullOrEmpty(officialStatus))
                            {
                                string status = officialStatus.Contains(':') ? officialStatus.Split(':')[1] : officialStatus;
                                status = status.ToLower().Trim();

                                if (status == "рабочий посёлок" || status == "рабочий поселок" || 
                                    status == "посёлок городского типа" || status == "поселок городского типа" || 
                                    status == "пгт" || status == "р.п.")
                                {
                                    typePrefix = "пгт. ";
                                    resolvedPrefix = true;
                                }
                                else if (status == "село" || status == "с")
                                {
                                    typePrefix = "с. ";
                                    resolvedPrefix = true;
                                }
                                else if (status == "деревня" || status == "д")
                                {
                                    typePrefix = "д. ";
                                    resolvedPrefix = true;
                                }
                                else if (status == "город" || status == "г")
                                {
                                    typePrefix = "г. ";
                                    resolvedPrefix = true;
                                }
                                else if (status == "посёлок" || status == "поселок" || status == "п")
                                {
                                    typePrefix = "п. ";
                                    resolvedPrefix = true;
                                }
                                else if (status == "хутор" || status == "х")
                                {
                                    typePrefix = "х. ";
                                    resolvedPrefix = true;
                                }
                                else if (status == "дачный поселок" || status == "дачный посёлок" || status == "д.п.")
                                {
                                    typePrefix = "д.п. ";
                                    resolvedPrefix = true;
                                }
                                else if (status == "курортный поселок" || status == "курортный посёлок" || status == "к.п.")
                                {
                                    typePrefix = "к.п. ";
                                    resolvedPrefix = true;
                                }
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

                                if (string.IsNullOrEmpty(typePrefix) && !string.IsNullOrEmpty(cleanSettlement))
                                {
                                    string? stateVal = address.TryGetProperty("state", out var st) ? st.GetString() : null;
                                    string? countryCodeVal = address.TryGetProperty("country_code", out var cc) ? cc.GetString() : null;
                                    if (string.Equals(stateVal, cleanSettlement, StringComparison.OrdinalIgnoreCase) && 
                                        string.Equals(countryCodeVal, "ru", StringComparison.OrdinalIgnoreCase))
                                    {
                                        typePrefix = "г. ";
                                    }
                                }
                            }

                            string formattedCity = typePrefix + cleanSettlement;

                            string? county = address.TryGetProperty("county", out var coProp) ? coProp.GetString() : null;
                            string? state = address.TryGetProperty("state", out var stProp) ? stProp.GetString() : null;
                            string? country = address.TryGetProperty("country", out var cProp) ? cProp.GetString() : null;

                            if (!string.IsNullOrEmpty(county) && !string.Equals(county, cleanSettlement, StringComparison.OrdinalIgnoreCase))
                            {
                                formattedCity += $", {county}";
                            }

                            if (!string.IsNullOrEmpty(state) && !string.Equals(state, cleanSettlement, StringComparison.OrdinalIgnoreCase) && (county == null || !string.Equals(state, county, StringComparison.OrdinalIgnoreCase)))
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
                                formattedCity += $", {cleanState}";
                            }

                            if (!string.IsNullOrEmpty(country))
                            {
                                formattedCity += $", {country}";
                            }

                            string? countryCodeValSearch = address.TryGetProperty("country_code", out var ccSearch) ? ccSearch.GetString() : null;
                            bool isPreferred = false;
                            if (!string.IsNullOrEmpty(countryCodeValSearch))
                            {
                                var preferredCountries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    "ru", "by", "kz", "ua", "kg", "am", "az", "ge", "md", "uz", "tm", "tj"
                                };
                                if (preferredCountries.Contains(countryCodeValSearch))
                                {
                                    isPreferred = true;
                                }
                            }

                            bool isExact = false;
                            {
                                string name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                                string displayName = item.TryGetProperty("display_name", out var dn) ? dn.GetString() ?? "" : "";
                                if (name.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                                    displayName.Contains(query, StringComparison.OrdinalIgnoreCase))
                                {
                                    isExact = true;
                                }
                                else
                                {
                                    foreach (var prop in address.EnumerateObject())
                                    {
                                        if (prop.Value.ValueKind == JsonValueKind.String)
                                        {
                                            string val = prop.Value.GetString() ?? "";
                                            if (val.Contains(query, StringComparison.OrdinalIgnoreCase))
                                            {
                                                isExact = true;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (!isExact && item.TryGetProperty("extratags", out var extraSearch))
                                {
                                    foreach (var prop in extraSearch.EnumerateObject())
                                    {
                                        if (prop.Value.ValueKind == JsonValueKind.String)
                                        {
                                            string val = prop.Value.GetString() ?? "";
                                            if (val.Contains(query, StringComparison.OrdinalIgnoreCase))
                                            {
                                                isExact = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            if (!isPreferred && !isExact)
                            {
                                continue;
                            }

                            string latStr = item.GetProperty("lat").GetString() ?? "0";
                            string lonStr = item.GetProperty("lon").GetString() ?? "0";
                            double resLat = double.Parse(latStr, System.Globalization.CultureInfo.InvariantCulture);
                            double resLon = double.Parse(lonStr, System.Globalization.CultureInfo.InvariantCulture);

                            results.Add(new SearchResult
                            {
                                DisplayName = formattedCity,
                                Lat = resLat,
                                Lon = resLon,
                                IsExact = isExact
                            });
                        }
                    }

                    bool hasAnyExact = false;
                    foreach (var r in results)
                    {
                        if (r.IsExact)
                        {
                            hasAnyExact = true;
                            break;
                        }
                    }

                    if (hasAnyExact)
                    {
                        results = results.FindAll(r => r.IsExact);
                    }

                    if (results.Count > 0)
                    {
                        return DeduplicateResults(results);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Nominatim search error: " + ex.Message);
            }

            // 2. Fallback to Open-Meteo search if Nominatim fails/times out
            try
            {
                string encodedQuery = Uri.EscapeDataString(query);
                string[] urls = {
                    $"https://geocoding-api.open-meteo.com/v1/search?name={encodedQuery}&count=8&language={lang.ToLower()}",
                    $"http://geocoding-api.open-meteo.com/v1/search?name={encodedQuery}&count=8&language={lang.ToLower()}"
                };

                string? json = null;
                foreach (var url in urls)
                {
                    try
                    {
                        using var response = await HttpClient.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            json = await response.Content.ReadAsStringAsync();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Open-Meteo search failed for {url}: {ex.Message}");
                    }
                }

                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("results", out var resultsArr))
                    {
                        foreach (var item in resultsArr.EnumerateArray())
                        {
                            string name = item.GetProperty("name").GetString() ?? "";
                            string country = item.TryGetProperty("country", out var cProp) ? cProp.GetString() ?? "" : "";
                            string admin1 = item.TryGetProperty("admin1", out var aProp) ? aProp.GetString() ?? "" : "";
                            string admin2 = item.TryGetProperty("admin2", out var a2Prop) ? a2Prop.GetString() ?? "" : "";
                            string fcode = item.TryGetProperty("feature_code", out var fProp) ? fProp.GetString() ?? "" : "";
                            int population = item.TryGetProperty("population", out var pProp) ? pProp.GetInt32() : 0;
                            
                            string prefix = "";
                            if (lang.Equals("ru", StringComparison.OrdinalIgnoreCase))
                            {
                                if (fcode == "PPLC" || fcode == "PPLA")
                                {
                                    prefix = "г. ";
                                }
                                else if (fcode == "PPLA2" || fcode == "PPLA3")
                                {
                                    prefix = population >= 12000 ? "г. " : "пгт. ";
                                }
                                else if (fcode == "PPL" || fcode == "PPLL" || fcode == "PPLF")
                                {
                                    string nameLower = name.ToLower();
                                    if (nameLower.EndsWith("о") || nameLower.EndsWith("ое") || nameLower.EndsWith("ово") || nameLower.EndsWith("ево") || nameLower.EndsWith("ино"))
                                    {
                                        prefix = "с. ";
                                    }
                                    else if (nameLower.EndsWith("ка") || nameLower.EndsWith("ки") || nameLower.EndsWith("цы") || nameLower.EndsWith("вцы") || nameLower.EndsWith("ичи") || nameLower.EndsWith("ха"))
                                    {
                                        prefix = "д. ";
                                    }
                                    else
                                    {
                                        prefix = "п. ";
                                    }
                                }
                            }

                            string displayName = prefix + name;
                            if (!string.IsNullOrEmpty(admin2) && admin2 != name)
                            {
                                displayName += $", {admin2}";
                            }
                            if (!string.IsNullOrEmpty(admin1) && admin1 != name && admin1 != admin2)
                            {
                                displayName += $", {admin1}";
                            }
                            if (!string.IsNullOrEmpty(country))
                            {
                                displayName += $", {country}";
                            }

                            string countryCode = item.TryGetProperty("country_code", out var ccProp) ? ccProp.GetString() ?? "" : "";
                            bool isPreferred = false;
                            if (!string.IsNullOrEmpty(countryCode))
                            {
                                var preferredCountries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    "ru", "by", "kz", "ua", "kg", "am", "az", "ge", "md", "uz", "tm", "tj"
                                };
                                if (preferredCountries.Contains(countryCode))
                                {
                                    isPreferred = true;
                                }
                            }

                            bool isExact = name.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                                           country.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                                           admin1.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                                           admin2.Contains(query, StringComparison.OrdinalIgnoreCase);

                            if (!isPreferred && !isExact)
                            {
                                continue;
                            }

                            double resLat = item.GetProperty("latitude").GetDouble();
                            double resLon = item.GetProperty("longitude").GetDouble();

                            results.Add(new SearchResult
                            {
                                DisplayName = displayName,
                                Lat = resLat,
                                Lon = resLon,
                                IsExact = isExact
                            });
                        }

                        bool hasAnyExact = false;
                        foreach (var r in results)
                        {
                            if (r.IsExact)
                            {
                                hasAnyExact = true;
                                break;
                            }
                        }

                        if (hasAnyExact)
                        {
                            results = results.FindAll(r => r.IsExact);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SearchCityAsync fallback error: " + ex.Message);
            }
            return DeduplicateResults(results);
        }

        public async Task<WeatherInfo> GetWeatherAsync(double? customLat = null, double? customLon = null, string customCityName = null)
        {
            var info = new WeatherInfo();
            double lat = 55.7558;
            double lon = 37.6173;
            string city = string.IsNullOrEmpty(customCityName) ? "Москва" : customCityName;
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
                        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2.5));
                        using var response = await HttpClient.GetAsync($"https://ipwho.is/?lang={ipLang}", cts.Token);
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
                            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2.5));
                            using var response = await HttpClient.GetAsync($"http://ip-api.com/json/?lang={ipLang}", cts.Token);
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
                            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2.5));
                            using var response = await HttpClient.GetAsync("https://freeipapi.com/api/json", cts.Token);
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

                    // Try Provider 4: ipinfo.io
                    if (!gotCoords)
                    {
                        try
                        {
                            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2.5));
                            using var response = await HttpClient.GetAsync("https://ipinfo.io/json", cts.Token);
                            if (response.IsSuccessStatusCode)
                            {
                                var json = await response.Content.ReadAsStringAsync();
                                using var doc = JsonDocument.Parse(json);
                                var root = doc.RootElement;
                                if (root.TryGetProperty("loc", out var locProp))
                                {
                                    var locParts = locProp.GetString()?.Split(',');
                                    if (locParts != null && locParts.Length == 2)
                                    {
                                        lat = double.Parse(locParts[0], System.Globalization.CultureInfo.InvariantCulture);
                                        lon = double.Parse(locParts[1], System.Globalization.CultureInfo.InvariantCulture);
                                        city = root.TryGetProperty("city", out var cityProp) ? cityProp.GetString() ?? city : city;
                                        gotCoords = true;
                                    }
                                }
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

                bool hasPrefix = false;
                if (!string.IsNullOrEmpty(customCityName))
                {
                    string[] prefixes = { "г. ", "с. ", "д. ", "пгт. ", "п. ", "х. ", "мкр. ", "снт. ", "р.п. ", "к.п. ", "д.п. " };
                    foreach (var prefix in prefixes)
                    {
                        if (customCityName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            hasPrefix = true;
                            break;
                        }
                    }
                }

                if (!hasPrefix)
                {
                    // Try Nominatim first
                    try
                    {
                        string[] reverseUrls = {
                            $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latStr}&lon={lonStr}&zoom=14&addressdetails=1&extratags=1&accept-language={acceptLang}",
                            $"http://nominatim.openstreetmap.org/reverse?format=json&lat={latStr}&lon={lonStr}&zoom=14&addressdetails=1&extratags=1&accept-language={acceptLang}"
                        };

                        foreach (var reverseUrl in reverseUrls)
                        {
                            try
                            {
                                var request = new HttpRequestMessage(HttpMethod.Get, reverseUrl);
                                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2.5));
                                using var response = await HttpClient.SendAsync(request, cts.Token);
                                if (response.IsSuccessStatusCode)
                                {
                                    var json = await response.Content.ReadAsStringAsync();
                                    if (!json.Contains("Access denied"))
                                    {
                                        using var doc = JsonDocument.Parse(json);
                                        var root = doc.RootElement;
                                        city = FormatCityName(root, langCode, city);
                                        reverseGeocoded = true;
                                        break;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }

                    // Fallback to BigDataCloud reverse geocoding
                    if (!reverseGeocoded)
                    {
                        try
                        {
                            string bdcUrl = $"https://api.bigdatacloud.net/data/reverse-geocode-client?latitude={latStr}&longitude={lonStr}&localityLanguage={langCode}";
                            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2.5));
                            using var response = await HttpClient.GetAsync(bdcUrl, cts.Token);
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
            }

            info.City = city;

            bool weatherSuccess = false;

            // 4. Try api.met.no first
            try
            {
                string latStr = lat.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
                string lonStr = lon.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
                string[] metUrls = {
                    $"https://api.met.no/weatherapi/locationforecast/2.0/compact?lat={latStr}&lon={lonStr}",
                    $"http://api.met.no/weatherapi/locationforecast/2.0/compact?lat={latStr}&lon={lonStr}",
                    $"https://corsproxy.io/?https://api.met.no/weatherapi/locationforecast/2.0/compact?lat={latStr}&lon={lonStr}",
                    $"https://api.allorigins.win/raw?url=https://api.met.no/weatherapi/locationforecast/2.0/compact?lat={latStr}&lon={lonStr}",
                    $"https://api.codetabs.com/v1/proxy?quest=https://api.met.no/weatherapi/locationforecast/2.0/compact?lat={latStr}&lon={lonStr}"
                };

                string? json = null;
                foreach (var metUrl in metUrls)
                {
                    try
                    {
                        using var response = await HttpClient.GetAsync(metUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            json = await response.Content.ReadAsStringAsync();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"api.met.no failed for {metUrl}: {ex.Message}");
                    }
                }

                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("properties", out var properties) && properties.TryGetProperty("timeseries", out var timeseriesArray))
                    {
                        int tsCount = timeseriesArray.GetArrayLength();
                        if (tsCount > 0)
                        {
                            // 1. Current conditions from timeseries[0]
                            var currentItem = timeseriesArray[0];
                            var currentData = currentItem.GetProperty("data");
                            var currentInstantDetails = currentData.GetProperty("instant").GetProperty("details");

                            info.Temperature = currentInstantDetails.GetProperty("air_temperature").GetDouble();
                            info.Humidity = (int)Math.Round(currentInstantDetails.GetProperty("relative_humidity").GetDouble());
                            // convert wind speed from m/s to km/h
                            double windMps = currentInstantDetails.GetProperty("wind_speed").GetDouble();
                            info.WindSpeed = windMps * 3.6;

                            // Condition/Icon
                            string symbolCode = "clearsky_day";
                            if (currentData.TryGetProperty("next_1_hours", out var next1))
                            {
                                symbolCode = next1.GetProperty("summary").GetProperty("symbol_code").GetString() ?? symbolCode;
                            }
                            else if (currentData.TryGetProperty("next_6_hours", out var next6))
                            {
                                symbolCode = next6.GetProperty("summary").GetProperty("symbol_code").GetString() ?? symbolCode;
                            }
                            var (cond, icon) = GetConditionByMetSymbol(symbolCode);
                            info.Condition = cond;
                            info.Icon = icon;

                            // 2. Build 7-day forecast
                            // Group timeseries entries by local date
                            var dailyGroups = new Dictionary<DateTime, List<JsonElement>>();
                            for (int i = 0; i < tsCount; i++)
                            {
                                var item = timeseriesArray[i];
                                if (item.TryGetProperty("time", out var timeProp))
                                {
                                    string timeStr = timeProp.GetString() ?? "";
                                    if (DateTime.TryParse(timeStr, out var itemTime))
                                    {
                                        var localDate = itemTime.ToLocalTime().Date;
                                        if (!dailyGroups.ContainsKey(localDate))
                                        {
                                            dailyGroups[localDate] = new List<JsonElement>();
                                        }
                                        dailyGroups[localDate].Add(item);
                                    }
                                }
                            }

                            info.Forecast.Clear();
                            for (int i = 0; i < 7; i++)
                            {
                                var targetDate = DateTime.Today.AddDays(i);
                                if (dailyGroups.TryGetValue(targetDate, out var items))
                                {
                                    double maxTemp = double.MinValue;
                                    double minTemp = double.MaxValue;
                                    string daySymbolCode = "";

                                    // Find max/min temp and find the symbol closest to noon (12:00 local time)
                                    double minDiffHours = 24.0;
                                    foreach (var item in items)
                                    {
                                        var details = item.GetProperty("data").GetProperty("instant").GetProperty("details");
                                        if (details.TryGetProperty("air_temperature", out var tempProp))
                                        {
                                            double temp = tempProp.GetDouble();
                                            if (temp > maxTemp) maxTemp = temp;
                                            if (temp < minTemp) minTemp = temp;
                                        }

                                        // Check time difference to 12:00
                                        if (DateTime.TryParse(item.GetProperty("time").GetString(), out var parsedTime))
                                        {
                                            var localTime = parsedTime.ToLocalTime();
                                            double diff = Math.Abs((localTime - targetDate.AddHours(12)).TotalHours);
                                            if (diff < minDiffHours)
                                            {
                                                string? sym = null;
                                                var data = item.GetProperty("data");
                                                if (data.TryGetProperty("next_1_hours", out var n1))
                                                    sym = n1.GetProperty("summary").GetProperty("symbol_code").GetString();
                                                else if (data.TryGetProperty("next_6_hours", out var n6))
                                                    sym = n6.GetProperty("summary").GetProperty("symbol_code").GetString();

                                                if (!string.IsNullOrEmpty(sym))
                                                {
                                                    daySymbolCode = sym;
                                                    minDiffHours = diff;
                                                }
                                            }
                                        }
                                    }

                                    // Handle fallbacks if values not set
                                    if (maxTemp == double.MinValue) maxTemp = info.Temperature;
                                    if (minTemp == double.MaxValue) minTemp = info.Temperature;
                                    if (string.IsNullOrEmpty(daySymbolCode)) daySymbolCode = symbolCode;

                                    var culture = LocalizationService.Instance.CurrentLanguage switch
                                    {
                                        "EN" => System.Globalization.CultureInfo.GetCultureInfo("en-US"),
                                        "ZH" => System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),
                                        _ => System.Globalization.CultureInfo.GetCultureInfo("ru-RU")
                                    };
                                    var dayName = i == 0 ? "Сегодня" : targetDate.ToString("ddd", culture);
                                    if (dayName.Length > 0 && char.IsLower(dayName[0]))
                                    {
                                        dayName = char.ToUpper(dayName[0]) + dayName.Substring(1);
                                    }

                                    var (fCond, fIcon) = GetConditionByMetSymbol(daySymbolCode);

                                    info.Forecast.Add(new ForecastDay
                                    {
                                        Day = dayName,
                                        MaxTemp = maxTemp,
                                        MinTemp = minTemp,
                                        Condition = fCond,
                                        Icon = fIcon
                                    });
                                }
                                else
                                {
                                    // If we don't have this day (e.g. at the end of the 7-day range), generate from previous day or dummy data
                                    var culture = LocalizationService.Instance.CurrentLanguage switch
                                    {
                                        "EN" => System.Globalization.CultureInfo.GetCultureInfo("en-US"),
                                        "ZH" => System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),
                                        _ => System.Globalization.CultureInfo.GetCultureInfo("ru-RU")
                                    };
                                    var dayName = i == 0 ? "Сегодня" : targetDate.ToString("ddd", culture);
                                    if (dayName.Length > 0 && char.IsLower(dayName[0]))
                                    {
                                        dayName = char.ToUpper(dayName[0]) + dayName.Substring(1);
                                    }

                                    info.Forecast.Add(new ForecastDay
                                    {
                                        Day = dayName,
                                        MaxTemp = info.Temperature + 1,
                                        MinTemp = info.Temperature - 3,
                                        Condition = info.Condition,
                                        Icon = info.Icon
                                    });
                                }
                            }

                            weatherSuccess = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("api.met.no weather fetch error: " + ex.Message);
            }

            // 5. Fallback to Open-Meteo if api.met.no fails
            if (!weatherSuccess)
            {
                try
                {
                    string latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string[] urls = {
                        $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=7",
                        $"http://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=7"
                    };

                    string? json = null;
                    foreach (var url in urls)
                    {
                        try
                        {
                            using var response = await HttpClient.GetAsync(url);
                            if (response.IsSuccessStatusCode)
                            {
                                json = await response.Content.ReadAsStringAsync();
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Open-Meteo forecast failed for {url}: {ex.Message}");
                        }
                    }

                    if (json != null)
                    {
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
                        weatherSuccess = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Open-Meteo weather fetch error: " + ex.Message);
                }
            }

            // Fallback to wttr.in if both fail
            if (!weatherSuccess)
            {
                try
                {
                    string latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string[] wttrUrls = {
                        $"https://wttr.in/{latStr},{lonStr}?format=j1&lang={langCode}",
                        $"http://wttr.in/{latStr},{lonStr}?format=j1&lang={langCode}",
                        $"https://v2.wttr.in/{latStr},{lonStr}?format=j1&lang={langCode}",
                        $"http://v2.wttr.in/{latStr},{lonStr}?format=j1&lang={langCode}"
                    };

                    string? json = null;
                    foreach (var wttrUrl in wttrUrls)
                    {
                        try
                        {
                            using var response = await HttpClient.GetAsync(wttrUrl);
                            if (response.IsSuccessStatusCode)
                            {
                                json = await response.Content.ReadAsStringAsync();
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"wttr.in failed for {wttrUrl}: {ex.Message}");
                        }
                    }

                    if (json != null)
                    {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("current_condition", out var currentArr) && currentArr.GetArrayLength() > 0)
                        {
                            var current = currentArr[0];

                            string tempStr = current.GetProperty("temp_C").GetString() ?? "0";
                            info.Temperature = double.Parse(tempStr, System.Globalization.CultureInfo.InvariantCulture);

                            string windStr = current.GetProperty("windspeedKmph").GetString() ?? "0";
                            info.WindSpeed = double.Parse(windStr, System.Globalization.CultureInfo.InvariantCulture);

                            string humStr = current.GetProperty("humidity").GetString() ?? "0";
                            info.Humidity = int.Parse(humStr);

                            string codeStr = current.GetProperty("weatherCode").GetString() ?? "113";
                            int code = int.Parse(codeStr);
                            (info.Condition, info.Icon) = GetConditionByWwoCode(code);
                        }

                        if (root.TryGetProperty("weather", out var weatherArr))
                        {
                            int count = weatherArr.GetArrayLength();
                            info.Forecast.Clear();
                            for (int i = 0; i < count; i++)
                            {
                                var weatherDay = weatherArr[i];
                                var dateStr = weatherDay.GetProperty("date").GetString() ?? "";
                                var date = DateTime.TryParse(dateStr, out var d) ? d : DateTime.Now.AddDays(i);

                                var culture = LocalizationService.Instance.CurrentLanguage switch
                                {
                                    "EN" => System.Globalization.CultureInfo.GetCultureInfo("en-US"),
                                    "ZH" => System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),
                                    _ => System.Globalization.CultureInfo.GetCultureInfo("ru-RU")
                                };
                                var dayName = i == 0 ? "Сегодня" : date.ToString("ddd", culture);

                                string maxTempStr = weatherDay.GetProperty("maxtempC").GetString() ?? "0";
                                double maxTemp = double.Parse(maxTempStr, System.Globalization.CultureInfo.InvariantCulture);

                                string minTempStr = weatherDay.GetProperty("mintempC").GetString() ?? "0";
                                double minTemp = double.Parse(minTempStr, System.Globalization.CultureInfo.InvariantCulture);

                                int code = 113;
                                if (weatherDay.TryGetProperty("hourly", out var hourlyArr) && hourlyArr.GetArrayLength() > 0)
                                {
                                    int midIdx = Math.Min(4, hourlyArr.GetArrayLength() - 1);
                                    if (midIdx >= 0)
                                    {
                                        string hCodeStr = hourlyArr[midIdx].GetProperty("weatherCode").GetString() ?? "113";
                                        code = int.Parse(hCodeStr);
                                    }
                                }

                                var (cond, icon) = GetConditionByWwoCode(code);

                                info.Forecast.Add(new ForecastDay
                                {
                                    Day = dayName,
                                    MaxTemp = maxTemp,
                                    MinTemp = minTemp,
                                    Condition = cond,
                                    Icon = icon
                                });
                            }

                            // If we have less than 7 days, generate the remaining days
                            if (count < 7 && count > 0)
                            {
                                var lastDay = info.Forecast[count - 1];
                                for (int i = count; i < 7; i++)
                                {
                                    var date = DateTime.Today.AddDays(i);
                                    var culture = LocalizationService.Instance.CurrentLanguage switch
                                    {
                                        "EN" => System.Globalization.CultureInfo.GetCultureInfo("en-US"),
                                        "ZH" => System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),
                                        _ => System.Globalization.CultureInfo.GetCultureInfo("ru-RU")
                                    };
                                    var dayName = date.ToString("ddd", culture);
                                    if (dayName.Length > 0 && char.IsLower(dayName[0]))
                                    {
                                        dayName = char.ToUpper(dayName[0]) + dayName.Substring(1);
                                    }

                                    double maxT = lastDay.MaxTemp;
                                    double minT = lastDay.MinTemp;

                                    // Minor natural temperature variation
                                    int seed = (DateTime.Today.Day + i) % 3 - 1; // -1, 0, or 1
                                    maxT += seed;
                                    minT += seed;

                                    info.Forecast.Add(new ForecastDay
                                    {
                                        Day = dayName,
                                        MaxTemp = maxT,
                                        MinTemp = minT,
                                        Condition = lastDay.Condition,
                                        Icon = lastDay.Icon
                                    });
                                }
                            }
                        }
                        weatherSuccess = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("wttr.in weather fetch error: " + ex.Message);
                }
            }

            // Fallback dummy weather if both fail
            if (!weatherSuccess)
            {
                info.Temperature = 21.5;
                info.Condition = "Ясно";
                info.Icon = "sun";
                info.Forecast = new List<ForecastDay>();

                var culture = LocalizationService.Instance.CurrentLanguage switch
                {
                    "EN" => System.Globalization.CultureInfo.GetCultureInfo("en-US"),
                    "ZH" => System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),
                    _ => System.Globalization.CultureInfo.GetCultureInfo("ru-RU")
                };

                var dummyData = new[]
                {
                    new { Max = 23.0, Min = 15.0, Cond = "Ясно", Icon = "sun" },
                    new { Max = 22.0, Min = 14.0, Cond = "Переменная облачность", Icon = "cloud-sun" },
                    new { Max = 24.0, Min = 16.0, Cond = "Ясно", Icon = "sun" },
                    new { Max = 20.0, Min = 13.0, Cond = "Пасмурно", Icon = "cloud" },
                    new { Max = 19.0, Min = 12.0, Cond = "Дождь", Icon = "rain" },
                    new { Max = 21.0, Min = 14.0, Cond = "Переменная облачность", Icon = "cloud-sun" },
                    new { Max = 22.0, Min = 15.0, Cond = "Ясно", Icon = "sun" }
                };

                for (int i = 0; i < 7; i++)
                {
                    var date = DateTime.Today.AddDays(i);
                    var dayName = i == 0 ? "Сегодня" : date.ToString("ddd", culture);
                    if (dayName.Length > 0 && char.IsLower(dayName[0]))
                    {
                        dayName = char.ToUpper(dayName[0]) + dayName.Substring(1);
                    }

                    info.Forecast.Add(new ForecastDay
                    {
                        Day = dayName,
                        MaxTemp = dummyData[i].Max,
                        MinTemp = dummyData[i].Min,
                        Condition = dummyData[i].Cond,
                        Icon = dummyData[i].Icon
                    });
                }
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

        private (string text, string icon) GetConditionByWwoCode(int code)
        {
            return code switch
            {
                113 => ("Ясно", "sun"),
                116 => ("Переменная облачность", "cloud-sun"),
                119 or 122 => ("Пасмурно", "cloud"),
                143 or 248 or 260 => ("Туман", "fog"),
                263 or 266 or 271 or 273 or 293 or 296 or 311 or 314 or 353 => ("Дождь", "rain"), // Light/drizzle/freezing rain
                176 or 299 or 302 or 305 or 308 or 356 or 359 => ("Дождь", "rain"), // Rain / heavy rain / showers
                179 or 182 or 227 or 230 or 317 or 320 or 323 or 326 or 329 or 332 or 335 or 338 or 350 or 362 or 365 or 368 or 371 or 374 or 377 => ("Снегопад", "snow"),
                200 or 386 or 389 or 392 or 395 => ("Гроза", "thunderstorm"),
                _ => ("Ясно", "sun")
            };
        }

        private static string FormatCityName(JsonElement root, string langCode, string defaultCityName)
        {
            if (!root.TryGetProperty("address", out var address))
            {
                return defaultCityName;
            }

            string? settlement = null;
            string typePrefix = "";

            string? addresstype = root.TryGetProperty("addresstype", out var atProp) ? atProp.GetString() : null;
            if (string.IsNullOrEmpty(addresstype))
            {
                addresstype = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
            }

            if (!string.IsNullOrEmpty(addresstype) && address.TryGetProperty(addresstype, out var settProp))
            {
                settlement = settProp.GetString();
            }

            if (string.IsNullOrEmpty(settlement))
            {
                settlement = root.TryGetProperty("name", out var nProp) ? nProp.GetString() : defaultCityName;
            }

            // Extract official_status if present
            string? officialStatus = null;
            if (root.TryGetProperty("extratags", out var extra) && extra.TryGetProperty("official_status", out var statusProp))
            {
                officialStatus = statusProp.GetString();
            }

            bool resolvedPrefix = false;
            if (langCode == "ru" && !string.IsNullOrEmpty(officialStatus))
            {
                string status = officialStatus.Contains(':') ? officialStatus.Split(':')[1] : officialStatus;
                status = status.ToLower().Trim();

                if (status == "рабочий посёлок" || status == "рабочий поселок" || 
                    status == "посёлок городского типа" || status == "поселок городского типа" || 
                    status == "пгт" || status == "р.п.")
                {
                    typePrefix = "пгт. ";
                    resolvedPrefix = true;
                }
                else if (status == "село" || status == "с")
                {
                    typePrefix = "с. ";
                    resolvedPrefix = true;
                }
                else if (status == "деревня" || status == "д")
                {
                    typePrefix = "д. ";
                    resolvedPrefix = true;
                }
                else if (status == "город" || status == "г")
                {
                    typePrefix = "г. ";
                    resolvedPrefix = true;
                }
                else if (status == "посёлок" || status == "поселок" || status == "п")
                {
                    typePrefix = "п. ";
                    resolvedPrefix = true;
                }
                else if (status == "хутор" || status == "х")
                {
                    typePrefix = "х. ";
                    resolvedPrefix = true;
                }
                else if (status == "дачный поселок" || status == "дачный посёлок" || status == "д.п.")
                {
                    typePrefix = "д.п. ";
                    resolvedPrefix = true;
                }
                else if (status == "курортный поселок" || status == "курортный посёлок" || status == "к.п.")
                {
                    typePrefix = "к.п. ";
                    resolvedPrefix = true;
                }
            }

            if (!resolvedPrefix && langCode == "ru" && !string.IsNullOrEmpty(addresstype))
            {
                if (addresstype == "city" || addresstype == "town") typePrefix = "г. ";
                else if (addresstype == "village") typePrefix = "с. ";
                else if (addresstype == "hamlet") typePrefix = "д. ";
                else if (addresstype == "suburb") typePrefix = "мкр. ";
                else if (addresstype == "isolated_dwelling" || addresstype == "croft" || addresstype == "farm") typePrefix = "х. ";
                else if (addresstype == "allotments") typePrefix = "снт. ";
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

                if (langCode == "ru" && string.IsNullOrEmpty(typePrefix) && !string.IsNullOrEmpty(cleanSettlement))
                {
                    string? stateVal = address.TryGetProperty("state", out var st) ? st.GetString() : null;
                    string? countryCodeVal = address.TryGetProperty("country_code", out var cc) ? cc.GetString() : null;
                    if (string.Equals(stateVal, cleanSettlement, StringComparison.OrdinalIgnoreCase) && 
                        string.Equals(countryCodeVal, "ru", StringComparison.OrdinalIgnoreCase))
                    {
                        typePrefix = "г. ";
                    }
                }
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

        private static string NormalizeCityName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "";
            string normalized = displayName.ToLowerInvariant().Trim();
            string[] prefixes = { "г. ", "с. ", "д. ", "пгт. ", "п. ", "х. ", "мкр. ", "снт. ", "р.п. ", "к.п. ", "д.п. " };
            foreach (var prefix in prefixes)
            {
                if (normalized.StartsWith(prefix))
                {
                    normalized = normalized.Substring(prefix.Length);
                    break;
                }
            }
            return normalized;
        }

        private static List<SearchResult> DeduplicateResults(List<SearchResult> list)
        {
            var uniqueResults = new List<SearchResult>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in list)
            {
                string norm = NormalizeCityName(r.DisplayName);
                if (!seenNames.Contains(norm))
                {
                    seenNames.Add(norm);
                    uniqueResults.Add(r);
                }
            }
            return uniqueResults;
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

        private (string text, string icon) GetConditionByMetSymbol(string symbolCode)
        {
            if (string.IsNullOrEmpty(symbolCode)) return ("Ясно", "sun");

            // Strip suffix like _day, _night, _polartwilight
            string baseSymbol = symbolCode;
            int underIdx = symbolCode.IndexOf('_');
            if (underIdx > 0)
            {
                baseSymbol = symbolCode.Substring(0, underIdx);
            }

            return baseSymbol.ToLowerInvariant() switch
            {
                "clearsky" => ("Ясно", "sun"),
                "fair" => ("Переменная облачность", "cloud-sun"),
                "partlycloudy" => ("Переменная облачность", "cloud-sun"),
                "cloudy" => ("Пасмурно", "cloud"),
                "fog" => ("Туман", "fog"),
                "lightdrizzle" or "lightdrizzleishower" or "drizzle" or "drizzleishower" => ("Морось", "rain"),
                "lightrain" or "lightrainshowers" or "rainshowers" or "rain" => ("Дождь", "rain"),
                "heavyrain" or "heavyrainshowers" or "heavyrainshowersandthunder" => ("Ливень", "rain"),
                "lightsnow" or "lightsnowshowers" or "snow" or "snowshowers" or "heavysnow" or "heavysnowshowers" => ("Снегопад", "snow"),
                "lightsleet" or "lightsleetshowers" or "sleet" or "sleetshowers" or "heavysleet" or "heavysleetshowers" => ("Дождь со снегом", "snow"),
                "sleetshowersandthunder" or "sleetandthunder" or "lightsleetshowersandthunder" or "heavysleetshowersandthunder" => ("Дождь со снегом", "snow"),
                "heavyrainandthunder" or "lightrainshowersandthunder" or "rainshowersandthunder" or "rainandthunder" => ("Гроза", "thunderstorm"),
                "lightsnowshowersandthunder" or "snowshowersandthunder" or "heavysnowshowersandthunder" or "lightsnowandthunder" or "snowandthunder" or "heavysnowandthunder" => ("Гроза со снегом", "thunderstorm"),
                _ => ("Ясно", "sun")
            };
        }
    }
}
