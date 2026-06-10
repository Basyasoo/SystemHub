using Avalonia.Controls;
using Avalonia.Input;
using MacStyleHub.ViewModels;

namespace MacStyleHub.Views
{
    public partial class MediaPlaybackView : UserControl
    {
        public MediaPlaybackView()
        {
            InitializeComponent();
        }

        private void OnSessionCardPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is MediaSessionInfo sessionInfo)
            {
                if (DataContext is MediaPlaybackViewModel vm)
                {
                    vm.SelectSession(sessionInfo.AppId);
                }
            }
        }
    }
}
