using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace SystemHub.Views
{
    public partial class DockWindow : Window
    {
        private static DockWindow? _instance;

        public static void ShowInstance()
        {
            if (_instance == null)
            {
                _instance = new DockWindow();
            }
            _instance.Show();
        }

        public static void HideInstance()
        {
            _instance?.Hide();
        }

        // Win32 Imports
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

        private const int GWL_EXSTYLE = -20;
        private const int GWL_WNDPROC = -4;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        
        private const uint WM_NCHITTEST = 0x0084;
        private static readonly IntPtr HTTRANSPARENT = new IntPtr(-1);
        private static readonly IntPtr HTCLIENT = new IntPtr(1);

        private IntPtr _prevWndProc = IntPtr.Zero;
        private IntPtr _subclassedHandle = IntPtr.Zero;
        private WndProcDelegate? _wndProc;
        private bool _isSubclassed;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public DockWindow()
        {
            InitializeComponent();
            Width = 460;
            Height = 70;
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            CenterOnBottomScreen();

            // Apply WS_EX_TOOLWINDOW and WS_EX_NOACTIVATE
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
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_NCHITTEST)
            {
                try
                {
                    int x = (int)(lParam.ToInt64() & 0xFFFF);
                    int y = (int)((lParam.ToInt64() >> 16) & 0xFFFF);
                    var clientPos = this.PointToClient(new PixelPoint(x, y));

                    var border = this.FindControl<Border>("DockBorder");
                    if (border != null)
                    {
                        double currentWidth = border.Bounds.Width;
                        if (currentWidth <= 0) currentWidth = border.Width;
                        if (currentWidth <= 0) currentWidth = 400; // safe fallback

                        double currentHeight = border.Bounds.Height;
                        if (currentHeight <= 0) currentHeight = border.Height;
                        if (currentHeight <= 0) currentHeight = 52; // safe fallback

                        double left = (Width - currentWidth) / 2;
                        double right = left + currentWidth;
                        double bottom = Height;
                        double top = Height - currentHeight;

                        if (clientPos.X >= left && clientPos.X <= right && clientPos.Y >= top && clientPos.Y <= bottom)
                        {
                            return HTCLIENT; // Inside the dock: handle clicks normally
                        }
                    }
                }
                catch { }

                return HTTRANSPARENT; // Outside the dock capsule: click passes through!
            }

            return CallWindowProc(_prevWndProc, hWnd, msg, wParam, lParam);
        }

        private void CenterOnBottomScreen()
        {
            var primaryScreen = Screens.Primary;
            if (primaryScreen != null)
            {
                double scale = primaryScreen.Scaling;
                double screenWidth = primaryScreen.WorkingArea.Width / scale;
                double screenHeight = primaryScreen.WorkingArea.Height / scale;
                
                int x = (int)(((screenWidth - Width) / 2) * scale);
                int y = (int)((screenHeight - Height - 10) * scale); // 10px offset from the bottom of working area
                
                Position = new PixelPoint(primaryScreen.WorkingArea.X + x, primaryScreen.WorkingArea.Y + y);
            }
        }

        // Click Actions
        private void LaunchExplorer(object? sender, RoutedEventArgs e)
        {
            try { Process.Start("explorer.exe"); } catch { }
        }

        private void LaunchNotepad(object? sender, RoutedEventArgs e)
        {
            try { Process.Start("notepad.exe"); } catch { }
        }

        private void LaunchCalc(object? sender, RoutedEventArgs e)
        {
            try { Process.Start("calc.exe"); } catch { }
        }

        private void LaunchBrowser(object? sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("https://www.google.com") { UseShellExecute = true }); } catch { }
        }

        private void LaunchSpotify(object? sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("spotify:") { UseShellExecute = true }); } catch { }
        }

        private void LaunchYandex(object? sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("https://music.yandex.ru") { UseShellExecute = true }); } catch { }
        }

        private void LaunchApp(object? sender, RoutedEventArgs e)
        {
            try
            {
                var app = Application.Current;
                if (app?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow != null)
                {
                    desktop.MainWindow.Show();
                    desktop.MainWindow.Activate();
                    desktop.MainWindow.WindowState = WindowState.Normal;
                }
            }
            catch { }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            var handle = this.TryGetPlatformHandle()?.Handle;
            if (_isSubclassed && handle != null && handle != IntPtr.Zero && _prevWndProc != IntPtr.Zero)
            {
                try { SetWindowLongPtr(handle.Value, GWL_WNDPROC, _prevWndProc); } catch { }
            }
            _prevWndProc = IntPtr.Zero;
            _isSubclassed = false;
            _subclassedHandle = IntPtr.Zero;
            _instance = null;
            base.OnClosing(e);
        }
    }
}

