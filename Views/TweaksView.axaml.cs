using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SystemHub.Services;
using SystemHub.ViewModels;

namespace SystemHub.Views
{
    public partial class TweaksView : UserControl
    {
        public TweaksView()
        {
            InitializeComponent();
        }

        private async void AddProtectedApp_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is TweaksViewModel vm)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = LocalizationService.Instance.ToolsOcrFileSelectorTitle,
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType(LocalizationService.Instance.ToolsOcrFileSelectorApps) { Patterns = new[] { "*.exe" } }
                    }
                });

                if (files != null && files.Any())
                {
                    var file = files.First();
                    string path = file.Path.LocalPath;
                    vm.AddProtectedApp(path);
                }
            }
        }
    }
}

