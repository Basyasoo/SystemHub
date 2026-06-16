using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using SystemHub.ViewModels;

namespace SystemHub.Views
{
    public partial class WeatherWidgetWindow : Window
    {
        private static WeatherWidgetWindow? _instance;

        public static void ShowInstance()
        {
            if (_instance == null)
            {
                _instance = new WeatherWidgetWindow();
            }
            _instance.Show();
        }

        public static void HideInstance()
        {
            _instance?.Hide();
        }

        // Win32 Imports for desktop z-order management
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

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

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        public WeatherWidgetWindow()
        {
            InitializeComponent();
            DataContext = new WeatherViewModel();
            Width = 260;
            Height = 110;
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            PositionOnTopRight();

            var handle = this.TryGetPlatformHandle()?.Handle;
            if (handle != null && handle != IntPtr.Zero)
            {
                try
                {
                    // Apply WS_EX_TOOLWINDOW and WS_EX_NOACTIVATE so it doesn't show in Alt-Tab or taskbar
                    var exStyle = GetWindowLong(handle.Value, GWL_EXSTYLE).ToInt64();
                    exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                    SetWindowLongPtr(handle.Value, GWL_EXSTYLE, new IntPtr(exStyle));

                    // Place the widget behind normal windows (on the desktop wallpaper level)
                    SetWindowPos(handle.Value, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
                }
                catch { }
            }
        }

        private void PositionOnTopRight()
        {
            var primaryScreen = Screens.Primary;
            if (primaryScreen != null)
            {
                double scale = primaryScreen.Scaling;
                double screenWidth = primaryScreen.WorkingArea.Width / scale;
                
                // Position 40px from the right edge and 80px from the top edge
                int x = (int)((screenWidth - Width - 40) * scale);
                int y = (int)(80 * scale);
                
                Position = new PixelPoint(primaryScreen.WorkingArea.X + x, primaryScreen.WorkingArea.Y + y);
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            _instance = null;
            base.OnClosing(e);
        }
    }
}

