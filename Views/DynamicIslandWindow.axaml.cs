using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.IO;
using SystemHub.ViewModels;

namespace SystemHub.Views
{
    public partial class DynamicIslandWindow : Window
    {
        // Win32 Subclassing P/Invokes
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        private static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLong32(hWnd, nIndex);
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetClipboardSequenceNumber();

        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);

        private const int GWL_EXSTYLE = -20;
        private const int GWL_WNDPROC = -4;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        
        private const uint WM_NCHITTEST = 0x0084;
        private static readonly IntPtr HTTRANSPARENT = new IntPtr(-1);
        private static readonly IntPtr HTCLIENT = new IntPtr(1);

        private const uint CF_BITMAP = 2;
        private const uint CF_DIB = 8;

        private IntPtr _prevWndProc = IntPtr.Zero;
        private IntPtr _subclassedHandle = IntPtr.Zero;
        private WndProcDelegate? _wndProc;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private MainWindowViewModel? _currentVm;
        private System.Threading.CancellationTokenSource? _hideDelayCts;
        private DateTime _keepVisibleUntil = DateTime.MinValue;
        private DispatcherTimer? _visualizerTimer;
        private bool _isSubclassed;
        private double _timePhase;
        private double _colorHue = 0.32; // Start with a nice green hue
        private bool _isHovered;
        private readonly Random _rand = new();

        // New state fields for Dynamic Island modules
        private bool _isCamActive;
        private bool _isMicActive;
        private bool _isOverheating;
        private bool _isFocusActive;
        private bool _isScreenshotActive;
        private double _vpnAngle = 0;
        private uint _lastClipboardSeq = 0;
        private System.Threading.CancellationTokenSource? _screenshotDismissCts;
        private Bitmap? _screenshotBitmap;
        private int _slowTickCounter = 0;

        // Settings cached properties
        private double CollapsedWidth => _currentVm?.TweaksVM.DynamicIslandWidth ?? 220;
        private double CollapsedHeight => 36;
        private double ExpandedWidth => 360;
        private double ExpandedHeight => 90;

        // Control cache
        private Border? _bar1;
        private Border? _bar2;
        private Border? _bar3;
        private Border? _bar4;
        private Border? _expBar1;
        private Border? _expBar2;
        private Border? _expBar3;
        private Border? _expBar4;
        private Border? _expBar5;
        private Border? _expBar6;

        public DynamicIslandWindow()
        {
            InitializeComponent();
            
            Width = 384;
            Height = 110;

            var border = this.FindControl<Border>("IslandBorder");
            if (border != null)
            {
                border.Width = CollapsedWidth;
                border.Height = CollapsedHeight;
                border.CornerRadius = new CornerRadius(18);
            }

            // Find visualizer bars
            _bar1 = this.FindControl<Border>("Bar1");
            _bar2 = this.FindControl<Border>("Bar2");
            _bar3 = this.FindControl<Border>("Bar3");
            _bar4 = this.FindControl<Border>("Bar4");
            _expBar1 = this.FindControl<Border>("ExpBar1");
            _expBar2 = this.FindControl<Border>("ExpBar2");
            _expBar3 = this.FindControl<Border>("ExpBar3");
            _expBar4 = this.FindControl<Border>("ExpBar4");
            _expBar5 = this.FindControl<Border>("ExpBar5");
            _expBar6 = this.FindControl<Border>("ExpBar6");

            // Setup visualizer animation timer (40ms = 25 FPS)
            _visualizerTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(40)
            };
            _visualizerTimer.Tick += VisualizerTimer_Tick;
            _visualizerTimer.Start();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            CenterOnScreen();
            
