using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SystemHub.Services
{
    public class SupabaseSession
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = "";

        [JsonPropertyName("user")]
        public SupabaseUser? User { get; set; }
    }

    public class SupabaseUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("email")]
        public string Email { get; set; } = "";

        [JsonPropertyName("user_metadata")]
        public JsonElement UserMetadata { get; set; }

        public string Username
        {
            get
            {
                if (UserMetadata.ValueKind == JsonValueKind.Object && UserMetadata.TryGetProperty("username", out var prop))
                {
                    return prop.GetString() ?? "";
                }
                return "";
            }
        }

        public string AvatarPath
        {
            get
            {
                if (UserMetadata.ValueKind == JsonValueKind.Object && UserMetadata.TryGetProperty("avatar_path", out var prop))
                {
                    return prop.GetString() ?? "";
                }
                return "";
            }
        }

        public DateTime RegistrationDate
        {
            get
            {
                if (UserMetadata.ValueKind == JsonValueKind.Object && UserMetadata.TryGetProperty("registration_date", out var prop))
                {
                    if (DateTime.TryParse(prop.GetString(), out var date))
                    {
                        return date;
                    }
                }
                return DateTime.Now;
            }
        }
    }

    public class SupabaseService
    {
        private static SupabaseService? _instance;
        public static SupabaseService Instance => _instance ??= new SupabaseService();

        private const string DefaultSupabaseUrl = "https://eraqshyixrerathfkhfo.supabase.co";
        private const string DefaultSupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVyYXFzaHlpeHJlcmF0aGZraGZvIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc4MTM1MjAxNywiZXhwIjoyMDk2OTI4MDE3fQ.Cmu3jMZ7MfkLnxqIXkI4l-I4wB-GIm3XgzGT0u3z7pk";

        private readonly HttpClient _client;
        private string _supabaseUrl = "";
        private string _supabaseKey = "";

        public string SupabaseUrl => _supabaseUrl;
        public string SupabaseKey => _supabaseKey;

        public bool IsConfigured => !string.IsNullOrEmpty(_supabaseUrl) && !string.IsNullOrEmpty(_supabaseKey);

        private SupabaseService()
        {
            _client = new HttpClient();
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);
                string configPath = Path.Combine(appData, "supabase_config.json");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("SupabaseUrl", out var urlProp)) _supabaseUrl = urlProp.GetString()?.Trim() ?? "";
                    if (root.TryGetProperty("SupabaseKey", out var keyProp)) _supabaseKey = keyProp.GetString()?.Trim() ?? "";
                }

                // If configuration is missing or contains placeholder values, fall back to default production keys
                if (string.IsNullOrEmpty(_supabaseUrl) || _supabaseUrl == "https://your-project.supabase.co")
                {
                    _supabaseUrl = DefaultSupabaseUrl;
                }
                if (string.IsNullOrEmpty(_supabaseKey) || _supabaseKey == "your-anon-key-here")
                {
                    _supabaseKey = DefaultSupabaseKey;
                }
            }
            catch
            {
                if (string.IsNullOrEmpty(_supabaseUrl)) _supabaseUrl = DefaultSupabaseUrl;
                if (string.IsNullOrEmpty(_supabaseKey)) _supabaseKey = DefaultSupabaseKey;
            }
        }

        private void EnsureHeaders(HttpRequestMessage request)
        {
            request.Headers.Clear();
            request.Headers.Add("apikey", _supabaseKey);
        }

        public static string GetFriendlyErrorMessage(string err)
        {
            if (string.IsNullOrWhiteSpace(err)) return "Произошла неизвестная ошибка.";

            // Database config errors
            if (err.Contains("Supabase не настроен") || err.Contains("Таблица профилей не найдена"))
            {
                if (err.Contains("Таблица профилей"))
                    return "Не удалось найти пользователя по никнейму. Пожалуйста, войдите с помощью Email.";
                return "Сервис авторизации временно недоступен. Пожалуйста, попробуйте позже.";
            }

            // Network errors
            if (err.Contains("could not translate host name", StringComparison.OrdinalIgnoreCase) || 
                err.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
                err.Contains("No connection could be made", StringComparison.OrdinalIgnoreCase) ||
                err.Contains("HttpRequestException", StringComparison.OrdinalIgnoreCase) ||
                err.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                err.Contains("socket", StringComparison.OrdinalIgnoreCase) ||
                err.Contains("dns", StringComparison.OrdinalIgnoreCase))
            {
                return "Ошибка подключения. Проверьте интернет-соединение.";
            }

            // Supabase API errors mapping
            string errLower = err.ToLower();
            if (errLower.Contains("invalid login credentials") || errLower.Contains("invalid_credentials") || errLower.Contains("invalid credentials"))
            {
                return "Неверная почта, логин или пароль.";
            }
            if (errLower.Contains("email not confirmed") || errLower.Contains("email_not_confirmed"))
            {
                return "Адрес электронной почты не подтвержден.";
            }
            if (errLower.Contains("user already exists") || errLower.Contains("user_already_exists") || errLower.Contains("already registered"))
            {
                return "Пользователь с таким адресом электронной почты уже зарегистрирован.";
            }
            if (errLower.Contains("password should be at least"))
            {
                return "Пароль должен содержать не менее 6 символов.";
            }
            if (errLower.Contains("token has expired") || errLower.Contains("invalid otp") || errLower.Contains("otp") || errLower.Contains("verification code"))
            {
                return "Неверный или истекший код подтверждения.";
            }
            if (errLower.Contains("invalid email"))
            {
                return "Пожалуйста, введите корректный Email.";
            }
            if (errLower.Contains("user not found"))
            {
                return "Пользователь не найден.";
            }

            // Return clean default if it doesn't match above, but filter out JSON/HTML
            if (err.StartsWith("{") || err.StartsWith("<"))
            {
                return "Ошибка сервера авторизации. Пожалуйста, попробуйте позже.";
            }

            return err;
        }

        private async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
        {
            try
            {
                string content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                string errMsg = "Unknown error";
                if (root.TryGetProperty("error_description", out var errDesc))
                {
                    errMsg = errDesc.GetString() ?? response.ReasonPhrase ?? "Unknown error";
                }
                else if (root.TryGetProperty("msg", out var msg))
                {
                    errMsg = msg.GetString() ?? response.ReasonPhrase ?? "Unknown error";
                }
                else if (root.TryGetProperty("message", out var message))
                {
                    errMsg = message.GetString() ?? response.ReasonPhrase ?? "Unknown error";
                }
                else
                {
                    errMsg = content;
                }
                return GetFriendlyErrorMessage(errMsg);
            }
            catch
            {
                return GetFriendlyErrorMessage(response.ReasonPhrase ?? "Unknown error");
            }
        }

        public async Task<(SupabaseSession? Session, string Error)> SignUpAsync(string email, string password, string username)
        {
            if (!IsConfigured)
            {
                return (null, GetFriendlyErrorMessage("Supabase не настроен."));
            }

            try
            {
                string url = $"{_supabaseUrl}/auth/v1/signup";
                var payload = new
                {
                    email = email,
                    password = password,
                    data = new
                    {
                        username = username,
                        avatar_path = "",
                        registration_date = DateTime.UtcNow.ToString("o")
                    }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                EnsureHeaders(request);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var session = JsonSerializer.Deserialize<SupabaseSession>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return (session, "");
                }

                string errMsg = await GetErrorMessageAsync(response);
                return (null, errMsg);
            }
            catch (Exception ex)
            {
                return (null, GetFriendlyErrorMessage(ex.Message));
            }
        }

        public async Task<(SupabaseSession? Session, string Error)> VerifyOtpAsync(string email, string code, string type)
        {
            if (!IsConfigured)
            {
                return (null, GetFriendlyErrorMessage("Supabase не настроен."));
            }

            try
            {
                string url = $"{_supabaseUrl}/auth/v1/verify";
                var payload = new
                {
                    type = type, // "signup" or "recovery"
                    email = email,
                    token = code
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                EnsureHeaders(request);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var session = JsonSerializer.Deserialize<SupabaseSession>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return (session, "");
                }

                string errMsg = await GetErrorMessageAsync(response);
                return (null, errMsg);
            }
            catch (Exception ex)
            {
                return (null, GetFriendlyErrorMessage(ex.Message));
            }
        }

        public async Task<(SupabaseSession? Session, string Error)> SignInAsync(string email, string password)
        {
            if (!IsConfigured)
            {
                return (null, GetFriendlyErrorMessage("Supabase не настроен."));
            }

            try
            {
                string url = $"{_supabaseUrl}/auth/v1/token?grant_type=password";
                var payload = new
                {
                    email = email,
                    password = password
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                EnsureHeaders(request);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var session = JsonSerializer.Deserialize<SupabaseSession>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return (session, "");
                }

                string errMsg = await GetErrorMessageAsync(response);
                return (null, errMsg);
            }
            catch (Exception ex)
            {
                return (null, GetFriendlyErrorMessage(ex.Message));
            }
        }

        public async Task<(bool Success, string Error)> RecoverPasswordAsync(string email)
        {
            if (!IsConfigured)
            {
                return (false, GetFriendlyErrorMessage("Supabase не настроен."));
            }

            try
            {
                string url = $"{_supabaseUrl}/auth/v1/recover";
                var payload = new
                {
                    email = email
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                EnsureHeaders(request);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "");
                }

                string errMsg = await GetErrorMessageAsync(response);
                return (false, errMsg);
            }
            catch (Exception ex)
            {
                return (false, GetFriendlyErrorMessage(ex.Message));
            }
        }

        public async Task<(SupabaseUser? User, string Error)> GetUserAsync(string accessToken)
        {
            if (!IsConfigured)
            {
                return (null, GetFriendlyErrorMessage("Supabase не настроен."));
            }

            try
            {
                string url = $"{_supabaseUrl}/auth/v1/user";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                EnsureHeaders(request);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<SupabaseUser>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return (user, "");
                }

                string errMsg = await GetErrorMessageAsync(response);
                return (null, errMsg);
            }
            catch (Exception ex)
            {
                return (null, GetFriendlyErrorMessage(ex.Message));
            }
        }

        public async Task<(SupabaseSession? Session, string Error)> RefreshTokenAsync(string refreshToken)
        {
            if (!IsConfigured)
            {
                return (null, GetFriendlyErrorMessage("Supabase не настроен."));
            }

            try
            {
                string url = $"{_supabaseUrl}/auth/v1/token?grant_type=refresh_token";
                var payload = new
                {
                    refresh_token = refreshToken
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                EnsureHeaders(request);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var session = JsonSerializer.Deserialize<SupabaseSession>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return (session, "");
                }

                string errMsg = await GetErrorMessageAsync(response);
                return (null, errMsg);
            }
            catch (Exception ex)
            {
                return (null, GetFriendlyErrorMessage(ex.Message));
            }
        }

        public async Task<(SupabaseUser? User, string Error)> UpdateUserAsync(string accessToken, string? password, string? username, string? avatarPath)
        {
            if (!IsConfigured)
            {
                return (null, GetFriendlyErrorMessage("Supabase не настроен."));
            }

            try
            {
                string url = $"{_supabaseUrl}/auth/v1/user";
                using var request = new HttpRequestMessage(HttpMethod.Put, url);
                EnsureHeaders(request);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var dataPayload = new System.Collections.Generic.Dictionary<string, object>();
                if (username != null) dataPayload["username"] = username;
                if (avatarPath != null) dataPayload["avatar_path"] = avatarPath;

                var payload = new System.Collections.Generic.Dictionary<string, object>();
                if (password != null) payload["password"] = password;
                if (dataPayload.Count > 0) payload["data"] = dataPayload;

                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<SupabaseUser>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return (user, "");
                }

                string errMsg = await GetErrorMessageAsync(response);
                return (null, errMsg);
            }
            catch (Exception ex)
            {
                return (null, GetFriendlyErrorMessage(ex.Message));
            }
        }

        public async Task<(string? Email, string Error)> GetEmailByUsernameAsync(string username)
        {
            if (!IsConfigured)
            {
                return (null, GetFriendlyErrorMessage("Supabase не настроен."));
            }

            try
            {
                string url = $"{_supabaseUrl}/rest/v1/profiles?username=eq.{Uri.EscapeDataString(username.Trim())}&select=email";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                EnsureHeaders(request);

                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                    {
                        var first = doc.RootElement[0];
                        if (first.TryGetProperty("email", out var emailProp))
                        {
                            return (emailProp.GetString(), "");
                        }
                    }
                    return (null, "Пользователь с таким именем не найден.");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (null, GetFriendlyErrorMessage("Таблица профилей не найдена в Supabase."));
                }

                string errMsg = await GetErrorMessageAsync(response);
                return (null, errMsg);
            }
            catch (Exception ex)
            {
                return (null, GetFriendlyErrorMessage(ex.Message));
            }
        }

        public async Task<(bool Success, string Error)> CreateOrUpdateProfileAsync(string accessToken, string id, string username, string email)
        {
            if (!IsConfigured) return (false, GetFriendlyErrorMessage("Supabase не настроен."));
            try
            {
                string url = $"{_supabaseUrl}/rest/v1/profiles";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                EnsureHeaders(request);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("Prefer", "resolution=merge-duplicates");
                
                var payload = new
                {
                    id = id,
                    username = username,
                    email = email
                };

                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "");
                }
                return (false, await GetErrorMessageAsync(response));
            }
            catch (Exception ex)
            {
                return (false, GetFriendlyErrorMessage(ex.Message));
            }
        }
    }
}

