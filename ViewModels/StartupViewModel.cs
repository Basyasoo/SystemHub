using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemHub.Services;

namespace SystemHub.ViewModels
{
    public partial class StartupViewModel : ViewModelBase
    {
        private readonly StartupService _startupService = new();

        [ObservableProperty]
        private bool _isAppAutostartEnabled;

        [ObservableProperty]
        private ObservableCollection<StartupItem> _startupItems = new();

        private bool _isInitializing = true;

        public StartupViewModel()
        {
            RefreshCommand = new RelayCommand(RefreshStartupItems);

            // Initial load
            _isInitializing = true;
            IsAppAutostartEnabled = _startupService.IsAutostartEnabled();
            _isInitializing = false;
            
            RefreshStartupItems();
        }

        public IRelayCommand RefreshCommand { get; }

        partial void OnIsAppAutostartEnabledChanged(bool value)
        {
            if (_isInitializing) return;
            _startupService.SetAutostart(value);
        }

        public void RefreshStartupItems()
        {
            foreach (var item in StartupItems)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }

            StartupItems.Clear();
            var items = _startupService.GetStartupItems();
            foreach (var item in items)
            {
                item.PropertyChanged += Item_PropertyChanged;
                StartupItems.Add(item);
            }
        }

        private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StartupItem.IsEnabled) && sender is StartupItem item)
            {
                _startupService.ToggleStartupItem(item.Name, item.Location, item.Command, item.IsEnabled);
            }
        }

        [RelayCommand]
        private void RemoveItem(StartupItem item)
        {
            if (item == null) return;
            _startupService.RemoveStartupItem(item.Name, item.Location);
            RefreshStartupItems();
        }
    }
}