            // Subclass window procedure and apply EX styles for focusless overlays
            var handle = this.TryGetPlatformHandle()?.Handle;
            if (handle != null && handle != IntPtr.Zero)
            {
                if (_isSubclassed && _subclassedHandle != handle.Value)
                {
                    // Handle changed: reset subclassing state
                    _prevWndProc = IntPtr.Zero;
                    _isSubclassed = false;
                    _subclassedHandle = IntPtr.Zero;
                }

                if (!_isSubclassed)
                {
                    try
                    {
                        // Apply WS_EX_TOOLWINDOW and WS_EX_NOACTIVATE
                        var exStyle = GetWindowLong(handle.Value, GWL_EXSTYLE).ToInt64();
                        exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                        SetWindowLongPtr(handle.Value, GWL_EXSTYLE, new IntPtr(exStyle));

                        // Subclass WndProc safely: retrieve previous window procedure BEFORE subclassing
                        // to prevent reentrancy during SetWindowLongPtr from using an unassigned _prevWndProc.
                        _wndProc = new WndProcDelegate(WndProc);
                        _prevWndProc = GetWindowLong(handle.Value, GWL_WNDPROC);
                        SetWindowLongPtr(handle.Value, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProc));
                        _isSubclassed = true;
                        _subclassedHandle = handle.Value;
                    }
                    catch { }
                }
            }

            var mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid != null)
            {
                mainGrid.Margin = new Thickness(0, 5, 0, 0);
            }

            if (DataContext is MainWindowViewModel vm)
            {
                _currentVm = vm;
                _currentVm.PlaybackVM.PropertyChanged += OnMediaPlaybackViewModelPropertyChanged;
                _currentVm.TweaksVM.PropertyChanged += OnTweaksViewModelPropertyChanged;
                UpdateVisibility();
            }
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_NCHITTEST)
            {
                try
                {
                    // Get mouse position from lParam (in screen coordinates)
                    int x = (int)(lParam.ToInt64() & 0xFFFF);
                    int y = (int)((lParam.ToInt64() >> 16) & 0xFFFF);

                    // Convert screen coordinates to client coordinates
                    var clientPos = this.PointToClient(new PixelPoint(x, y));

                    // Check if the coordinate is inside the active IslandBorder bounds
                    var border = this.FindControl<Border>("IslandBorder");
                    if (border != null)
                    {
                        double currentWidth = border.Width;
                        double currentHeight = border.Height;

                        // Center alignment within window width
                        double left = (Width - currentWidth) / 2;
                        double right = left + currentWidth;
                        // Margin top offset is 5px since we set Margin="0,5,0,0"
                        double top = 5; 
                        double bottom = top + currentHeight;

                        if (clientPos.X >= left && clientPos.X <= right && clientPos.Y >= top && clientPos.Y <= bottom)
                        {
                            return HTCLIENT; // Inside the pill: handle hover & clicks normally
                        }
                    }
                }
                catch { }

                return HTTRANSPARENT; // Outside the pill: click passes through completely!
            }

            return CallWindowProc(_prevWndProc, hWnd, msg, wParam, lParam);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (_currentVm != null)
            {
                _currentVm.PlaybackVM.PropertyChanged -= OnMediaPlaybackViewModelPropertyChanged;
                _currentVm.TweaksVM.PropertyChanged -= OnTweaksViewModelPropertyChanged;
            }

            if (DataContext is MainWindowViewModel vm)
            {
                _currentVm = vm;
                _currentVm.PlaybackVM.PropertyChanged += OnMediaPlaybackViewModelPropertyChanged;
                _currentVm.TweaksVM.PropertyChanged += OnTweaksViewModelPropertyChanged;
                UpdateVisibility();
            }
            else
            {
                _currentVm = null;
            }
        }

        private void OnMediaPlaybackViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is MediaPlaybackViewModel vm)
            {
                if (e.PropertyName == nameof(MediaPlaybackViewModel.Title) || 
                    e.PropertyName == nameof(MediaPlaybackViewModel.Artist) ||
                    e.PropertyName == nameof(MediaPlaybackViewModel.IsPlaying) ||
                    e.PropertyName == nameof(MediaPlaybackViewModel.HasMedia))
                {
                    // If track changed or music paused/stopped, keep visible for 5 seconds
                    if (e.PropertyName == nameof(MediaPlaybackViewModel.Title) ||
                        e.PropertyName == nameof(MediaPlaybackViewModel.Artist) ||
                        !(vm.HasMedia && vm.IsPlaying))
                    {
                        _keepVisibleUntil = DateTime.UtcNow.AddSeconds(5);
                        ResetHideTimer();
                    }

                    Dispatcher.UIThread.Post(() => UpdateVisibility());
                }
            }
        }

        private void OnTweaksViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TweaksViewModel.IsDynamicIslandEnabled))
            {
                Dispatcher.UIThread.Post(() => UpdateVisibility());
            }
            else if (e.PropertyName == nameof(TweaksViewModel.DynamicIslandWidth))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var border = this.FindControl<Border>("IslandBorder");
                    if (border != null && !_isHovered && !_isScreenshotActive)
                    {
                        border.Width = CollapsedWidth;
                    }
                });
            }
            else if (e.PropertyName == nameof(TweaksViewModel.DynamicIslandTopMargin) ||
                     e.PropertyName == nameof(TweaksViewModel.SelectedScreenIndex))
            {
                Dispatcher.UIThread.Post(() => CenterOnScreen());
            }
        }

        private void ResetHideTimer()
        {
            _hideDelayCts?.Cancel();
            _hideDelayCts = new System.Threading.CancellationTokenSource();
            var token = _hideDelayCts.Token;

            Task.Run(async () =>
            {
                await Task.Delay(5000);
                if (token.IsCancellationRequested) return;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (token.IsCancellationRequested) return;
                    UpdateVisibility();
                });
            }, token);
        }

        private void UpdateVisibility()
        {
            if (_currentVm == null) return;

            bool isEnabled = _currentVm.TweaksVM.IsDynamicIslandEnabled;
            if (!isEnabled)
            {
                if (IsVisible) Hide();
                return;
            }

            bool hasMusic = _currentVm.PlaybackVM.HasMedia && _currentVm.PlaybackVM.IsPlaying && _currentVm.TweaksVM.DynamicIslandEnableMusic;

            bool shouldBeVisible = hasMusic 
                                   || _isHovered 
                                   || DateTime.UtcNow < _keepVisibleUntil 
                                   || _isOverheating 
                                   || _isFocusActive 
                                   || _isScreenshotActive;

            if (shouldBeVisible)
            {
                if (!IsVisible)
                {
                    Show();
                    CenterOnScreen();
                }
            }
            else
            {
                if (IsVisible)
                {
                    Hide();
                }
            }
        }

        private void CenterOnScreen()
        {
            var screens = Screens;
            if (screens == null) return;

            var allScreens = screens.All;
            if (allScreens == null || allScreens.Count == 0) return;

            int selectedIdx = _currentVm?.TweaksVM.SelectedScreenIndex ?? 0;
            if (selectedIdx < 0 || selectedIdx >= allScreens.Count)
            {
                selectedIdx = 0;
            }

            var targetScreen = allScreens[selectedIdx];
            if (targetScreen != null)
            {
                double scale = targetScreen.Scaling;
                double screenWidth = targetScreen.WorkingArea.Width / scale;
                int x = (int)(((screenWidth - Width) / 2) * scale);
                double margin = _currentVm?.TweaksVM.DynamicIslandTopMargin ?? 10;
                int y = (int)(margin * scale);
                
                Position = new PixelPoint(targetScreen.WorkingArea.X + x, targetScreen.WorkingArea.Y + y);
            }
        }

        private void OnPointerEntered(object? sender, PointerEventArgs e)
        {
            _isHovered = true;
            UpdateVisibility();

            if (_isScreenshotActive) return;

            var border = this.FindControl<Border>("IslandBorder");
            var collapsed = this.FindControl<Grid>("CollapsedContent");
            var expanded = this.FindControl<Grid>("ExpandedContent");

            if (border != null)
            {
                border.Width = ExpandedWidth;
                border.Height = ExpandedHeight;
                border.CornerRadius = new CornerRadius(22);
            }

            if (collapsed != null) collapsed.IsVisible = false;
            if (expanded != null) expanded.IsVisible = true;
        }

        private void OnPointerExited(object? sender, PointerEventArgs e)
        {
            _isHovered = false;
            _keepVisibleUntil = DateTime.UtcNow.AddSeconds(5);
            ResetHideTimer();
            UpdateVisibility();

            if (_isScreenshotActive) return;

            var border = this.FindControl<Border>("IslandBorder");
            var collapsed = this.FindControl<Grid>("CollapsedContent");
            var expanded = this.FindControl<Grid>("ExpandedContent");

            if (border != null)
            {
                border.Width = CollapsedWidth;
                border.Height = CollapsedHeight;
                border.CornerRadius = new CornerRadius(18);
            }

            if (collapsed != null) collapsed.IsVisible = true;
            if (expanded != null) expanded.IsVisible = false;
        }

        private void VisualizerTimer_Tick(object? sender, EventArgs e)
        {
            _slowTickCounter++;

            // 1. Slow check for Camera/Mic, Overheat, Focus and VPN every 1 second (25 visualizer ticks)
            if (_slowTickCounter % 25 == 0)
            {
                QueryActiveModules();
            }

            // 2. Clipboard check for screenshot every 500ms (12 visualizer ticks)
            if (_slowTickCounter % 12 == 0)
            {
                QueryClipboardForScreenshot();
            }

            UpdateTitleText();
            AnimateVpnIcon();
            AnimateOverheatAlert();

            if (_currentVm != null)
            {
                bool isPlaying = _currentVm.PlaybackVM.IsPlaying && _currentVm.PlaybackVM.HasMedia && _currentVm.TweaksVM.DynamicIslandEnableMusic;
                
                if (isPlaying)
                {
                    _timePhase += 0.22;
                    
                    // Cycle color hues slowly (rainbow cycle flow)
                    _colorHue += 0.005;
                    if (_colorHue > 1.0) _colorHue -= 1.0;
                    
                    var color = ColorFromAhsl(_colorHue, 0.85, 0.55);
                    var brush = new SolidColorBrush(color);

                    // Update backgrounds dynamically for color flow
                    if (_bar1 != null) _bar1.Background = brush;
                    if (_bar2 != null) _bar2.Background = brush;
                    if (_bar3 != null) _bar3.Background = brush;
                    if (_bar4 != null) _bar4.Background = brush;
                    if (_expBar1 != null) _expBar1.Background = brush;
                    if (_expBar2 != null) _expBar2.Background = brush;
                    if (_expBar3 != null) _expBar3.Background = brush;
                    if (_expBar4 != null) _expBar4.Background = brush;
                    if (_expBar5 != null) _expBar5.Background = brush;
                    if (_expBar6 != null) _expBar6.Background = brush;

                    // Normalize peak from GlowRingScale (typically 1.0 to 1.28)
                    double peak = (_currentVm.PlaybackVM.GlowRingScale - 1.0) / 0.28;
                    if (peak < 0) peak = 0;
                    if (peak > 1) peak = 1;

                    // Idle bounce when active but quiet
                    if (peak < 0.08) peak = 0.2;

                    // Animate collapsed visualizer (Bar1 to Bar4) smoothly using sine wave phases
                    if (_bar1 != null) _bar1.Height = Math.Clamp(4 + 10 * peak * (0.5 + 0.5 * Math.Sin(_timePhase * 1.5 + 0.0)), 3, 14);
                    if (_bar2 != null) _bar2.Height = Math.Clamp(4 + 14 * peak * (0.5 + 0.5 * Math.Sin(_timePhase * 2.1 + 1.0)), 3, 18);
                    if (_bar3 != null) _bar3.Height = Math.Clamp(4 + 8 * peak * (0.5 + 0.5 * Math.Sin(_timePhase * 1.2 + 2.0)), 3, 12);
                    if (_bar4 != null) _bar4.Height = Math.Clamp(4 + 12 * peak * (0.5 + 0.5 * Math.Sin(_timePhase * 1.8 + 3.0)), 3, 16);

                    // Animate expanded visualizer (ExpBar1 to ExpBar6)
                    if (_expBar1 != null) _expBar1.Height = Math.Clamp(4 + 10 * peak * (0.5 + 0.5 * Math.Sin(_timePhase * 1.6 + 0.5)), 3, 14);
                    if (_expBar2 != null) _expBar2.Height = Math.Clamp(4 + 14 * peak * (0.5 + 0.5 * Math.Sin(_timePhase * 2.2 + 1.5)), 3, 18);
                    if (_expBar3 != null) _expBar3.Height = Math.Clamp(4 + 8 * peak * (0.5 + 0.5 * Math.Sin(_timePhase * 1.3 + 2.5)), 3, 12);
                    if (_expBar4 != null) _expBar4.Height = Math.Clamp(4 + 12 * peak * (0.5 + 0.5 * Math.Sin(_timePhase * 1.9 + 3.5)), 3, 16);
                    if (_expBar5 != null) _expBar5.Height = Math.Clamp(4 + 15 * peak * (0.5 + 0.5 * Math.Sin(_timePhase * 2.5 + 4.5)), 3, 20);
                    if (_expBar6 != null) _expBar6.Height = Math.Clamp(4 + 9 * peak * (0.5 + 0.5 * Math.Sin(_timePhase * 1.4 + 5.5)), 3, 13);
                }
                else
                {
                    // Decay heights smoothly to silent flat line
                    double decay = 0.85;
                    
                    if (_bar1 != null) _bar1.Height = Math.Max(3, _bar1.Height * decay);
                    if (_bar2 != null) _bar2.Height = Math.Max(3, _bar2.Height * decay);
                    if (_bar3 != null) _bar3.Height = Math.Max(3, _bar3.Height * decay);
                    if (_bar4 != null) _bar4.Height = Math.Max(3, _bar4.Height * decay);

                    if (_expBar1 != null) _expBar1.Height = Math.Max(3, _expBar1.Height * decay);
                    if (_expBar2 != null) _expBar2.Height = Math.Max(3, _expBar2.Height * decay);
                    if (_expBar3 != null) _expBar3.Height = Math.Max(3, _expBar3.Height * decay);
                    if (_expBar4 != null) _expBar4.Height = Math.Max(3, _expBar4.Height * decay);
                    if (_expBar5 != null) _expBar5.Height = Math.Max(3, _expBar5.Height * decay);
                    if (_expBar6 != null) _expBar6.Height = Math.Max(3, _expBar6.Height * decay);
                }
            }
        }

        private void QueryActiveModules()
        {
            if (_currentVm == null) return;

            // Camera / Microphone Indicators
            bool enableCamMic = _currentVm.TweaksVM.DynamicIslandEnableCamMic;
            _isCamActive = enableCamMic && IsDeviceInUse("webcam");
            _isMicActive = enableCamMic && IsDeviceInUse("microphone");
            
            var micDot = this.FindControl<Ellipse>("MicIndicatorDot");
            var camDot = this.FindControl<Ellipse>("CamIndicatorDot");
            if (micDot != null) micDot.IsVisible = _isMicActive;
            if (camDot != null) camDot.IsVisible = _isCamActive;

            // VPN Active Status
            bool enableVpn = _currentVm.TweaksVM.DynamicIslandEnableVpn;
            bool isVpn = enableVpn && IsVpnActive();
            var vpnIcon = this.FindControl<TextBlock>("VpnIcon");
            if (vpnIcon != null)
            {
                vpnIcon.IsVisible = isVpn;
            }

            // System Overheat (temps > 90C)
            bool enableOverheat = _currentVm.TweaksVM.DynamicIslandEnableOverheat;
            double cpuTemp = _currentVm.SystemInfoVM.CpuTemperature;
            double gpuTemp = _currentVm.SystemInfoVM.GpuTemperature;
            double mbTemp = _currentVm.SystemInfoVM.MotherboardTemperature;
            _isOverheating = enableOverheat && (cpuTemp > 90.0 || gpuTemp > 90.0 || mbTemp > 90.0);

            // Focus Timer (Pomodoro countdown)
            bool enableFocus = _currentVm.TweaksVM.DynamicIslandEnableFocus;
            _isFocusActive = enableFocus && _currentVm.ToolsVM.IsFocusRunning;
            var focusIcon = this.FindControl<TextBlock>("FocusIcon");
            if (focusIcon != null)
            {
                focusIcon.IsVisible = _isFocusActive;
            }

            UpdateVisibility();
        }

        private void QueryClipboardForScreenshot()
        {
            if (_currentVm == null || !_currentVm.TweaksVM.DynamicIslandEnableScreenshot) return;

            uint currentSeq = GetClipboardSequenceNumber();
            if (_lastClipboardSeq == 0)
            {
                _lastClipboardSeq = currentSeq;
            }
            else if (currentSeq != _lastClipboardSeq)
            {
                _lastClipboardSeq = currentSeq;
                if (IsClipboardFormatAvailable(CF_BITMAP) || IsClipboardFormatAvailable(CF_DIB))
                {
                    Dispatcher.UIThread.Post(async () => await OnNewScreenshotDetected());
                }
            }
        }

        private async Task OnNewScreenshotDetected()
        {
            var clipboard = this.Clipboard;
            if (clipboard == null) return;

            try
            {
                var bitmap = await clipboard.TryGetBitmapAsync();
                if (bitmap != null)
                {
                    _screenshotBitmap = bitmap;

                    var thumbnail = this.FindControl<Image>("ScreenshotThumbnail");
                    if (thumbnail != null)
                    {
                        thumbnail.Source = bitmap;
                    }

                    _isScreenshotActive = true;

                    // Toggle visibility of panels
                    var collapsed = this.FindControl<Grid>("CollapsedContent");
                    var expanded = this.FindControl<Grid>("ExpandedContent");
                    var screenshotContent = this.FindControl<Grid>("ScreenshotContent");

                    if (collapsed != null) collapsed.IsVisible = false;
                    if (expanded != null) expanded.IsVisible = false;
                    if (screenshotContent != null) screenshotContent.IsVisible = true;

                    // Resize the border to fit screenshot thumbnail and buttons
                    var border = this.FindControl<Border>("IslandBorder");
                    if (border != null)
                    {
                        border.Width = 340;
                        border.Height = 80;
                        border.CornerRadius = new CornerRadius(16);
                    }

                    UpdateVisibility();

                    // Start auto-dismiss timer (10s)
                    _screenshotDismissCts?.Cancel();
                    _screenshotDismissCts = new System.Threading.CancellationTokenSource();
                    var token = _screenshotDismissCts.Token;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(10000);
                        if (token.IsCancellationRequested) return;
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (token.IsCancellationRequested) return;
                            DismissScreenshot();
                        });
                    }, token);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Screenshot fetch failed: " + ex.Message);
            }
        }

        private void DismissScreenshot()
        {
            _screenshotDismissCts?.Cancel();
            _isScreenshotActive = false;
            _screenshotBitmap = null;

            var collapsed = this.FindControl<Grid>("CollapsedContent");
            var expanded = this.FindControl<Grid>("ExpandedContent");
            var screenshotContent = this.FindControl<Grid>("ScreenshotContent");

            if (collapsed != null) collapsed.IsVisible = !_isHovered;
            if (expanded != null) expanded.IsVisible = _isHovered;
            if (screenshotContent != null) screenshotContent.IsVisible = false;

            var border = this.FindControl<Border>("IslandBorder");
            if (border != null)
            {
                double targetW = _isHovered ? ExpandedWidth : CollapsedWidth;
                double targetH = _isHovered ? ExpandedHeight : CollapsedHeight;
                border.Width = targetW;
                border.Height = targetH;
                border.CornerRadius = new CornerRadius(_isHovered ? 22 : 18);
            }

            UpdateVisibility();
        }

        private void CopyScreenshot_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            DismissScreenshot();
        }

        private void SaveScreenshot_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_screenshotBitmap != null)
            {
                try
                {
                    string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    string folderPath = System.IO.Path.Combine(picturesPath, "Screenshots");
                    Directory.CreateDirectory(folderPath);
                    string fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    string filePath = System.IO.Path.Combine(folderPath, fileName);

                    _screenshotBitmap.Save(filePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Save screenshot failed: " + ex.Message);
                }
            }
            DismissScreenshot();
        }

        private void DismissScreenshot_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            DismissScreenshot();
        }

        private void UpdateTitleText()
        {
            if (_currentVm == null) return;

            var islandTitleText = this.FindControl<TextBlock>("IslandTitleText");
            if (islandTitleText == null) return;

            if (_isScreenshotActive) return;

            bool hasMusic = _currentVm.PlaybackVM.HasMedia && _currentVm.PlaybackVM.IsPlaying && _currentVm.TweaksVM.DynamicIslandEnableMusic;

            if (hasMusic)
            {
                islandTitleText.Text = _currentVm.PlaybackVM.Title;
            }
            else if (_isFocusActive)
            {
                islandTitleText.Text = $"Фокус: {_currentVm.ToolsVM.FocusTimeDisplay}";
            }
            else if (_isOverheating)
            {
                islandTitleText.Text = "ПЕРЕГРЕВ СИСТЕМЫ!";
            }
            else
            {
                islandTitleText.Text = "SystemHub";
            }
        }

        private void AnimateVpnIcon()
        {
            var vpnIcon = this.FindControl<TextBlock>("VpnIcon");
            if (vpnIcon != null && vpnIcon.IsVisible)
            {
                _vpnAngle += 4.0;
                if (_vpnAngle >= 360) _vpnAngle = 0;
                var rotateTransform = vpnIcon.RenderTransform as RotateTransform;
                if (rotateTransform != null)
                {
                    rotateTransform.Angle = _vpnAngle;
                }
            }
        }

        private void AnimateOverheatAlert()
        {
            var border = this.FindControl<Border>("IslandBorder");
            var overheatIcon = this.FindControl<TextBlock>("OverheatIcon");
            if (overheatIcon != null) overheatIcon.IsVisible = _isOverheating;

            if (border != null)
            {
                if (_isOverheating)
                {
                    bool flashPhase = (DateTime.UtcNow.Millisecond / 250) % 2 == 0;
                    border.Background = flashPhase 
                        ? new SolidColorBrush(Color.Parse("#FF3B30")) 
                        : new SolidColorBrush(Color.Parse("#FC0E0E10"));
                }
                else
                {
                    border.Background = new SolidColorBrush(Color.Parse("#FC0E0E10"));
                }
            }
        }

        private bool IsDeviceInUse(string deviceName)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\{deviceName}"))
                {
                    if (key == null) return false;
                    
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        if (subKeyName == "NonPackaged")
                        {
                            using (var nonPackagedKey = key.OpenSubKey("NonPackaged"))
                            {
                                if (nonPackagedKey != null)
                                {
                                    foreach (var win32AppName in nonPackagedKey.GetSubKeyNames())
                                    {
                                        using (var appKey = nonPackagedKey.OpenSubKey(win32AppName))
                                        {
                                            if (appKey != null)
                                            {
                                                var stopTime = appKey.GetValue("LastUsedTimeStop");
                                                if (stopTime is long stopTimeLong && stopTimeLong == 0)
                                                {
                                                    return true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            using (var appKey = key.OpenSubKey(subKeyName))
                            {
                                if (appKey != null)
                                {
                                    var stopTime = appKey.GetValue("LastUsedTimeStop");
                                    if (stopTime is long stopTimeLong && stopTimeLong == 0)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private bool IsVpnActive()
        {
            try
            {
                var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in interfaces)
                {
                    if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                    {
                        string desc = ni.Description.ToLower();
                        string name = ni.Name.ToLower();
                        if (desc.Contains("vpn") || desc.Contains("tap") || desc.Contains("tun") || 
                            desc.Contains("wireguard") || desc.Contains("openvpn") || 
                            name.Contains("vpn") || name.Contains("tap") || name.Contains("tun") ||
                            name.Contains("wireguard") || name.Contains("openvpn"))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private Color ColorFromAhsl(double h, double s, double l)
        {
            double r = 0, g = 0, b = 0;
            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;
                r = HueToRgb(p, q, h + 1.0 / 3.0);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1.0 / 3.0);
            }
            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
            return p;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            _hideDelayCts?.Cancel();
            _screenshotDismissCts?.Cancel();
            _visualizerTimer?.Stop();
            _visualizerTimer = null;
  
            // Unhook subclassing if it exists
            var handle = this.TryGetPlatformHandle()?.Handle;
            if (_isSubclassed && handle != null && handle != IntPtr.Zero && _prevWndProc != IntPtr.Zero)
            {
                try
                {
                    SetWindowLongPtr(handle.Value, GWL_WNDPROC, _prevWndProc);
                }
                catch { }
            }
            _prevWndProc = IntPtr.Zero;
            _isSubclassed = false;
            _subclassedHandle = IntPtr.Zero;
  
            if (_currentVm != null)
            {
                _currentVm.PlaybackVM.PropertyChanged -= OnMediaPlaybackViewModelPropertyChanged;
                _currentVm.TweaksVM.PropertyChanged -= OnTweaksViewModelPropertyChanged;
            }
            base.OnClosing(e);
        }
    }
}

