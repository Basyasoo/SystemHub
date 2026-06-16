using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SystemHub.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Drag the window when clicking and holding the TitleBar
            BeginMoveDrag(e);
        }

        private DynamicIslandWindow? _dynamicIsland;

        protected override void OnOpened(System.EventArgs e)
        {
            base.OnOpened(e);

            if (DataContext is ViewModels.MainWindowViewModel mainVm)
            {
                _dynamicIsland = new DynamicIslandWindow
                {
                    DataContext = mainVm
                };
                _dynamicIsland.Show();
            }
        }

        private bool _isExiting = false;

        public void ExitApp()
        {
            _isExiting = true;
            Close();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (!_isExiting)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                if (DataContext is ViewModels.MainWindowViewModel mainVm)
                {
                    mainVm.ToolsVM.CleanupOnExit();
                }
                _dynamicIsland?.Close();
                base.OnClosing(e);
            }
        }

        private void Close_OnClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Minimize_OnClick(object? sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_OnClick(object? sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }
}
