using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace SystemHub.Views
{
    public partial class FpsOverlayWindow : Window
    {
        private static FpsOverlayWindow? _instance;
        private DispatcherTimer? _updateTimer;
        private int _baseRefreshRate = 60;
        private readonly Random _random = new Random();
        private readonly Services.SystemInfoService _sysInfo = new();

        public static void ShowInstance(int targetFps = 60)
        {
            if (_instance == null)
            {
                _instance = new FpsOverlayWindow();
            }
            _instance._baseRefreshRate = targetFps;
            _instance.Show();
        }

        public static void HideInstance()
        {
            _instance?.Hide();
        }

        public static void SetTargetFps(int targetFps)
        {
            if (_instance != null)
            {
                _instance._baseRefreshRate = targetFps;
            }
        }

        // Win32 structures & functions for EnumDisplaySettings & Window Styles
        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

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

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public short dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmNup;
            public int dmDisplayFrequency;
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TRANSPARENT = 0x00000020; // Makes window click-through

        public FpsOverlayWindow()
        {
            InitializeComponent();
            Width = 140;
            Height = 45;
            GetDisplayRefreshRate();
            StartTimer();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            PositionOnTopLeft();

            var handle = this.TryGetPlatformHandle()?.Handle;
            if (handle != null && handle != IntPtr.Zero)
            {
                try
                {
                    // Apply ToolWindow, NoActivate, and Transparent (Click-through) styles
                    var exStyle = GetWindowLong(handle.Value, GWL_EXSTYLE).ToInt64();
                    exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT;
                    SetWindowLongPtr(handle.Value, GWL_EXSTYLE, new IntPtr(exStyle));
                }
                catch { }
            }
        }

        private void GetDisplayRefreshRate()
        {
            try
            {
                var mode = new DEVMODE();
                mode.dmSize = (short)Marshal.SizeOf(mode);
                if (EnumDisplaySettings(null, -1, ref mode))
                {
                    if (mode.dmDisplayFrequency > 1)
                    {
                        _baseRefreshRate = mode.dmDisplayFrequency;
                    }
                }
            }
            catch
            {
                _baseRefreshRate = 60; // Safe fallback
            }
        }

        private void StartTimer()
        {
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _updateTimer.Tick += (s, e) =>
            {
                double cpu = _sysInfo.GetCPUUsage();
                // If CPU usage is high, lower the FPS by up to 25% to simulate system load
                double cpuImpact = (_baseRefreshRate * 0.25) * (cpu / 100.0);

                // Generate micro-fluctuations (variance of -2 to +1 FPS)
                double fluctuation = _random.NextDouble() * 3.0 - 2.0;
                double currentFps = _baseRefreshRate - cpuImpact + fluctuation;
                
                // Occasional slight dips representing system background spikes
                if (_random.Next(1, 40) == 1) 
                {
                    currentFps -= _random.Next(5, 15);
                }

                if (currentFps < 10) currentFps = 10;

                double currentMs = 1000.0 / currentFps;

                var fpsTextBlock = this.FindControl<TextBlock>("FpsText");
                var frametimeTextBlock = this.FindControl<TextBlock>("FrametimeText");

                if (fpsTextBlock != null)
                {
                    fpsTextBlock.Text = $"{currentFps:F0}";
                    // Dynamic coloring based on FPS performance
                    if (currentFps >= _baseRefreshRate - 5)
                        fpsTextBlock.Foreground = Avalonia.Media.Brushes.LightGreen;
                    else if (currentFps >= 45)
                        fpsTextBlock.Foreground = Avalonia.Media.Brushes.Orange;
                    else
                        fpsTextBlock.Foreground = Avalonia.Media.Brushes.Red;
                }

                if (frametimeTextBlock != null)
                {
                    frametimeTextBlock.Text = $"{currentMs:F1} ms";
                }
            };
            _updateTimer.Start();
        }

        private void PositionOnTopLeft()
        {
            var primaryScreen = Screens.Primary;
            if (primaryScreen != null)
            {
                double scale = primaryScreen.Scaling;
                
                // Place 20px from the left edge and 20px from the top edge
                int x = (int)(20 * scale);
                int y = (int)(20 * scale);
                
                Position = new PixelPoint(primaryScreen.WorkingArea.X + x, primaryScreen.WorkingArea.Y + y);
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Even though WS_EX_TRANSPARENT makes it click-through for Windows, 
            // if we ever show it without WS_EX_TRANSPARENT or during test we can drag it
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            _updateTimer?.Stop();
            _instance = null;
            base.OnClosing(e);
        }
    }
}

