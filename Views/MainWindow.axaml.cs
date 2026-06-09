using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MacStyleHub.Views
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