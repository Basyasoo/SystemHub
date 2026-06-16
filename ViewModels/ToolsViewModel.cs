using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.Runtime.InteropServices;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemHub.Services;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Media.Ocr;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SystemHub.ViewModels
{
    public class AudioDeviceItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class TempMailMessage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("from")]
        public string From { get; set; } = "";

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = "";

        [JsonPropertyName("date")]
        public string Date { get; set; } = "";

        [JsonPropertyName("textBody")]
        public string TextBody { get; set; } = "";

        [JsonPropertyName("htmlBody")]
        public string HtmlBody { get; set; } = "";
    }

    #region Mail.tm API Models

    public class MailTmDomainResponse
    {
        [JsonPropertyName("hydra:member")]
        public List<MailTmDomain>? Members { get; set; }
    }

    public class MailTmDomain
    {
        [JsonPropertyName("domain")]
        public string Domain { get; set; } = "";
    }

    public class MailTmTokenResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = "";
    }

    public class MailTmMessageListResponse
    {
        [JsonPropertyName("hydra:member")]
        public List<MailTmMessage>? Members { get; set; }
    }

    public class MailTmMessage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
    }

    public class MailTmMessageDetail
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("from")]
        public MailTmAddress? From { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("intro")]
        public string? Intro { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("html")]
        public List<string>? Html { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    public class MailTmAddress
    {
        [JsonPropertyName("address")]
        public string Address { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    #endregion

    public partial class ToDoItem : ObservableObject
    {
        [ObservableProperty]
        private string _text = "";

        [ObservableProperty]
        private bool _isCompleted;
    }

    public partial class ToolsViewModel : ViewModelBase
    {
        #region Win32 P/Invokes for Native Functionality

        [DllImport("user32.dll")]
        private static extern bool MessageBeep(uint uType);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("gdi32.dll")]
        private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, [Out] byte[] lpvBits, ref BITMAPINFO lpbmi, uint uUsage);

        private const int SRCCOPY = 0x00CC0020;

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public int bmiColors;
        }

        #endregion

        private readonly HttpClient _httpClient = new();
        private readonly DispatcherTimer _pomodoroTimer;
        private readonly DispatcherTimer _mailPollTimer;
        private bool _isLoadingSettings;

        // Pomodoro State
        [ObservableProperty]
        private double _workSessionMinutes = 25;

        [ObservableProperty]
        private double _breakSessionMinutes = 5;

        private int _totalSecondsRemaining = 25 * 60;
        private int _sessionDurationSeconds = 25 * 60;

        [ObservableProperty]
        private string _focusStateText = "";

        [ObservableProperty]
        private string _focusTimeDisplay = "25:00";

        [ObservableProperty]
        private double _focusProgress = 100.0;

        [ObservableProperty]
        private bool _isFocusRunning;

        [ObservableProperty]
        private bool _isWorkSession = true;

        // Temp Mail State
        [ObservableProperty]
        private string _tempMailAddress = "";

        private string? _mailToken;

        [ObservableProperty]
        private ObservableCollection<TempMailMessage> _mailMessages = new();

        [ObservableProperty]
        private TempMailMessage? _selectedMessage;

        [ObservableProperty]
        private bool _isMailLoading;

        // File Shredder State
        [ObservableProperty]
        private string _shredStatus = "";

        // QR Code Generator State
        [ObservableProperty]
        private string _qrInputText = "https://github.com/Basyasoo/SystemHub";

        [ObservableProperty]
        private Bitmap? _qrImage;

        // Date Calculator State
        [ObservableProperty]
        private DateTime? _date1 = DateTime.Now;

        [ObservableProperty]
        private DateTime? _date2 = DateTime.Now.AddDays(7);

        [ObservableProperty]
        private string _dateDiffResult = "";

        // To-Do List State
        [ObservableProperty]
        private string _newToDoText = "";

        [ObservableProperty]
        private ObservableCollection<ToDoItem> _toDoItems = new();

        // Image Converter State
        [ObservableProperty]
        private string _selectedImagePath = "";

        [ObservableProperty]
        private string _imageNameDisplay = "";

        // World Clocks State
        [ObservableProperty]
        private string _localTime = "";

        [ObservableProperty]
        private string _newYorkTime = "";

        [ObservableProperty]
        private string _londonTime = "";

        [ObservableProperty]
        private string _tokyoTime = "";

        [ObservableProperty]
        private string _localDate = "";

        [ObservableProperty]
        private string _newYorkDate = "";

        [ObservableProperty]
        private string _londonDate = "";

        [ObservableProperty]
        private string _tokyoDate = "";

        [ObservableProperty]
        private string _localIcon = "☀️";

        [ObservableProperty]
        private string _newYorkIcon = "☀️";

        [ObservableProperty]
        private string _londonIcon = "☀️";

        [ObservableProperty]
        private string _tokyoIcon = "☀️";

        [ObservableProperty]
        private string _newYorkOffset = "";

        [ObservableProperty]
        private string _londonOffset = "";

        [ObservableProperty]
        private string _tokyoOffset = "";

        // Custom Wallpaper State
        [ObservableProperty]
        private string _activeCustomWallpaperPath = "";

        // Volume Limiter & Sound Profiles State
        [ObservableProperty]
        private bool _isVolumeLimiterEnabled;

        [ObservableProperty]
        private double _maxVolumeLimit = 70.0;

        private readonly DispatcherTimer _volumeLimiterTimer;

        // Calculator State
        [ObservableProperty]
        private string _calculatorDisplay = "0";

        private string _currentNumber = "";
        private double? _operand1;
        private string? _pendingOperator;
        private double _lastOperand2;
        private string? _lastOperator;
        private bool _isOperatorJustPressed;

        public string CalculatorClearText => string.IsNullOrEmpty(_currentNumber) && _operand1 == null && _pendingOperator == null ? "AC" : "C";

        [RelayCommand]
        public void CalculatorPress(string button)
        {
            if (button == "C")
            {
                if (!string.IsNullOrEmpty(_currentNumber))
                {
                    _currentNumber = "";
                    CalculatorDisplay = "0";
                }
                else
                {
                    _operand1 = null;
                    _pendingOperator = null;
                    _lastOperand2 = 0;
                    _lastOperator = null;
                    _isOperatorJustPressed = false;
                    CalculatorDisplay = "0";
                }
                OnPropertyChanged(nameof(CalculatorClearText));
            }
            else if (button == "DEL")
            {
                if (!string.IsNullOrEmpty(_currentNumber) && _currentNumber != "0")
                {
                    _currentNumber = _currentNumber.Substring(0, _currentNumber.Length - 1);
                    if (string.IsNullOrEmpty(_currentNumber) || _currentNumber == "-")
                    {
                        _currentNumber = "";
                        CalculatorDisplay = "0";
                    }
                    else
                    {
                        CalculatorDisplay = _currentNumber;
                    }
                }
                OnPropertyChanged(nameof(CalculatorClearText));
            }
            else if (button == "±" || button == "+/-")
            {
                double currentVal = ParseDisplayValue();
                double newVal = -currentVal;
                _currentNumber = FormatDouble(newVal);
                CalculatorDisplay = _currentNumber;
                _isOperatorJustPressed = false;
                OnPropertyChanged(nameof(CalculatorClearText));
            }
            else if (button == "√")
            {
                double currentVal = ParseDisplayValue();
                if (currentVal < 0)
                {
                    CalculatorDisplay = LocalizationService.Instance.ToolsError;
                    _currentNumber = "";
                    _operand1 = null;
                    _pendingOperator = null;
                }
                else
                {
                    double newVal = Math.Sqrt(currentVal);
                    _currentNumber = FormatDouble(newVal);
                    CalculatorDisplay = _currentNumber;
                }
                _isOperatorJustPressed = false;
                OnPropertyChanged(nameof(CalculatorClearText));
            }
            else if (button == "%")
            {
                double currentVal = ParseDisplayValue();
                double newVal = currentVal / 100.0;
                _currentNumber = FormatDouble(newVal);
                CalculatorDisplay = _currentNumber;
                _isOperatorJustPressed = false;
                OnPropertyChanged(nameof(CalculatorClearText));
            }
            else if (button == "+" || button == "-" || button == "*" || button == "/" || button == "×" || button == "÷")
            {
                string op = button == "×" ? "*" : (button == "÷" ? "/" : button);
                double currentVal = ParseDisplayValue();

                if (_pendingOperator != null && !string.IsNullOrEmpty(_currentNumber))
                {
                    try
                    {
                        double result = PerformOperation(_operand1 ?? 0, currentVal, _pendingOperator);
                        CalculatorDisplay = FormatDouble(result);
                        _operand1 = result;
                    }
                    catch
                    {
                        CalculatorDisplay = LocalizationService.Instance.ToolsError;
                        _operand1 = null;
                        _pendingOperator = null;
                        _currentNumber = "";
                        _isOperatorJustPressed = false;
                        OnPropertyChanged(nameof(CalculatorClearText));
                        return;
                    }
                }
                else
                {
                    _operand1 = currentVal;
                }

                _pendingOperator = op;
                _currentNumber = "";
                _isOperatorJustPressed = true;
                OnPropertyChanged(nameof(CalculatorClearText));
            }
            else if (button == "=")
            {
                double currentVal = ParseDisplayValue();

                if (_pendingOperator != null)
                {
                    try
                    {
                        double result = PerformOperation(_operand1 ?? 0, currentVal, _pendingOperator);
                        CalculatorDisplay = FormatDouble(result);
                        _lastOperand2 = currentVal;
                        _lastOperator = _pendingOperator;
                        _operand1 = result;
                        _pendingOperator = null;
                    }
                    catch
                    {
                        CalculatorDisplay = LocalizationService.Instance.ToolsError;
                        _operand1 = null;
                        _pendingOperator = null;
                    }
                }
                else if (_lastOperator != null)
                {
                    try
                    {
                        double result = PerformOperation(currentVal, _lastOperand2, _lastOperator);
                        CalculatorDisplay = FormatDouble(result);
                        _operand1 = result;
                    }
                    catch
                    {
                        CalculatorDisplay = LocalizationService.Instance.ToolsError;
                        _operand1 = null;
                        _lastOperator = null;
                    }
                }

                _currentNumber = "";
                _isOperatorJustPressed = true;
                OnPropertyChanged(nameof(CalculatorClearText));
            }
            else // Digits and Dot
            {
                if (_isOperatorJustPressed)
                {
                    _currentNumber = "";
                    _isOperatorJustPressed = false;
                }

                if (button == ".")
                {
                    if (string.IsNullOrEmpty(_currentNumber))
                    {
                        _currentNumber = "0.";
                    }
                    else if (!_currentNumber.Contains("."))
                    {
                        _currentNumber += ".";
                    }
                }
                else
                {
                    if (_currentNumber == "0")
                    {
                        _currentNumber = button;
                    }
                    else
                    {
                        _currentNumber += button;
                    }
                }

                CalculatorDisplay = _currentNumber;
                OnPropertyChanged(nameof(CalculatorClearText));
            }
        }

        private double PerformOperation(double op1, double op2, string op)
        {
            return op switch
            {
                "+" => op1 + op2,
                "-" => op1 - op2,
                "*" => op1 * op2,
                "/" => op2 != 0 ? op1 / op2 : throw new DivideByZeroException(),
                _ => op2
            };
        }

        private double ParseDisplayValue()
        {
            if (string.IsNullOrEmpty(CalculatorDisplay) || CalculatorDisplay == LocalizationService.Instance.ToolsError) return 0;
            if (double.TryParse(CalculatorDisplay, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                return val;
            }
            if (double.TryParse(CalculatorDisplay, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out val))
            {
                return val;
            }
            return 0;
        }

        private string FormatDouble(double val)
        {
            if (double.IsNaN(val) || double.IsInfinity(val)) return LocalizationService.Instance.ToolsError;
            string str = val.ToString("G12", System.Globalization.CultureInfo.InvariantCulture);
            if (str.Contains("E") || str.Contains("e"))
            {
                return val.ToString("G8", System.Globalization.CultureInfo.InvariantCulture);
            }
            return str;
        }

        public ToolsViewModel()
        {
            // Pomodoro Setup
            _pomodoroTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _pomodoroTimer.Tick += PomodoroTimer_Tick;
            _sessionDurationSeconds = (int)WorkSessionMinutes * 60;
            _totalSecondsRemaining = _sessionDurationSeconds;
            UpdateFocusUI();

            // Temp Mail Setup
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _mailPollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
            _mailPollTimer.Tick += MailPollTimer_Tick;

            // Clocks Timer Setup
            var clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            clockTimer.Tick += ClockTimer_Tick;
            clockTimer.Start();
            UpdateClocks();

            // Generate initial QR Code
            _ = GenerateQrCode();

            // Populate Audio Output Devices
            RefreshAudioDevices();

            // Initialize presets
            InitializePresets();

            // Load saved settings
            LoadToolsSettings();

            ImageNameDisplay = LocalizationService.Instance.ToolsFileNotSelected;

            // Subscribe to language changes
            LocalizationService.Instance.PropertyChanged += (sender, args) =>
            {
                InitializePresets();
                CalculateDateDiff();
                if (string.IsNullOrEmpty(SelectedImagePath))
                {
                    ImageNameDisplay = LocalizationService.Instance.ToolsFileNotSelected;
                }
            };

            // Volume Limiter Timer Setup
            _volumeLimiterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _volumeLimiterTimer.Tick += VolumeLimiterTimer_Tick;
            _volumeLimiterTimer.Start();

            // Fallback process exit handler to ensure audio device is restored
            AppDomain.CurrentDomain.ProcessExit += (s, e) => CleanupOnExit();
        }

        #region Focus / Pomodoro Timer

        partial void OnWorkSessionMinutesChanged(double value)
        {
            if (value < 1) return;
            if (IsWorkSession && !IsFocusRunning)
            {
                _sessionDurationSeconds = (int)value * 60;
                _totalSecondsRemaining = _sessionDurationSeconds;
                UpdateFocusUI();
            }
            SaveFocusTimerSettings();
        }

        partial void OnBreakSessionMinutesChanged(double value)
        {
            if (value < 1) return;
            if (!IsWorkSession && !IsFocusRunning)
            {
                _sessionDurationSeconds = (int)value * 60;
                _totalSecondsRemaining = _sessionDurationSeconds;
                UpdateFocusUI();
            }
            SaveFocusTimerSettings();
        }

        [RelayCommand]
        public void ToggleFocus()
        {
            IsFocusRunning = !IsFocusRunning;
            if (IsFocusRunning)
            {
                _pomodoroTimer.Start();
            }
            else
            {
                _pomodoroTimer.Stop();
            }
        }

        [RelayCommand]
        public void ResetFocus()
        {
            _pomodoroTimer.Stop();
            IsFocusRunning = false;
            IsWorkSession = true;
            _sessionDurationSeconds = (int)WorkSessionMinutes * 60;
            _totalSecondsRemaining = _sessionDurationSeconds;
            UpdateFocusUI();
        }

        private void PomodoroTimer_Tick(object? sender, EventArgs e)
        {
            if (_totalSecondsRemaining > 0)
            {
                _totalSecondsRemaining--;
                UpdateFocusUI();
            }
            else
            {
                // Play notification sound
                try
                {
                    MessageBeep(0x00000030); // MB_ICONEXCLAMATION
                }
                catch { }

                // Switch session types
                IsWorkSession = !IsWorkSession;
                _sessionDurationSeconds = IsWorkSession ? ((int)WorkSessionMinutes * 60) : ((int)BreakSessionMinutes * 60);
                _totalSecondsRemaining = _sessionDurationSeconds;
                UpdateFocusUI();
            }
        }

        private void UpdateFocusUI()
        {
            int m = _totalSecondsRemaining / 60;
            int s = _totalSecondsRemaining % 60;
            FocusTimeDisplay = $"{m:D2}:{s:D2}";
            FocusProgress = ((double)_totalSecondsRemaining / _sessionDurationSeconds) * 100.0;

            FocusStateText = IsWorkSession
                ? LocalizationService.Instance.FocusTimerWork
                : LocalizationService.Instance.FocusTimerBreak;
        }

        #endregion

        #region Temp Mail

        [RelayCommand]
        public async Task GenerateTempMail()
        {
            IsMailLoading = true;
            TempMailAddress = LocalizationService.Instance.ToolsMailCreatingInbox;
            try
            {
                // 1. Get domains
                var domainsJson = await _httpClient.GetStringAsync("https://api.mail.tm/domains");
                var domainsResponse = JsonSerializer.Deserialize<MailTmDomainResponse>(domainsJson);
                
                if (domainsResponse?.Members != null && domainsResponse.Members.Count > 0)
                {
                    string domain = domainsResponse.Members[0].Domain;
                    string randomUser = "syshub_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    string randomPass = Guid.NewGuid().ToString("N").Substring(0, 12);
                    string email = $"{randomUser}@{domain}";

                    // 2. Create account
                    var accountBody = JsonSerializer.Serialize(new { address = email, password = randomPass });
                    var accountContent = new StringContent(accountBody, Encoding.UTF8, "application/json");
                    var accountResponse = await _httpClient.PostAsync("https://api.mail.tm/accounts", accountContent);
                    accountResponse.EnsureSuccessStatusCode();

                    // 3. Get JWT token
                    var tokenResponse = await _httpClient.PostAsync("https://api.mail.tm/token", accountContent);
                    tokenResponse.EnsureSuccessStatusCode();
                    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                    var tokenData = JsonSerializer.Deserialize<MailTmTokenResponse>(tokenJson);

                    if (tokenData != null && !string.IsNullOrEmpty(tokenData.Token))
                    {
                        _mailToken = tokenData.Token;
                        TempMailAddress = email;
                        MailMessages.Clear();
                        SelectedMessage = null;
                        _mailPollTimer.Start();
                        await FetchEmailsAsync();
                    }
                    else
                    {
                        TempMailAddress = LocalizationService.Instance.ToolsMailAuthError;
                    }
                }
                else
                {
                    TempMailAddress = LocalizationService.Instance.ToolsMailDomainError;
                }
            }
            catch (Exception ex)
            {
                TempMailAddress = LocalizationService.Instance.ToolsMailNetworkError + ex.Message;
            }
            finally
            {
                IsMailLoading = false;
            }
        }

        private async void MailPollTimer_Tick(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_mailToken))
            {
                await FetchEmailsAsync();
            }
        }

        private async Task FetchEmailsAsync()
        {
            if (string.IsNullOrEmpty(_mailToken)) return;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.mail.tm/messages");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _mailToken);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                
                var mailResponse = JsonSerializer.Deserialize<MailTmMessageListResponse>(json);
                if (mailResponse?.Members != null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var existingIds = MailMessages.Select(m => m.Id).ToList();
                        foreach (var msg in mailResponse.Members)
                        {
                            if (!existingIds.Contains(msg.Id))
                            {
                                // Fetch message body details asynchronously
                                _ = LoadMessageDetailsAndAdd(msg);
                            }
                        }
                    });
                }
            }
            catch { }
        }

        private async Task LoadMessageDetailsAndAdd(MailTmMessage msg)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.mail.tm/messages/{msg.Id}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _mailToken);
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                
                var detailed = JsonSerializer.Deserialize<MailTmMessageDetail>(json);
                if (detailed != null)
                {
                    var tempMsg = new TempMailMessage
                    {
                        Id = msg.Id,
                        From = detailed.From?.Address ?? "",
                        Subject = detailed.Subject ?? "",
                        Date = detailed.CreatedAt.ToLocalTime().ToString("HH:mm:ss"),
                        TextBody = detailed.Text ?? detailed.Intro ?? "",
                        HtmlBody = (detailed.Html != null && detailed.Html.Count > 0) ? detailed.Html[0] : ""
                    };

                    Dispatcher.UIThread.Post(() =>
                    {
                        MailMessages.Insert(0, tempMsg);
                    });
                }
            }
            catch { }
        }

        [RelayCommand]
        public void CopyMailToClipboard()
        {
            if (!string.IsNullOrEmpty(TempMailAddress))
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow != null)
                {
                    desktop.MainWindow.Clipboard?.SetTextAsync(TempMailAddress);
                }
                ShredStatus = LocalizationService.Instance.TempMailCopied;
                Task.Delay(2000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
            }
        }

        public async Task ShredFiles(IEnumerable<string> paths)
        {
            ShredStatus = LocalizationService.Instance.ToolsShredderErasing;
            await Task.Run(() =>
            {
                foreach (var path in paths)
                {
                    try
                    {
                        if (File.Exists(path))
                        {
                            ShredFile(path);
                        }
                        else if (Directory.Exists(path))
                        {
                            ShredDirectory(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.UIThread.Post(() => ShredStatus = $"{LocalizationService.Instance.ToolsShredderError}{ex.Message}");
                        return;
                    }
                }
                Dispatcher.UIThread.Post(() => ShredStatus = LocalizationService.Instance.FileShredderSuccess);
            });
 
            await Task.Delay(3000);
            ShredStatus = "";
        }

        private void ShredDirectory(string dirPath)
        {
            foreach (var file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
            {
                ShredFile(file);
            }
            Directory.Delete(dirPath, true);
        }

        private void ShredFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) return;

            // Remove read-only attributes
            File.SetAttributes(filePath, FileAttributes.Normal);

            // DoD 5220.22-M 3-pass Shredding
            long length = fileInfo.Length;
            byte[] buffer = new byte[65536]; // 64KB buffer

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                // Pass 1: Write Zeros
                Array.Clear(buffer, 0, buffer.Length);
                WritePass(stream, buffer, length);

                // Pass 2: Write Ones
                for (int i = 0; i < buffer.Length; i++) buffer[i] = 0xFF;
                WritePass(stream, buffer, length);

                // Pass 3: Write Random Bytes
                var rand = new Random();
                rand.NextBytes(buffer);
                WritePass(stream, buffer, length);

                stream.SetLength(0);
            }
            File.Delete(filePath);
        }

        private void WritePass(FileStream stream, byte[] buffer, long totalLength)
        {
            stream.Position = 0;
            long written = 0;
            while (written < totalLength)
            {
                int toWrite = (int)Math.Min(buffer.Length, totalLength - written);
                stream.Write(buffer, 0, toWrite);
                written += toWrite;
            }
            stream.Flush();
        }

        #endregion

        #region QR Code Generator

        [RelayCommand]
        public async Task GenerateQrCode()
        {
            if (string.IsNullOrWhiteSpace(QrInputText)) return;

            try
            {
                string encoded = Uri.EscapeDataString(QrInputText);
                string url = $"https://api.qrserver.com/v1/create-qr-code/?size=160x160&data={encoded}";
                var bytes = await _httpClient.GetByteArrayAsync(url);
                using (var ms = new MemoryStream(bytes))
                {
                    QrImage = new Bitmap(ms);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("QR Code Generation failed: " + ex.Message);
            }
        }

        #endregion

        #region Date Calculator

        partial void OnDate1Changed(DateTime? value) => CalculateDateDiff();
        partial void OnDate2Changed(DateTime? value) => CalculateDateDiff();

        private string FormatDaysPlural(int days)
        {
            if (LocalizationService.Instance.CurrentLanguage == "EN")
            {
                return days == 1 ? "1 day" : $"{days} days";
            }
            if (LocalizationService.Instance.CurrentLanguage == "ZH")
            {
                return $"{days} 天";
            }
            // Russian pluralization rules
            int mod10 = days % 10;
            int mod100 = days % 100;
            if (mod100 >= 11 && mod100 <= 19)
            {
                return $"{days} {LocalizationService.Instance.ToolsDaysPluralMany}";
            }
            if (mod10 == 1)
            {
                return $"{days} {LocalizationService.Instance.ToolsDaysPluralOne}";
            }
            if (mod10 >= 2 && mod10 <= 4)
            {
                return $"{days} {LocalizationService.Instance.ToolsDaysPluralTwoFour}";
            }
            return $"{days} {LocalizationService.Instance.ToolsDaysPluralMany}";
        }

        private void CalculateDateDiff()
        {
            if (Date1 == null || Date2 == null)
            {
                DateDiffResult = "-";
                return;
            }
 
            var diff = (Date2.Value - Date1.Value).Duration();
            int days = (int)diff.TotalDays;
            
            DateDiffResult = FormatDaysPlural(days);
        }

        #endregion

        #region To-Do List

        [RelayCommand]
        public void AddToDoItem()
        {
            if (!string.IsNullOrWhiteSpace(NewToDoText))
            {
                var newItem = new ToDoItem { Text = NewToDoText.Trim(), IsCompleted = false };
                newItem.PropertyChanged += ToDoItem_PropertyChanged;
                ToDoItems.Add(newItem);
                SaveToDoList();
                NewToDoText = "";
            }
        }

        [RelayCommand]
        public void RemoveToDoItem(ToDoItem item)
        {
            item.PropertyChanged -= ToDoItem_PropertyChanged;
            ToDoItems.Remove(item);
            SaveToDoList();
        }

        private void ToDoItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ToDoItem.IsCompleted) || e.PropertyName == nameof(ToDoItem.Text))
            {
                SaveToDoList();
            }
        }

        #endregion

        #region Clocks & Timezones

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            UpdateClocks();
        }

        private void UpdateClocks()
        {
            var utcNow = DateTime.UtcNow;

            var culture = LocalizationService.Instance.CurrentLanguage switch
            {
                "EN" => System.Globalization.CultureInfo.GetCultureInfo("en-US"),
                "ZH" => System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),
                _ => System.Globalization.CultureInfo.GetCultureInfo("ru-RU")
            };

            // Local Time
            var localTimeNow = DateTime.Now;
            LocalTime = localTimeNow.ToString("HH:mm:ss");
            LocalDate = localTimeNow.ToString("dddd, d MMMM", culture);
            LocalIcon = (localTimeNow.Hour >= 6 && localTimeNow.Hour < 18) ? "☀️" : "🌙";

            try
            {
                // Timezones using TimeZoneInfo (Windows IDs)
                TimeZoneInfo nyZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                TimeZoneInfo lonZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
                TimeZoneInfo tokZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

                var nyTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, nyZone);
                var lonTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, lonZone);
                var tokTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tokZone);

                NewYorkTime = nyTime.ToString("HH:mm:ss");
                NewYorkDate = nyTime.ToString("dddd, d MMMM", culture);
                NewYorkIcon = (nyTime.Hour >= 6 && nyTime.Hour < 18) ? "☀️" : "🌙";

                LondonTime = lonTime.ToString("HH:mm:ss");
                LondonDate = lonTime.ToString("dddd, d MMMM", culture);
                LondonIcon = (lonTime.Hour >= 6 && lonTime.Hour < 18) ? "☀️" : "🌙";

                TokyoTime = tokTime.ToString("HH:mm:ss");
                TokyoDate = tokTime.ToString("dddd, d MMMM", culture);
                TokyoIcon = (tokTime.Hour >= 6 && tokTime.Hour < 18) ? "☀️" : "🌙";

                // Offsets compared to local time
                double nyDiff = (nyTime - localTimeNow).TotalHours;
                double lonDiff = (lonTime - localTimeNow).TotalHours;
                double tokDiff = (tokTime - localTimeNow).TotalHours;

                NewYorkOffset = FormatOffsetDiff(nyDiff);
                LondonOffset = FormatOffsetDiff(lonDiff);
                TokyoOffset = FormatOffsetDiff(tokDiff);
            }
            catch
            {
                // Fallback standard offsets if system timezone database fails
                NewYorkTime = utcNow.AddHours(-4).ToString("HH:mm:ss");
                NewYorkDate = utcNow.AddHours(-4).ToString("dddd, d MMMM", culture);
                NewYorkIcon = (utcNow.AddHours(-4).Hour >= 6 && utcNow.AddHours(-4).Hour < 18) ? "☀️" : "🌙";

                LondonTime = utcNow.AddHours(1).ToString("HH:mm:ss");
                LondonDate = utcNow.AddHours(1).ToString("dddd, d MMMM", culture);
                LondonIcon = (utcNow.AddHours(1).Hour >= 6 && utcNow.AddHours(1).Hour < 18) ? "☀️" : "🌙";

                TokyoTime = utcNow.AddHours(9).ToString("HH:mm:ss");
                TokyoDate = utcNow.AddHours(9).ToString("dddd, d MMMM", culture);
                TokyoIcon = (utcNow.AddHours(9).Hour >= 6 && utcNow.AddHours(9).Hour < 18) ? "☀️" : "🌙";

                NewYorkOffset = "EST";
                LondonOffset = "GMT";
                TokyoOffset = "JST";
            }
        }

        private string FormatOffsetDiff(double diffHours)
        {
            int rounded = (int)Math.Round(diffHours);
            if (rounded == 0)
            {
                return LocalizationService.Instance.CurrentLanguage switch
                {
                    "EN" => "same as local",
                    "ZH" => "与本地时间相同",
                    _ => "совпадает с местным"
                };
            }

            string prefix = rounded > 0 ? "+" : "";
            return LocalizationService.Instance.CurrentLanguage switch
            {
                "EN" => $"{prefix}{rounded}h from local",
                "ZH" => $"比本地 {prefix}{rounded} 小时",
                _ => $"{prefix}{rounded}ч от местного"
            };
        }

        [ObservableProperty]
        private int _selectedClockTab = 0;

        public bool IsClockTab0 => SelectedClockTab == 0;
        public bool IsClockTab1 => SelectedClockTab == 1;
        public bool IsClockTab2 => SelectedClockTab == 2;

        partial void OnSelectedClockTabChanged(int value)
        {
            OnPropertyChanged(nameof(IsClockTab0));
            OnPropertyChanged(nameof(IsClockTab1));
            OnPropertyChanged(nameof(IsClockTab2));
        }

        [RelayCommand]
        public void SetClockTab(string tabIndexStr)
        {
            if (int.TryParse(tabIndexStr, out int idx))
            {
                SelectedClockTab = idx;
            }
        }

        // Stopwatch State
        private DispatcherTimer? _stopwatchTimer;
        private DateTime _stopwatchStartTime;
        private TimeSpan _stopwatchElapsed = TimeSpan.Zero;

        [ObservableProperty]
        private string _stopwatchDisplay = "00:00.00";

        [ObservableProperty]
        private bool _isStopwatchRunning;

        [RelayCommand]
        public void ToggleStopwatch()
        {
            if (_stopwatchTimer == null)
            {
                _stopwatchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
                _stopwatchTimer.Tick += (s, e) =>
                {
                    var elapsed = DateTime.UtcNow - _stopwatchStartTime + _stopwatchElapsed;
                    StopwatchDisplay = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds / 10:D2}";
                };
            }

            IsStopwatchRunning = !IsStopwatchRunning;
            if (IsStopwatchRunning)
            {
                _stopwatchStartTime = DateTime.UtcNow;
                _stopwatchTimer.Start();
            }
            else
            {
                _stopwatchTimer.Stop();
                _stopwatchElapsed += DateTime.UtcNow - _stopwatchStartTime;
            }
        }

        [RelayCommand]
        public void ResetStopwatch()
        {
            _stopwatchTimer?.Stop();
            IsStopwatchRunning = false;
            _stopwatchElapsed = TimeSpan.Zero;
            StopwatchDisplay = "00:00.00";
        }

        // Countdown Timer State
        private DispatcherTimer? _countdownTimer;
        private int _countdownTotalSeconds = 5 * 60;
        private int _countdownSecondsRemaining = 5 * 60;

        [ObservableProperty]
        private string _countdownDisplay = "05:00";

        [ObservableProperty]
        private double _countdownProgress = 100.0;

        [ObservableProperty]
        private bool _isCountdownRunning;

        [ObservableProperty]
        private double _countdownInputMinutes = 5;

        partial void OnCountdownInputMinutesChanged(double value)
        {
            if (value < 1) return;
            if (!IsCountdownRunning)
            {
                _countdownTotalSeconds = (int)value * 60;
                _countdownSecondsRemaining = _countdownTotalSeconds;
                UpdateCountdownUI();
            }
            SaveCountdownTimerSettings();
        }

        [RelayCommand]
        public void ToggleCountdown()
        {
            if (_countdownTimer == null)
            {
                _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _countdownTimer.Tick += (s, e) =>
                {
                    if (_countdownSecondsRemaining > 0)
                    {
                        _countdownSecondsRemaining--;
                        UpdateCountdownUI();
                    }
                    else
                    {
                        _countdownTimer.Stop();
                        IsCountdownRunning = false;
                        try { MessageBeep(0x00000030); } catch { }
                        ShredStatus = LocalizationService.Instance.ToolsTimerCountdownDone;
                        Task.Delay(3000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
                    }
                };
            }

            IsCountdownRunning = !IsCountdownRunning;
            if (IsCountdownRunning)
            {
                _countdownTimer.Start();
            }
            else
            {
                _countdownTimer.Stop();
            }
        }

        [RelayCommand]
        public void ResetCountdown()
        {
            _countdownTimer?.Stop();
            IsCountdownRunning = false;
            _countdownTotalSeconds = (int)CountdownInputMinutes * 60;
            _countdownSecondsRemaining = _countdownTotalSeconds;
            UpdateCountdownUI();
        }

        private void UpdateCountdownUI()
        {
            int m = _countdownSecondsRemaining / 60;
            int s = _countdownSecondsRemaining % 60;
            CountdownDisplay = $"{m:D2}:{s:D2}";
            CountdownProgress = _countdownTotalSeconds > 0 
                ? ((double)_countdownSecondsRemaining / _countdownTotalSeconds) * 100.0 
                : 100.0;
        }

        #endregion

        #region Tools Sub-Tabs Navigation

        [ObservableProperty]
        private int _selectedToolsTab = 0; // 0 = Focus, 1 = Utilities, 2 = Sounds

        public bool IsToolsTab0 => SelectedToolsTab == 0;
        public bool IsToolsTab1 => SelectedToolsTab == 1;
        public bool IsToolsTab2 => SelectedToolsTab == 2;

        partial void OnSelectedToolsTabChanged(int value)
        {
            OnPropertyChanged(nameof(IsToolsTab0));
            OnPropertyChanged(nameof(IsToolsTab1));
            OnPropertyChanged(nameof(IsToolsTab2));
        }

        [RelayCommand]
        public void SetToolsTab(string tabIndexStr)
        {
            if (int.TryParse(tabIndexStr, out int idx))
            {
                SelectedToolsTab = idx;
            }
        }

        #endregion



        #region Screen OCR (Распознавание текста)

        [RelayCommand]
        public async Task StartOcr()
        {
            ShredStatus = LocalizationService.Instance.ToolsOcrHighlightPrompt;
            
            var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var mainWin = desktop?.MainWindow;
            
            try
            {
                // 1. Hide main window
                if (mainWin != null)
                {
                    mainWin.Hide();
                }

                // Wait for the window to hide completely and screen to redraw
                await Task.Delay(300);

                // 2. Capture full screen
                var cap = CaptureScreen();
                var screenShot = CapturedScreenToAvaloniaBitmap(cap);

                // 3. Show the interactive cropping overlay
                var overlay = new SystemHub.Views.OcrOverlayWindow(screenShot);
                var tcs = new TaskCompletionSource<bool>();
                overlay.Closed += (s, e) => tcs.TrySetResult(true);
                overlay.Show();
                await tcs.Task;

                // 4. Get the selected rect
                var selectRect = overlay.SelectedRect;

                // 5. Process selection
                if (selectRect.HasValue)
                {
                    var rect = selectRect.Value;
                    int x = (int)rect.X;
                    int y = (int)rect.Y;
                    int cropW = (int)rect.Width;
                    int cropH = (int)rect.Height;

                    ShredStatus = LocalizationService.Instance.ToolsOcrRecognizing;

                    await Task.Run(async () =>
                    {
                        try
                        {
                            var croppedBytes = CropBgraBytes(cap.PixelBytes, cap.Width, cap.Height, x, y, cropW, cropH);
                            var uwpBitmap = await BytesToSoftwareBitmap(croppedBytes, cropW, cropH);

                            if (uwpBitmap != null)
                            {
                                var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                                if (ocrEngine != null)
                                {
                                    var result = await ocrEngine.RecognizeAsync(uwpBitmap);
                                    string text = result.Text;

                                    Dispatcher.UIThread.Post(() =>
                                    {
                                        if (string.IsNullOrWhiteSpace(text))
                                        {
                                            ShredStatus = LocalizationService.Instance.ToolsOcrNotFound;
                                        }
                                        else
                                        {
                                            if (mainWin != null)
                                            {
                                                mainWin.Clipboard?.SetTextAsync(text);
                                            }
                                            ShredStatus = LocalizationService.Instance.ToolsOcrCopiedSuccess;
                                        }
                                    });
                                }
                                else
                                {
                                    Dispatcher.UIThread.Post(() => ShredStatus = LocalizationService.Instance.ToolsOcrPackNotInstalled);
                                }
                            }
                            else
                            {
                                Dispatcher.UIThread.Post(() => ShredStatus = LocalizationService.Instance.ToolsOcrProcessingError);
                            }
                        }
                        catch (Exception ocrEx)
                        {
                            Dispatcher.UIThread.Post(() => ShredStatus = LocalizationService.Instance.ToolsOcrGenericError + ocrEx.Message);
                        }
                    });
                }
                else
                {
                    ShredStatus = LocalizationService.Instance.ToolsOcrSelectionCanceled;
                }
            }
            catch (Exception ex)
            {
                ShredStatus = LocalizationService.Instance.ToolsError + ": " + ex.Message;
            }
            finally
            {
                // 6. Restore the main window
                if (mainWin != null)
                {
                    mainWin.Show();
                    mainWin.Focus();
                }
            }

            await Task.Delay(4000);
            ShredStatus = "";
        }

        private struct CapturedScreen
        {
            public byte[] PixelBytes;
            public int Width;
            public int Height;
        }

        private static CapturedScreen CaptureScreen()
        {
            int w = GetSystemMetrics(0); // SM_CXSCREEN
            int h = GetSystemMetrics(1); // SM_CYSCREEN

            IntPtr hwndDesk = GetDesktopWindow();
            IntPtr hdcDesk = GetWindowDC(hwndDesk);
            IntPtr hdcMem = CreateCompatibleDC(hdcDesk);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcDesk, w, h);
            IntPtr hOld = SelectObject(hdcMem, hBitmap);

            BitBlt(hdcMem, 0, 0, w, h, hdcDesk, 0, 0, SRCCOPY);

            var bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
            bmi.bmiHeader.biWidth = w;
            bmi.bmiHeader.biHeight = -h; // top-down
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0; // BI_RGB

            byte[] pixelBytes = new byte[w * h * 4];
            GetDIBits(hdcMem, hBitmap, 0, (uint)h, pixelBytes, ref bmi, 0);

            // Clean up GDI
            SelectObject(hdcMem, hOld);
            DeleteObject(hBitmap);
            DeleteDC(hdcMem);
            ReleaseDC(hwndDesk, hdcDesk);

            return new CapturedScreen
            {
                PixelBytes = pixelBytes,
                Width = w,
                Height = h
            };
        }

        private static Bitmap CapturedScreenToAvaloniaBitmap(CapturedScreen cap)
        {
            var wbmp = new WriteableBitmap(
                new Avalonia.PixelSize(cap.Width, cap.Height),
                new Avalonia.Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888,
                Avalonia.Platform.AlphaFormat.Premul);

            using (var buf = wbmp.Lock())
            {
                Marshal.Copy(cap.PixelBytes, 0, buf.Address, cap.PixelBytes.Length);
            }
            return wbmp;
        }

        private static byte[] CropBgraBytes(byte[] srcBytes, int srcWidth, int srcHeight, int x, int y, int cropW, int cropH)
        {
            byte[] destBytes = new byte[cropW * cropH * 4];
            for (int row = 0; row < cropH; row++)
            {
                int srcOffset = ((y + row) * srcWidth + x) * 4;
                int destOffset = row * cropW * 4;
                
                if (srcOffset >= 0 && srcOffset + cropW * 4 <= srcBytes.Length)
                {
                    Array.Copy(srcBytes, srcOffset, destBytes, destOffset, cropW * 4);
                }
            }
            return destBytes;
        }

        private static async Task<SoftwareBitmap?> BytesToSoftwareBitmap(byte[] bgraBytes, int width, int height)
        {
            using (var stream = new InMemoryRandomAccessStream())
            {
                using (var writer = new DataWriter(stream))
                {
                    // Write BMP File Header (14 bytes)
                    writer.WriteBytes(new byte[] { 0x42, 0x4D });
                    writer.WriteInt32(14 + 40 + bgraBytes.Length);
                    writer.WriteInt16(0);
                    writer.WriteInt16(0);
                    writer.WriteInt32(14 + 40);

                    // Write DIB Header (40 bytes)
                    writer.WriteInt32(40);
                    writer.WriteInt32(width);
                    writer.WriteInt32(-height); // negative for top-down
                    writer.WriteInt16(1);
                    writer.WriteInt16(32);
                    writer.WriteInt32(0); // BI_RGB
                    writer.WriteInt32(bgraBytes.Length);
                    writer.WriteInt32(0);
                    writer.WriteInt32(0);
                    writer.WriteInt32(0);
                    writer.WriteInt32(0);

                    // Write Pixels
                    writer.WriteBytes(bgraBytes);
                    
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                }
                
                stream.Seek(0);
                var decoder = await BitmapDecoder.CreateAsync(stream);
                return await decoder.GetSoftwareBitmapAsync();
            }
        }

        #endregion

        #region Global Parametric Equalizer Simulation

        public ObservableCollection<AudioDeviceItem> AudioDevices { get; } = new();

        [ObservableProperty]
        private AudioDeviceItem? _selectedAudioDevice;

        public void RefreshAudioDevices()
        {
            AudioDevices.Clear();
            try
            {
                var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var renders = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                
                NAudio.CoreAudioApi.MMDevice? defaultDevice = null;
                try
                {
                    defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                }
                catch { }

                AudioDeviceItem? defaultItem = null;

                foreach (var r in renders)
                {
                    if (r.FriendlyName.Contains("CABLE Input", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var item = new AudioDeviceItem
                    {
                        Id = r.ID,
                        Name = r.FriendlyName
                    };
                    AudioDevices.Add(item);

                    if (defaultDevice != null && r.ID == defaultDevice.ID)
                    {
                        defaultItem = item;
                    }
                }

                if (defaultItem != null)
                {
                    SelectedAudioDevice = defaultItem;
                }
                else if (AudioDevices.Count > 0)
                {
                    SelectedAudioDevice = AudioDevices[0];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error enumerating audio devices: " + ex.Message);
            }
        }

        partial void OnSelectedAudioDeviceChanged(AudioDeviceItem? value)
        {
            if (IsEqualizerEnabled)
            {
                StartGlobalEq();
            }
            SaveEqualizerSettings();
        }

        private WasapiLoopbackCapture? _globalCapture;
        private WasapiOut? _globalOutput;
        private BufferedWaveProvider? _globalBufferedProvider;
        private EqualizerSampleProvider? _globalEqProvider;
        private string? _originalDefaultDeviceId;
        private bool _isVirtualCableInstallerRunning = false;
        private System.Diagnostics.Process? _watchdogProcess;

        [ObservableProperty]
        private ObservableCollection<EqualizerPreset> _equalizerPresets = new();

        [ObservableProperty]
        private EqualizerPreset? _selectedEqualizerPreset;

        [ObservableProperty]
        private string _newPresetName = "";

        public bool IsCustomPresetSelected => SelectedEqualizerPreset?.IsCustom == true;

        [ObservableProperty]
        private bool _isEqualizerEnabled;

        [ObservableProperty]
        private double _masterEqGain = 0.0;

        [ObservableProperty]
        private double _bassBoostLevel = 4;

        [ObservableProperty]
        private double _eq60Hz = 0.0;

        [ObservableProperty]
        private double _eq170Hz = 0.0;

        [ObservableProperty]
        private double _eq310Hz = 0.0;

        [ObservableProperty]
        private double _eq600Hz = 0.0;

        [ObservableProperty]
        private double _eq1kHz = 0.0;

        [ObservableProperty]
        private double _eq3kHz = 0.0;

        [ObservableProperty]
        private double _eq6kHz = 0.0;

        [ObservableProperty]
        private double _eq12kHz = 0.0;

        [ObservableProperty]
        private double _eq14kHz = 0.0;

        [ObservableProperty]
        private double _eq16kHz = 0.0;

        private static ISampleProvider MatchFormat(ISampleProvider source, WaveFormat targetFormat)
        {
            ISampleProvider current = source;
            if (current.WaveFormat.SampleRate != targetFormat.SampleRate)
            {
                current = new NAudio.Wave.SampleProviders.WdlResamplingSampleProvider(current, targetFormat.SampleRate);
            }
            if (current.WaveFormat.Channels != targetFormat.Channels)
            {
                if (current.WaveFormat.Channels == 1 && targetFormat.Channels == 2)
                {
                    current = new NAudio.Wave.SampleProviders.MonoToStereoSampleProvider(current);
                }
                else if (current.WaveFormat.Channels == 2 && targetFormat.Channels == 1)
                {
                    current = new NAudio.Wave.SampleProviders.StereoToMonoSampleProvider(current);
                }
            }
            return current;
        }

        private (NAudio.CoreAudioApi.MMDevice? inputCable, NAudio.CoreAudioApi.MMDevice? outputCable) FindCableDevices()
        {
            var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            NAudio.CoreAudioApi.MMDevice? inputCable = null;
            NAudio.CoreAudioApi.MMDevice? outputCable = null;

            try
            {
                var renders = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                foreach (var r in renders)
                {
                    if (r.FriendlyName.Contains("CABLE Input"))
                    {
                        inputCable = r;
                        break;
                    }
                }

                var captures = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                foreach (var c in captures)
                {
                    if (c.FriendlyName.Contains("CABLE Output"))
                    {
                        outputCable = c;
                        break;
                    }
                }
            }
            catch { }

            return (inputCable, outputCable);
        }

        private async Task<bool> CheckAndInstallVirtualCableAsync()
        {
            if (_isVirtualCableInstallerRunning) return false;
            _isVirtualCableInstallerRunning = true;

            ShredStatus = LocalizationService.Instance.ToolsCableInstalling;

            try
            {
                string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempCableSetup");
                Directory.CreateDirectory(tempDir);
                string zipPath = Path.Combine(tempDir, "vbcable.zip");

                ShredStatus = LocalizationService.Instance.ToolsCableDownloading;
                using (var client = new HttpClient())
                {
                    var bytes = await client.GetByteArrayAsync("https://download.vb-audio.com/Download_CABLE/VBCABLE_Driver_Pack45.zip");
                    await File.WriteAllBytesAsync(zipPath, bytes);
                }

                ShredStatus = LocalizationService.Instance.ToolsCableExtracting;
                string extractDir = Path.Combine(tempDir, "extracted");
                if (Directory.Exists(extractDir))
                {
                    Directory.Delete(extractDir, true);
                }
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir);

                ShredStatus = LocalizationService.Instance.ToolsCableLaunchingUac;
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Path.Combine(extractDir, "VBCABLE_Setup_x64.exe"),
                    Arguments = "-i -h",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                var proc = System.Diagnostics.Process.Start(psi);
                if (proc != null)
                {
                    await proc.WaitForExitAsync();
                }

                ShredStatus = LocalizationService.Instance.ToolsCableInstallSuccess;
                await Task.Delay(2000);
                ShredStatus = "";
                _isVirtualCableInstallerRunning = false;
                return true;
            }
            catch (Exception ex)
            {
                ShredStatus = LocalizationService.Instance.ToolsCableInstallError + ex.Message;
                await Task.Delay(3000);
                ShredStatus = "";
                _isVirtualCableInstallerRunning = false;
                return false;
            }
        }

        private void DisableListenToDevice(NAudio.CoreAudioApi.MMDevice device)
        {
            try
            {
                var fieldDeviceInterface = typeof(NAudio.CoreAudioApi.MMDevice).GetField("deviceInterface", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldDeviceInterface == null) return;
                var rawDeviceInterface = fieldDeviceInterface.GetValue(device);
                if (rawDeviceInterface == null) return;

                var typeIMMDevice = typeof(NAudio.CoreAudioApi.MMDevice).Assembly.GetType("NAudio.CoreAudioApi.Interfaces.IMMDevice");
                var typeIPropertyStore = typeof(NAudio.CoreAudioApi.MMDevice).Assembly.GetType("NAudio.CoreAudioApi.Interfaces.IPropertyStore");
                if (typeIMMDevice == null || typeIPropertyStore == null) return;

                var openMethod = typeIMMDevice.GetMethod("OpenPropertyStore");
                if (openMethod == null) return;

                // StorageAccessMode.ReadWrite = 2
                object[] args = new object[] { 2, null };
                int hr = (int)openMethod.Invoke(rawDeviceInterface, args);
                if (hr != 0) return;

                var propertyStore = args[1];
                if (propertyStore == null) return;

                var getValueMethod = typeIPropertyStore.GetMethod("GetValue");
                var setValueMethod = typeIPropertyStore.GetMethod("SetValue");
                var commitMethod = typeIPropertyStore.GetMethod("Commit");
                if (getValueMethod == null || setValueMethod == null || commitMethod == null) return;

                var key = new NAudio.CoreAudioApi.PropertyKey(new Guid("24b41950-0339-11d3-9b4f-00c04f8ef95e"), 1);

                object[] getArgs = new object[] { key, null };
                hr = (int)getValueMethod.Invoke(propertyStore, getArgs);
                if (hr == 0 && getArgs[1] is NAudio.CoreAudioApi.Interfaces.PropVariant val)
                {
                    if (val.DataType == 0) // VT_EMPTY
                    {
                        return;
                    }
                }

                var emptyProp = new NAudio.CoreAudioApi.Interfaces.PropVariant();
                object[] setArgs = new object[] { key, emptyProp };
                hr = (int)setValueMethod.Invoke(propertyStore, setArgs);
                if (hr == 0)
                {
                    commitMethod.Invoke(propertyStore, null);
                }
            }
            catch { }
        }

        private async void StartGlobalEq()
        {
            StopGlobalEq(false);
            if (!IsEqualizerEnabled) return;

            try
            {
                var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var (inputCable, outputCable) = FindCableDevices();

                if (inputCable == null)
                {
                    bool installed = await CheckAndInstallVirtualCableAsync();
                    if (!installed)
                    {
                        ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Equalizer disabled: VB-Cable required.", "ZH" => "均衡器已禁用：需要安装 VB-Cable。", _ => "Эквалайзер отключен: нужен VB-Cable." };
                        IsEqualizerEnabled = false;
                        return;
                    }

                    (inputCable, outputCable) = FindCableDevices();
                    if (inputCable == null)
                    {
                        ShredStatus = LocalizationService.Instance.ToolsErrorCableNotFound;
                        IsEqualizerEnabled = false;
                        return;
                    }
                }

                if (outputCable != null)
                {
                    DisableListenToDevice(outputCable);
                }

                NAudio.CoreAudioApi.MMDevice? realPlaybackDevice = null;
                if (SelectedAudioDevice != null)
                {
                    try
                    {
                        realPlaybackDevice = enumerator.GetDevice(SelectedAudioDevice.Id);
                    }
                    catch { }
                }

                if (realPlaybackDevice == null)
                {
                    try
                    {
                        realPlaybackDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                    }
                    catch { }

                    if (realPlaybackDevice == null || realPlaybackDevice.FriendlyName.Contains("CABLE Input"))
                    {
                        var renders = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                        foreach (var r in renders)
                        {
                            if (!r.FriendlyName.Contains("CABLE Input"))
                            {
                                realPlaybackDevice = r;
                                break;
                            }
                        }
                    }
                }

                if (realPlaybackDevice != null)
                {
                    if (string.IsNullOrEmpty(_originalDefaultDeviceId))
                    {
                        try
                        {
                            var sysDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                            if (sysDefault != null && !sysDefault.FriendlyName.Contains("CABLE Input", StringComparison.OrdinalIgnoreCase))
                            {
                                _originalDefaultDeviceId = sysDefault.ID;
                            }
                            else
                            {
                                var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                                foreach (var ep in endpoints)
                                {
                                    if (!ep.FriendlyName.Contains("CABLE Input", StringComparison.OrdinalIgnoreCase))
                                    {
                                        _originalDefaultDeviceId = ep.ID;
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    if (string.IsNullOrEmpty(_originalDefaultDeviceId))
                    {
                        _originalDefaultDeviceId = realPlaybackDevice.ID;
                    }
                    
                    _globalCapture = new WasapiLoopbackCapture(inputCable);
                    _globalBufferedProvider = new BufferedWaveProvider(_globalCapture.WaveFormat) { DiscardOnBufferOverflow = true };
                    
                    _globalCapture.DataAvailable += (s, e) =>
                    {
                        _globalBufferedProvider?.AddSamples(e.Buffer, 0, e.BytesRecorded);
                    };

                    float[] frequencies = { 60f, 170f, 310f, 600f, 1000f, 3000f, 6000f, 12000f, 14000f, 16000f };
                    float[] gains = { 
                        (float)Eq60Hz, (float)Eq170Hz, (float)Eq310Hz, (float)Eq600Hz, 
                        (float)Eq1kHz, (float)Eq3kHz, (float)Eq6kHz, (float)Eq12kHz, 
                        (float)Eq14kHz, (float)Eq16kHz 
                    };

                    _globalOutput = new WasapiOut(realPlaybackDevice, AudioClientShareMode.Shared, true, 100);
                    var targetFormat = _globalOutput.OutputWaveFormat;

                    var rawSampleProvider = _globalBufferedProvider.ToSampleProvider();
                    var matchedSampleProvider = MatchFormat(rawSampleProvider, targetFormat);

                    _globalEqProvider = new EqualizerSampleProvider(matchedSampleProvider, frequencies, gains);
                    _globalEqProvider.MasterGainDb = (float)MasterEqGain;
                    _globalEqProvider.IsEnabled = IsEqualizerEnabled;

                    _globalOutput.Init(_globalEqProvider);

                    AudioDeviceSwitcher.SetDefaultDevice(inputCable.ID);
                    StartWatchdog();

                    _globalCapture.StartRecording();
                    _globalOutput.Play();

                    ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "System equalizer is active", "ZH" => "系统均衡器已激活", _ => "Эквалайзер системы активен" };
                }
                else
                {
                    ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Error: No headphones found for output.", "ZH" => "错误：未找到输出耳机设备。", _ => "Ошибка: Не найдены наушники для вывода." };
                    IsEqualizerEnabled = false;
                }
            }
            catch (Exception ex)
            {
                ShredStatus = (LocalizationService.Instance.CurrentLanguage switch { "EN" => "EQ Error: ", "ZH" => "均衡器错误: ", _ => "Ошибка EQ: " }) + ex.Message;
                StopGlobalEq(false);
                IsEqualizerEnabled = false;
            }

            if (!IsEqualizerEnabled)
            {
                _ = Task.Delay(3000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
            }
        }

        private void StopGlobalEq(bool restoreDevice = true)
        {
            KillWatchdog();
            try
            {
                _globalCapture?.StopRecording();
                _globalCapture?.Dispose();
            }
            catch { }
            _globalCapture = null;

            try
            {
                _globalOutput?.Stop();
                _globalOutput?.Dispose();
            }
            catch { }
            _globalOutput = null;

            _globalBufferedProvider = null;
            _globalEqProvider = null;

            if (restoreDevice)
            {
                string? deviceToRestore = null;
                if (!string.IsNullOrEmpty(_originalDefaultDeviceId))
                {
                    deviceToRestore = _originalDefaultDeviceId;
                }
                else if (SelectedAudioDevice != null)
                {
                    deviceToRestore = SelectedAudioDevice.Id;
                }
                else
                {
                    try
                    {
                        var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                        var defaultDev = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                        if (defaultDev != null && !defaultDev.FriendlyName.Contains("CABLE Input"))
                        {
                            deviceToRestore = defaultDev.ID;
                        }
                        else
                        {
                            var renders = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                            foreach (var r in renders)
                            {
                                if (!r.FriendlyName.Contains("CABLE Input"))
                                {
                                    deviceToRestore = r.ID;
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(deviceToRestore))
                {
                    AudioDeviceSwitcher.SetDefaultDevice(deviceToRestore);
                }
            }
            _originalDefaultDeviceId = null;
            ShredStatus = "";
        }

        private void StartWatchdog()
        {
            KillWatchdog();
            if (string.IsNullOrEmpty(_originalDefaultDeviceId)) return;

            try
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    int currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = $"--watchdog {currentPid} \"{_originalDefaultDeviceId}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                    };
                    _watchdogProcess = System.Diagnostics.Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error starting watchdog: " + ex.Message);
            }
        }

        private void KillWatchdog()
        {
            try
            {
                if (_watchdogProcess != null && !_watchdogProcess.HasExited)
                {
                    _watchdogProcess.Kill();
                }
            }
            catch { }
            _watchdogProcess = null;
        }

        public void CleanupOnExit()
        {
            StopGlobalEq(true);
            SaveEqualizerSettings();
            SaveToDoList();
            SaveFocusTimerSettings();
            SaveCountdownTimerSettings();
        }

        partial void OnIsEqualizerEnabledChanged(bool value)
        {
            if (value)
            {
                StartGlobalEq();
            }
            else
            {
                StopGlobalEq(true);
            }
            SaveEqualizerSettings();
        }

        private void UpdateEqBand(int bandIndex, float gainDb)
        {
            _globalEqProvider?.UpdateBand(bandIndex, gainDb);
            if (!_isLoadingSettings)
            {
                if (SelectedEqualizerPreset != null && !SelectedEqualizerPreset.IsCustom)
                {
                    SelectedEqualizerPreset = null;
                }
            }
            SaveEqualizerSettings();
        }

        partial void OnMasterEqGainChanged(double value)
        {
            if (_globalEqProvider != null)
            {
                _globalEqProvider.MasterGainDb = (float)value;
            }
            if (!_isLoadingSettings)
            {
                if (SelectedEqualizerPreset != null && !SelectedEqualizerPreset.IsCustom)
                {
                    SelectedEqualizerPreset = null;
                }
            }
            SaveEqualizerSettings();
        }

        partial void OnBassBoostLevelChanged(double value)
        {
            if (!_isLoadingSettings)
            {
                if (SelectedEqualizerPreset != null && !SelectedEqualizerPreset.IsCustom)
                {
                    SelectedEqualizerPreset = null;
                }
            }
            SaveEqualizerSettings();
        }

        partial void OnEq60HzChanged(double value) => UpdateEqBand(0, (float)value);
        partial void OnEq170HzChanged(double value) => UpdateEqBand(1, (float)value);
        partial void OnEq310HzChanged(double value) => UpdateEqBand(2, (float)value);
        partial void OnEq600HzChanged(double value) => UpdateEqBand(3, (float)value);
        partial void OnEq1kHzChanged(double value) => UpdateEqBand(4, (float)value);
        partial void OnEq3kHzChanged(double value) => UpdateEqBand(5, (float)value);
        partial void OnEq6kHzChanged(double value) => UpdateEqBand(6, (float)value);
        partial void OnEq12kHzChanged(double value) => UpdateEqBand(7, (float)value);
        partial void OnEq14kHzChanged(double value) => UpdateEqBand(8, (float)value);
        partial void OnEq16kHzChanged(double value) => UpdateEqBand(9, (float)value);

        partial void OnSelectedEqualizerPresetChanged(EqualizerPreset? value)
        {
            OnPropertyChanged(nameof(IsCustomPresetSelected));
            if (value == null) return;

            if (value.IsCustom)
            {
                NewPresetName = value.Name;
            }
            else
            {
                NewPresetName = "";
            }

            _isLoadingSettings = true;
            try
            {
                MasterEqGain = value.MasterGain;
                BassBoostLevel = value.BassBoostLevel;
                Eq60Hz = value.Eq60Hz;
                Eq170Hz = value.Eq170Hz;
                Eq310Hz = value.Eq310Hz;
                Eq600Hz = value.Eq600Hz;
                Eq1kHz = value.Eq1kHz;
                Eq3kHz = value.Eq3kHz;
                Eq6kHz = value.Eq6kHz;
                Eq12kHz = value.Eq12kHz;
                Eq14kHz = value.Eq14kHz;
                Eq16kHz = value.Eq16kHz;

                if (IsEqualizerEnabled && _globalEqProvider != null)
                {
                    _globalEqProvider.MasterGainDb = (float)MasterEqGain;
                    _globalEqProvider.UpdateBand(0, (float)Eq60Hz);
                    _globalEqProvider.UpdateBand(1, (float)Eq170Hz);
                    _globalEqProvider.UpdateBand(2, (float)Eq310Hz);
                    _globalEqProvider.UpdateBand(3, (float)Eq600Hz);
                    _globalEqProvider.UpdateBand(4, (float)Eq1kHz);
                    _globalEqProvider.UpdateBand(5, (float)Eq3kHz);
                    _globalEqProvider.UpdateBand(6, (float)Eq6kHz);
                    _globalEqProvider.UpdateBand(7, (float)Eq12kHz);
                    _globalEqProvider.UpdateBand(8, (float)Eq14kHz);
                    _globalEqProvider.UpdateBand(9, (float)Eq16kHz);
                }
            }
            finally
            {
                _isLoadingSettings = false;
            }
            SaveEqualizerSettings();
        }

        private void InitializePresets()
        {
            EqualizerPresets.Clear();
            
            // Add Built-in presets
            EqualizerPresets.Add(new EqualizerPreset { Name = LocalizationService.Instance.ToolsPresetReset, IsCustom = false, MasterGain = 0, BassBoostLevel = 0 });
            EqualizerPresets.Add(new EqualizerPreset 
            { 
                Name = LocalizationService.Instance.ToolsPresetRock, IsCustom = false, MasterGain = 0, BassBoostLevel = 4,
                Eq60Hz = 4.5, Eq170Hz = 3.5, Eq310Hz = 2.0, Eq600Hz = -1.0, Eq1kHz = -2.5,
                Eq3kHz = 1.5, Eq6kHz = 3.0, Eq12kHz = 4.0, Eq14kHz = 4.0, Eq16kHz = 4.5 
            });
            EqualizerPresets.Add(new EqualizerPreset 
            { 
                Name = LocalizationService.Instance.ToolsPresetPop, IsCustom = false, MasterGain = 0, BassBoostLevel = 2,
                Eq60Hz = -1.5, Eq170Hz = -0.5, Eq310Hz = 1.0, Eq600Hz = 2.5, Eq1kHz = 3.0,
                Eq3kHz = 1.5, Eq6kHz = -0.5, Eq12kHz = -1.0, Eq14kHz = -1.5, Eq16kHz = -2.0 
            });
            EqualizerPresets.Add(new EqualizerPreset 
            { 
                Name = LocalizationService.Instance.ToolsPresetBass, IsCustom = false, MasterGain = 2.0, BassBoostLevel = 10,
                Eq60Hz = 8.0, Eq170Hz = 7.0, Eq310Hz = 5.0, Eq600Hz = 2.5, Eq1kHz = 0.5,
                Eq3kHz = 0.0, Eq6kHz = 0.0, Eq12kHz = 0.5, Eq14kHz = 1.0, Eq16kHz = 1.5 
            });

            // Load Custom Presets from file
            LoadCustomPresets();
        }

        private void LoadCustomPresets()
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                string configPath = Path.Combine(appData, "equalizer_presets.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var customList = JsonSerializer.Deserialize<List<EqualizerPreset>>(json);
                    if (customList != null)
                    {
                        foreach (var preset in customList)
                        {
                            preset.IsCustom = true;
                            EqualizerPresets.Add(preset);
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveCustomPresets()
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);
                string configPath = Path.Combine(appData, "equalizer_presets.json");
                var customList = EqualizerPresets.Where(p => p.IsCustom).ToList();
                string json = JsonSerializer.Serialize(customList);
                File.WriteAllText(configPath, json);
            }
            catch { }
        }

        [RelayCommand]
        public void UpdateEqualizerPreset()
        {
            if (SelectedEqualizerPreset == null || !SelectedEqualizerPreset.IsCustom) return;

            var preset = SelectedEqualizerPreset;
            preset.MasterGain = MasterEqGain;
            preset.BassBoostLevel = BassBoostLevel;
            preset.Eq60Hz = Eq60Hz;
            preset.Eq170Hz = Eq170Hz;
            preset.Eq310Hz = Eq310Hz;
            preset.Eq600Hz = Eq600Hz;
            preset.Eq1kHz = Eq1kHz;
            preset.Eq3kHz = Eq3kHz;
            preset.Eq6kHz = Eq6kHz;
            preset.Eq12kHz = Eq12kHz;
            preset.Eq14kHz = Eq14kHz;
            preset.Eq16kHz = Eq16kHz;

            SaveCustomPresets();
            
            ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => $"Preset '{preset.Name}' updated!", "ZH" => $"预设 '{preset.Name}' 已更新！", _ => $"Пресет '{preset.Name}' обновлен!" };
            Task.Delay(2000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
        }

        [RelayCommand]
        public void SaveCurrentAsPreset()
        {
            if (string.IsNullOrWhiteSpace(NewPresetName))
            {
                ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Enter preset name!", "ZH" => "请输入预设名称！", _ => "Введите имя пресета!" };
                Task.Delay(2000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
                return;
            }

            string name = NewPresetName.Trim();

            // If selected custom preset name matches the name entered, update it
            if (SelectedEqualizerPreset != null && SelectedEqualizerPreset.IsCustom && SelectedEqualizerPreset.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                UpdateEqualizerPreset();
                return;
            }

            // Check if another custom preset already exists with this name to overwrite it
            var existing = EqualizerPresets.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (existing.IsCustom)
                {
                    existing.MasterGain = MasterEqGain;
                    existing.BassBoostLevel = BassBoostLevel;
                    existing.Eq60Hz = Eq60Hz;
                    existing.Eq170Hz = Eq170Hz;
                    existing.Eq310Hz = Eq310Hz;
                    existing.Eq600Hz = Eq600Hz;
                    existing.Eq1kHz = Eq1kHz;
                    existing.Eq3kHz = Eq3kHz;
                    existing.Eq6kHz = Eq6kHz;
                    existing.Eq12kHz = Eq12kHz;
                    existing.Eq14kHz = Eq14kHz;
                    existing.Eq16kHz = Eq16kHz;

                    SaveCustomPresets();
                    SelectedEqualizerPreset = existing;
                    NewPresetName = "";
                    ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => $"Preset '{name}' updated!", "ZH" => $"预设 '{name}' 已更新！", _ => $"Пресет '{name}' обновлен!" };
                    Task.Delay(2000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
                    return;
                }
                else
                {
                    ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Cannot overwrite built-in preset!", "ZH" => "无法覆写内置预设！", _ => "Нельзя перезаписать встроенный пресет!" };
                    Task.Delay(2000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
                    return;
                }
            }

            var newPreset = new EqualizerPreset
            {
                Name = name,
                IsCustom = true,
                MasterGain = MasterEqGain,
                BassBoostLevel = BassBoostLevel,
                Eq60Hz = Eq60Hz,
                Eq170Hz = Eq170Hz,
                Eq310Hz = Eq310Hz,
                Eq600Hz = Eq600Hz,
                Eq1kHz = Eq1kHz,
                Eq3kHz = Eq3kHz,
                Eq6kHz = Eq6kHz,
                Eq12kHz = Eq12kHz,
                Eq14kHz = Eq14kHz,
                Eq16kHz = Eq16kHz
            };

            EqualizerPresets.Add(newPreset);
            SaveCustomPresets();
            SelectedEqualizerPreset = newPreset;
            NewPresetName = "";
            ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => $"Preset '{name}' saved!", "ZH" => $"预设 '{name}' 已保存！", _ => $"Пресет '{name}' сохранен!" };
            Task.Delay(2000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
        }

        [RelayCommand]
        public void DeleteEqualizerPreset()
        {
            if (SelectedEqualizerPreset == null || !SelectedEqualizerPreset.IsCustom) return;

            string name = SelectedEqualizerPreset.Name;
            EqualizerPresets.Remove(SelectedEqualizerPreset);
            SaveCustomPresets();
            SelectedEqualizerPreset = null;
            ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => $"Preset '{name}' deleted!", "ZH" => $"预设 '{name}' 已删除！", _ => $"Пресет '{name}' удален!" };
            Task.Delay(2000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
        }

        [RelayCommand]
        public async Task ImportEqualizerPreset()
        {
            var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (desktop?.MainWindow == null) return;

            var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Select preset file (.json)", "ZH" => "选择预设文件 (.json)", _ => "Выберите файл пресета (.json)" },
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType(LocalizationService.Instance.CurrentLanguage switch { "EN" => "Equalizer Presets", "ZH" => "均衡器预设", _ => "Пресеты эквалайзера" }) { Patterns = new[] { "*.json" } }
                }
            };

            try
            {
                var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(options);
                if (files != null && files.Count > 0)
                {
                    string path = files[0].Path.LocalPath;
                    string json = await File.ReadAllTextAsync(path);
                    
                    List<EqualizerPreset> importedPresets = new();
                    try
                    {
                        var single = JsonSerializer.Deserialize<EqualizerPreset>(json);
                        if (single != null && !string.IsNullOrEmpty(single.Name))
                        {
                            importedPresets.Add(single);
                        }
                    }
                    catch
                    {
                        var list = JsonSerializer.Deserialize<List<EqualizerPreset>>(json);
                        if (list != null)
                        {
                            importedPresets.AddRange(list.Where(p => !string.IsNullOrEmpty(p.Name)));
                        }
                    }

                    if (importedPresets.Count == 0)
                    {
                        ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Could not recognize presets in file!", "ZH" => "无法识别文件中的预设！", _ => "Не удалось распознать пресеты в файле!" };
                        _ = Task.Delay(2500).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
                        return;
                    }

                    int addedCount = 0;
                    foreach (var preset in importedPresets)
                    {
                        preset.IsCustom = true;
                        
                        string uniqueName = preset.Name;
                        int idx = 1;
                        while (EqualizerPresets.Any(p => p.Name.Equals(uniqueName, StringComparison.OrdinalIgnoreCase)))
                        {
                            uniqueName = $"{preset.Name} ({idx++})";
                        }
                        preset.Name = uniqueName;
                        
                        EqualizerPresets.Add(preset);
                        addedCount++;
                    }

                    if (addedCount > 0)
                    {
                        SaveCustomPresets();
                        if (addedCount == 1)
                        {
                            SelectedEqualizerPreset = EqualizerPresets.Last();
                            ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => $"Preset '{SelectedEqualizerPreset.Name}' imported!", "ZH" => $"预设 '{SelectedEqualizerPreset.Name}' 已导入！", _ => $"Пресет '{SelectedEqualizerPreset.Name}' импортирован!" };
                        }
                        else
                        {
                            ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => $"Imported presets: {addedCount}!", "ZH" => $"已导入预设: {addedCount}！", _ => $"Импортировано пресетов: {addedCount}!" };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShredStatus = (LocalizationService.Instance.CurrentLanguage switch { "EN" => "Import error: ", "ZH" => "导入错误: ", _ => "Ошибка импорта: " }) + ex.Message;
            }
            _ = Task.Delay(3000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
        }

        [RelayCommand]
        public async Task ExportEqualizerPreset()
        {
            if (SelectedEqualizerPreset == null)
            {
                ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Select preset to export!", "ZH" => "请选择要导出的预设！", _ => "Выберите пресет для экспорта!" };
                _ = Task.Delay(2000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
                return;
            }

            var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (desktop?.MainWindow == null) return;

            var options = new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Save preset as...", "ZH" => "预设另存为...", _ => "Сохранить пресет как..." },
                DefaultExtension = "json",
                SuggestedFileName = $"{SelectedEqualizerPreset.Name.Replace(" ", "_")}_preset.json",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType(LocalizationService.Instance.CurrentLanguage switch { "EN" => "Equalizer Presets", "ZH" => "均衡器预设", _ => "Пресеты эквалайзера" }) { Patterns = new[] { "*.json" } }
                }
            };

            try
            {
                var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(options);
                if (file != null)
                {
                    string path = file.Path.LocalPath;
                    var exportObj = new EqualizerPreset
                    {
                        Name = SelectedEqualizerPreset.Name,
                        IsCustom = true,
                        MasterGain = SelectedEqualizerPreset.MasterGain,
                        BassBoostLevel = SelectedEqualizerPreset.BassBoostLevel,
                        Eq60Hz = SelectedEqualizerPreset.Eq60Hz,
                        Eq170Hz = SelectedEqualizerPreset.Eq170Hz,
                        Eq310Hz = SelectedEqualizerPreset.Eq310Hz,
                        Eq600Hz = SelectedEqualizerPreset.Eq600Hz,
                        Eq1kHz = SelectedEqualizerPreset.Eq1kHz,
                        Eq3kHz = SelectedEqualizerPreset.Eq3kHz,
                        Eq6kHz = SelectedEqualizerPreset.Eq6kHz,
                        Eq12kHz = SelectedEqualizerPreset.Eq12kHz,
                        Eq14kHz = SelectedEqualizerPreset.Eq14kHz,
                        Eq16kHz = SelectedEqualizerPreset.Eq16kHz
                    };
                    string json = JsonSerializer.Serialize(exportObj, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(path, json);
                    ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Preset exported successfully!", "ZH" => "预设成功导出！", _ => "Пресет успешно экспортирован!" };
                }
            }
            catch (Exception ex)
            {
                ShredStatus = (LocalizationService.Instance.CurrentLanguage switch { "EN" => "Export error: ", "ZH" => "导出错误: ", _ => "Ошибка экспорта: " }) + ex.Message;
            }
            _ = Task.Delay(3000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
        }

        #endregion

        #region Image Converter

        [ObservableProperty]
        private string _imageConverterStatus = "";

        [RelayCommand]
        public async Task SelectImage()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Select image for conversion", "ZH" => "选择要转换的图片", _ => "Выберите изображение для конвертации" },
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType(LocalizationService.Instance.CurrentLanguage switch { "EN" => "Images", "ZH" => "图片", _ => "Изображения" })
                        {
                            Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp" }
                        }
                    }
                };

                var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(options);
                if (files.Count > 0)
                {
                    SelectedImagePath = files[0].Path.LocalPath;
                    ImageNameDisplay = Path.GetFileName(SelectedImagePath);
                }
            }
        }

        [RelayCommand]
        public void ClearImageSelection()
        {
            SelectedImagePath = "";
            ImageNameDisplay = LocalizationService.Instance.ToolsFileNotSelected;
            ImageConverterStatus = "";
        }

        [RelayCommand]
        public async Task ConvertImage(string targetFormat)
        {
            if (string.IsNullOrEmpty(SelectedImagePath) || !File.Exists(SelectedImagePath))
            {
                ImageConverterStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Select or drag an image first!", "ZH" => "请先选择或拖放一张图片！", _ => "Сначала выберите или перетащите изображение!" };
                await Task.Delay(3000);
                ImageConverterStatus = "";
                return;
            }

            ImageConverterStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Converting...", "ZH" => "正在转换...", _ => "Конвертирование..." };
            try
            {
                string dir = Path.GetDirectoryName(SelectedImagePath) ?? "";
                string nameWithoutExt = Path.GetFileNameWithoutExtension(SelectedImagePath);
                string newFileName = $"{nameWithoutExt}_converted.{targetFormat.ToLower()}";
                string targetPath = Path.Combine(dir, newFileName);

                await Task.Run(async () =>
                {
                    var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(SelectedImagePath);
                    using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        var decoder = await BitmapDecoder.CreateAsync(stream);
                        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                        Guid encoderId = targetFormat.ToLower() switch
                        {
                            "png" => BitmapEncoder.PngEncoderId,
                            "jpg" or "jpeg" => BitmapEncoder.JpegEncoderId,
                            "bmp" => BitmapEncoder.BmpEncoderId,
                            _ => BitmapEncoder.PngEncoderId
                        };

                        var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(dir);
                        var outputFile = await folder.CreateFileAsync(newFileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                        using (var outputStream = await outputFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                        {
                            var encoder = await BitmapEncoder.CreateAsync(encoderId, outputStream);
                            encoder.SetSoftwareBitmap(softwareBitmap);
                            await encoder.FlushAsync();
                        }
                    }
                });

                ImageConverterStatus = (LocalizationService.Instance.CurrentLanguage switch { "EN" => "Saved successfully: ", "ZH" => "保存成功: ", _ => "Успешно сохранено: " }) + newFileName;
            }
            catch (Exception ex)
            {
                ImageConverterStatus = (LocalizationService.Instance.CurrentLanguage switch { "EN" => "Conversion error: ", "ZH" => "转换错误: ", _ => "Ошибка конвертации: " }) + ex.Message;
            }

            await Task.Delay(4000);
            ImageConverterStatus = "";
        }

        #endregion

        #region Live Wallpaper Settings

        public async Task SetCustomWallpaper(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Wallpaper file not found!", "ZH" => "未找到壁纸文件！", _ => "Файл обоев не найден!" };
                return;
            }

            ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Installing live wallpaper...", "ZH" => "正在安装动态壁纸...", _ => "Установка живых обоев..." };
            await Task.Delay(500);

            try
            {
                await WallpaperService.ApplyCustomWallpaper(path);
                ActiveCustomWallpaperPath = path;
                SaveWallpaperSetting(path);
                ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Live wallpaper installed successfully!", "ZH" => "动态壁纸已成功安装！", _ => "Живые обои успешно установлены!" };
            }
            catch (Exception ex)
            {
                ShredStatus = (LocalizationService.Instance.CurrentLanguage switch { "EN" => "Error: ", "ZH" => "错误: ", _ => "Ошибка: " }) + ex.Message;
            }

            await Task.Delay(3000);
            ShredStatus = "";
        }

        [RelayCommand]
        public async Task SetBuiltInWallpaper(string type)
        {
            ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Installing built-in live wallpaper...", "ZH" => "正在安装内置动态壁纸...", _ => "Установка встроенных живых обоев..." };
            await Task.Delay(500);

            try
            {
                await WallpaperService.ApplyBuiltInWallpaper(type);
                ActiveCustomWallpaperPath = $"builtin:{type}";
                SaveWallpaperSetting($"builtin:{type}");
                ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Live wallpaper installed successfully!", "ZH" => "动态壁纸已成功安装！", _ => "Живые обои успешно установлены!" };
            }
            catch (Exception ex)
            {
                ShredStatus = (LocalizationService.Instance.CurrentLanguage switch { "EN" => "Error: ", "ZH" => "错误: ", _ => "Ошибка: " }) + ex.Message;
            }

            await Task.Delay(3000);
            ShredStatus = "";
        }

        [RelayCommand]
        public async Task ResetWallpaper()
        {
            ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Resetting desktop wallpaper...", "ZH" => "正在重置桌面壁纸...", _ => "Сброс обоев рабочего стола..." };
            await Task.Delay(500);
            try
            {
                WallpaperService.StopWallpaper();
                ActiveCustomWallpaperPath = "";
                SaveWallpaperSetting("");
                ShredStatus = LocalizationService.Instance.CurrentLanguage switch { "EN" => "Wallpaper reset to Windows default.", "ZH" => "壁纸已恢复为 Windows 默认壁纸。", _ => "Обои сброшены к стандартным Windows." };
            }
            catch (Exception ex)
            {
                ShredStatus = (LocalizationService.Instance.CurrentLanguage switch { "EN" => "Reset error: ", "ZH" => "重置错误: ", _ => "Ошибка сброса обоев: " }) + ex.Message;
            }
            await Task.Delay(3000);
            ShredStatus = "";
        }

        private void LoadToolsSettings()
        {
            _isLoadingSettings = true;
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);


                // Load Wallpaper setting
                string wallpaperConfigPath = Path.Combine(appData, "wallpaper.json");
                if (File.Exists(wallpaperConfigPath))
                {
                    string json = File.ReadAllText(wallpaperConfigPath);
                    var pathObj = JsonSerializer.Deserialize<string>(json);
                    if (!string.IsNullOrEmpty(pathObj))
                    {
                        if (pathObj.StartsWith("builtin:"))
                        {
                            string type = pathObj.Substring("builtin:".Length);
                            ActiveCustomWallpaperPath = pathObj;
                            Dispatcher.UIThread.Post(async () =>
                            {
                                await WallpaperService.ApplyBuiltInWallpaper(type);
                            });
                        }
                        else if (File.Exists(pathObj))
                        {
                            ActiveCustomWallpaperPath = pathObj;
                            Dispatcher.UIThread.Post(async () =>
                            {
                                await WallpaperService.ApplyCustomWallpaper(pathObj);
                            });
                        }
                    }
                }

                // Load Volume Limiter Settings
                string limiterPath = Path.Combine(appData, "volume_limiter.json");
                if (File.Exists(limiterPath))
                {
                    string json = File.ReadAllText(limiterPath);
                    var settings = JsonSerializer.Deserialize<VolumeLimiterSettings>(json);
                    if (settings != null)
                    {
                        IsVolumeLimiterEnabled = settings.IsEnabled;
                        MaxVolumeLimit = settings.MaxVolume;
                    }
                }

                // Load Equalizer Settings
                string eqPath = Path.Combine(appData, "equalizer.json");
                if (File.Exists(eqPath))
                {
                    try
                    {
                        string json = File.ReadAllText(eqPath);
                        var settings = JsonSerializer.Deserialize<EqualizerSettings>(json);
                        if (settings != null)
                        {
                            MasterEqGain = settings.MasterGain;
                            BassBoostLevel = settings.BassBoostLevel;
                            Eq60Hz = settings.Eq60Hz;
                            Eq170Hz = settings.Eq170Hz;
                            Eq310Hz = settings.Eq310Hz;
                            Eq600Hz = settings.Eq600Hz;
                            Eq1kHz = settings.Eq1kHz;
                            Eq3kHz = settings.Eq3kHz;
                            Eq6kHz = settings.Eq6kHz;
                            Eq12kHz = settings.Eq12kHz;
                            Eq14kHz = settings.Eq14kHz;
                            Eq16kHz = settings.Eq16kHz;

                            if (!string.IsNullOrEmpty(settings.SelectedAudioDeviceId))
                            {
                                var match = AudioDevices.FirstOrDefault(d => d.Id == settings.SelectedAudioDeviceId);
                                if (match != null)
                                {
                                    SelectedAudioDevice = match;
                                }
                            }

                            if (!string.IsNullOrEmpty(settings.SelectedPresetName))
                            {
                                var matchPreset = EqualizerPresets.FirstOrDefault(p => p.Name == settings.SelectedPresetName);
                                if (matchPreset != null)
                                {
                                    SelectedEqualizerPreset = matchPreset;
                                }
                            }

                            IsEqualizerEnabled = settings.IsEnabled;
                        }
                    }
                    catch { }
                }

                // Load To-Do List
                string todoPath = Path.Combine(appData, "todo_list.json");
                if (File.Exists(todoPath))
                {
                    try
                    {
                        string json = File.ReadAllText(todoPath);
                        var items = JsonSerializer.Deserialize<List<ToDoItem>>(json);
                        if (items != null)
                        {
                            ToDoItems.Clear();
                            foreach (var item in items)
                            {
                                item.PropertyChanged += ToDoItem_PropertyChanged;
                                ToDoItems.Add(item);
                            }
                        }
                    }
                    catch { }
                }

                // Load Focus Timer Settings
                string focusPath = Path.Combine(appData, "focus_timer.json");
                if (File.Exists(focusPath))
                {
                    try
                    {
                        string json = File.ReadAllText(focusPath);
                        var settings = JsonSerializer.Deserialize<FocusTimerSettings>(json);
                        if (settings != null)
                        {
                            WorkSessionMinutes = settings.WorkSessionMinutes;
                            BreakSessionMinutes = settings.BreakSessionMinutes;
                        }
                    }
                    catch { }
                }

                // Load Countdown Timer Settings
                string countdownPath = Path.Combine(appData, "countdown_timer.json");
                if (File.Exists(countdownPath))
                {
                    try
                    {
                        string json = File.ReadAllText(countdownPath);
                        var settings = JsonSerializer.Deserialize<CountdownTimerSettings>(json);
                        if (settings != null)
                        {
                            CountdownInputMinutes = settings.CountdownInputMinutes;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            finally
            {
                _isLoadingSettings = false;
            }
        }

        private void SaveWallpaperSetting(string path)
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);
                string wallpaperConfigPath = Path.Combine(appData, "wallpaper.json");
                string json = JsonSerializer.Serialize(path);
                File.WriteAllText(wallpaperConfigPath, json);
            }
            catch { }
        }

        private void VolumeLimiterTimer_Tick(object? sender, EventArgs e)
        {
            if (IsVolumeLimiterEnabled)
            {
                try
                {
                    float currentVol = VolumeService.GetVolume();
                    if (currentVol > MaxVolumeLimit)
                    {
                        VolumeService.SetVolume((float)MaxVolumeLimit);
                    }
                }
                catch { }
            }
        }

        private void SaveVolumeLimiterSettings()
        {
            if (_isLoadingSettings) return;
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);
                string limiterPath = Path.Combine(appData, "volume_limiter.json");
                string json = JsonSerializer.Serialize(new VolumeLimiterSettings
                {
                    IsEnabled = IsVolumeLimiterEnabled,
                    MaxVolume = MaxVolumeLimit
                });
                File.WriteAllText(limiterPath, json);
            }
            catch { }
        }

        partial void OnIsVolumeLimiterEnabledChanged(bool value) => SaveVolumeLimiterSettings();
        partial void OnMaxVolumeLimitChanged(double value) => SaveVolumeLimiterSettings();

        [RelayCommand]
        public void ApplyVolumeProfile(string profile)
        {
            switch (profile.ToLower())
            {
                case "games":
                    VolumeService.SetVolume(80f);
                    ShredStatus = LocalizationService.Instance.ToolsVolumeProfileGames;
                    break;
                case "movies":
                    VolumeService.SetVolume(60f);
                    ShredStatus = LocalizationService.Instance.ToolsVolumeProfileMovies;
                    break;
                case "work":
                    VolumeService.SetVolume(20f);
                    ShredStatus = LocalizationService.Instance.ToolsVolumeProfileWork;
                    break;
            }
            Task.Delay(2000).ContinueWith(_ => Dispatcher.UIThread.Post(() => ShredStatus = ""));
        }


        #endregion

        public class VolumeLimiterSettings
        {
            public bool IsEnabled { get; set; }
            public double MaxVolume { get; set; } = 70.0;
        }

        public void SaveEqualizerSettings()
        {
            if (_isLoadingSettings) return;
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);
                string configPath = Path.Combine(appData, "equalizer.json");
                string json = JsonSerializer.Serialize(new EqualizerSettings
                {
                    IsEnabled = IsEqualizerEnabled,
                    MasterGain = MasterEqGain,
                    SelectedAudioDeviceId = SelectedAudioDevice?.Id ?? "",
                    BassBoostLevel = BassBoostLevel,
                    Eq60Hz = Eq60Hz,
                    Eq170Hz = Eq170Hz,
                    Eq310Hz = Eq310Hz,
                    Eq600Hz = Eq600Hz,
                    Eq1kHz = Eq1kHz,
                    Eq3kHz = Eq3kHz,
                    Eq6kHz = Eq6kHz,
                    Eq12kHz = Eq12kHz,
                    Eq14kHz = Eq14kHz,
                    Eq16kHz = Eq16kHz,
                    SelectedPresetName = SelectedEqualizerPreset?.Name ?? ""
                });
                File.WriteAllText(configPath, json);
            }
            catch { }
        }

        public void SaveToDoList()
        {
            if (_isLoadingSettings) return;
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);
                string configPath = Path.Combine(appData, "todo_list.json");
                string json = JsonSerializer.Serialize(ToDoItems.ToList());
                File.WriteAllText(configPath, json);
            }
            catch { }
        }

        public void SaveFocusTimerSettings()
        {
            if (_isLoadingSettings) return;
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);
                string configPath = Path.Combine(appData, "focus_timer.json");
                string json = JsonSerializer.Serialize(new FocusTimerSettings
                {
                    WorkSessionMinutes = WorkSessionMinutes,
                    BreakSessionMinutes = BreakSessionMinutes
                });
                File.WriteAllText(configPath, json);
            }
            catch { }
        }

        public void SaveCountdownTimerSettings()
        {
            if (_isLoadingSettings) return;
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);
                string configPath = Path.Combine(appData, "countdown_timer.json");
                string json = JsonSerializer.Serialize(new CountdownTimerSettings
                {
                    CountdownInputMinutes = CountdownInputMinutes
                });
                File.WriteAllText(configPath, json);
            }
            catch { }
        }

        public class EqualizerSettings
        {
            public bool IsEnabled { get; set; }
            public double MasterGain { get; set; }
            public string SelectedAudioDeviceId { get; set; } = "";
            public double BassBoostLevel { get; set; }
            public double Eq60Hz { get; set; }
            public double Eq170Hz { get; set; }
            public double Eq310Hz { get; set; }
            public double Eq600Hz { get; set; }
            public double Eq1kHz { get; set; }
            public double Eq3kHz { get; set; }
            public double Eq6kHz { get; set; }
            public double Eq12kHz { get; set; }
            public double Eq14kHz { get; set; }
            public double Eq16kHz { get; set; }
            public string SelectedPresetName { get; set; } = "";
        }

        public class FocusTimerSettings
        {
            public double WorkSessionMinutes { get; set; } = 25;
            public double BreakSessionMinutes { get; set; } = 5;
        }

        public class CountdownTimerSettings
        {
            public double CountdownInputMinutes { get; set; } = 5;
        }
    }

    public class EqualizerPreset
    {
        public string Name { get; set; } = "";
        public bool IsCustom { get; set; }
        public double MasterGain { get; set; }
        public double BassBoostLevel { get; set; }
        public double Eq60Hz { get; set; }
        public double Eq170Hz { get; set; }
        public double Eq310Hz { get; set; }
        public double Eq600Hz { get; set; }
        public double Eq1kHz { get; set; }
        public double Eq3kHz { get; set; }
        public double Eq6kHz { get; set; }
        public double Eq12kHz { get; set; }
        public double Eq14kHz { get; set; }
        public double Eq16kHz { get; set; }
    }

}

