using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacStyleHub.Services;

namespace MacStyleHub.ViewModels
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
            StartupItems.Clear();
            var items = _startupService.GetStartupItems();
            foreach (var item in items)
            {
                StartupItems.Add(item);
            }
        }
    }
}
