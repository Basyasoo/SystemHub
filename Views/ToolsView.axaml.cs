using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Media;
using SystemHub.Services;
using SystemHub.ViewModels;

namespace SystemHub.Views
{
    public partial class ToolsView : UserControl
    {
        public ToolsView()
        {
            InitializeComponent();
            var dragZone = this.Find<Border>("DragDropShredder");
            if (dragZone != null)
            {
                dragZone.AddHandler(DragDrop.DragOverEvent, DragOver);
                dragZone.AddHandler(DragDrop.DropEvent, Drop);
                dragZone.AddHandler(DragDrop.DragLeaveEvent, DragLeave);
            }

            var converterZone = this.Find<Border>("DragDropConverter");
            if (converterZone != null)
            {
                converterZone.AddHandler(DragDrop.DragOverEvent, DragOverImage);
                converterZone.AddHandler(DragDrop.DropEvent, DropImage);
                converterZone.AddHandler(DragDrop.DragLeaveEvent, DragLeave);
            }
        }

        private void DragLeave(object? sender, RoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.Parse("#0AFFFFFF"));
            }
        }

        private void DragOver(object? sender, DragEventArgs e)
        {
            if (e.DataTransfer.TryGetFiles() != null)
            {
                e.DragEffects = DragDropEffects.Copy;
                if (sender is Border border)
                {
                    border.Background = new SolidColorBrush(Color.Parse("#20007AFF"));
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private async void Drop(object? sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.Parse("#0AFFFFFF"));
            }
            var files = e.DataTransfer.TryGetFiles();
            if (files != null)
            {
                var paths = files.Select(f => f.Path.LocalPath).ToList();
                if (paths.Count > 0 && DataContext is ToolsViewModel vm)
                {
                    await vm.ShredFiles(paths);
                }
            }
            e.Handled = true;
        }

        private void DragOverImage(object? sender, DragEventArgs e)
        {
            var files = e.DataTransfer.TryGetFiles();
            if (files != null && files.Any())
            {
                e.DragEffects = DragDropEffects.Copy;
                if (sender is Border border)
                {
                    border.Background = new SolidColorBrush(Color.Parse("#20007AFF"));
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DropImage(object? sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.Parse("#0AFFFFFF"));
            }
            var files = e.DataTransfer.TryGetFiles();
            if (files != null && DataContext is ToolsViewModel vm)
            {
                var file = files.FirstOrDefault();
                if (file != null)
                {
                    vm.SelectedImagePath = file.Path.LocalPath;
                    vm.ImageNameDisplay = System.IO.Path.GetFileName(vm.SelectedImagePath);
                }
            }
            e.Handled = true;
        }



        private async void SelectWallpaper_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is ToolsViewModel vm)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = LocalizationService.Instance.ToolsWallpapersPickerTitle,
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType(LocalizationService.Instance.ToolsWallpapersPickerFilter) { Patterns = new[] { "*.mp4", "*.html", "*.htm" } }
                    }
                });
                if (files != null && files.Any())
                {
                    var file = files.First();
                    string path = file.Path.LocalPath;
                    await vm.SetCustomWallpaper(path);
                }
            }
        }
    }
}

