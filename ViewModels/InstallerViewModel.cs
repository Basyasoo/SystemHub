using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacStyleHub.Services;
using Avalonia.Threading;

namespace MacStyleHub.ViewModels
{
    public partial class ProgramInstallItemViewModel : ObservableObject
    {
        public string Id { get; set; } = "";

        [ObservableProperty]
        private string _name = "";

        [ObservableProperty]
        private string _category = "";

        public string WingetId { get; set; } = "";

        [ObservableProperty]
        private string _description = "";

        public string IconKey { get; set; } = "";

        [ObservableProperty]
        private InstallState _state;

        [ObservableProperty]
        private int _progress;

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private bool _isSelected;

        public bool IsNotInstalled => State == InstallState.NotInstalled || State == InstallState.Failed;
        public bool IsInstalling => State == InstallState.Installing || State == InstallState.Queued;
        public bool IsInstalled => State == InstallState.Installed;

        public string StateText => State switch
        {
            InstallState.Queued => LocalizationService.Instance.InstallerStatusQueued,
            InstallState.Installing => LocalizationService.Instance.InstallerStatusInstalling,
            InstallState.Installed => LocalizationService.Instance.InstallerStatusInstalled,
            InstallState.Failed => LocalizationService.Instance.InstallerStatusFailed,
            _ => LocalizationService.Instance.InstallerStatusNotInstalled
        };

        public string ActionText => IsInstalling ? StateText : LocalizationService.Instance.InstallerBtnInstall;

        public void Update(InstallState state, int progress, string message)
        {
            State = state;
            Progress = progress;
            StatusMessage = message;

            if (Id == "yandexmusicmod")
            {
                Name = LocalizationService.Instance.YandexMusicModName;
                Description = LocalizationService.Instance.YandexMusicModDesc;
                Category = LocalizationService.Instance.SidebarPlayer;
            }
            
            OnPropertyChanged(nameof(IsNotInstalled));
            OnPropertyChanged(nameof(IsInstalling));
            OnPropertyChanged(nameof(IsInstalled));
            OnPropertyChanged(nameof(StateText));
            OnPropertyChanged(nameof(ActionText));
        }
    }

    public partial class InstallerViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<ProgramInstallItemViewModel> _programs = new();

        public InstallerViewModel()
        {
            var serviceProgs = InstallerService.Instance.GetPrograms();
            foreach (var p in serviceProgs)
            {
                Programs.Add(new ProgramInstallItemViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.Category,
                    WingetId = p.WingetId,
                    Description = p.Description,
                    IconKey = p.IconKey,
                    State = p.State,
                    Progress = p.Progress,
                    StatusMessage = p.StatusMessage
                });
            }

            InstallerService.Instance.ProgramStateChanged += OnProgramStateChanged;
            
            LocalizationService.Instance.PropertyChanged += (sender, args) =>
            {
                foreach (var p in Programs)
                {
                    p.Update(p.State, p.Progress, p.StatusMessage);
                }
                OnPropertyChanged(nameof(InstallerHeader));
                OnPropertyChanged(nameof(InstallerDesc));
                OnPropertyChanged(nameof(InstallerBtnScan));
            };
        }

        public string InstallerHeader => LocalizationService.Instance.InstallerHeader;
        public string InstallerDesc => LocalizationService.Instance.InstallerDesc;
        public string InstallerBtnScan => LocalizationService.Instance.InstallerBtnScan;

        private void OnProgramStateChanged(string id, InstallState state, int progress, string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var prog = Programs.FirstOrDefault(p => p.Id == id);
                if (prog != null)
                {
                    prog.Update(state, progress, message);
                }
            });
        }

        [RelayCommand]
        public void RescanInstalled()
        {
            InstallerService.Instance.ScanInstalledApps();
        }

        [RelayCommand]
        public void InstallProgram(string id)
        {
            InstallerService.Instance.InstallProgram(id);
        }

        [RelayCommand]
        public void InstallSelected()
        {
            foreach (var prog in Programs)
            {
                if (prog.IsSelected && prog.IsNotInstalled)
                {
                    InstallerService.Instance.InstallProgram(prog.Id);
                }
            }
        }
    }
}
