using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacStyleHub.Services;

namespace MacStyleHub.ViewModels
{
    public partial class MediaPlaybackViewModel : ViewModelBase
    {
        private readonly MediaPlaybackService _playbackService = new();
        private readonly DispatcherTimer _sessionRefreshTimer;

        [ObservableProperty]
        private string _title = "";

        [ObservableProperty]
        private string _artist = "";

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private bool _hasMedia;

        [ObservableProperty]
        private string _playerName = "";

        [ObservableProperty]
        private string _mediaText = "";

        [ObservableProperty]
        private ObservableCollection<MediaSessionInfo> _activeSessions = new();

        [ObservableProperty]
        private double _volume;

        [ObservableProperty]
        private bool _isMuted;

        public IAsyncRelayCommand TogglePlayCommand { get; }
        public IAsyncRelayCommand NextCommand { get; }
        public IAsyncRelayCommand PrevCommand { get; }

        public MediaPlaybackViewModel()
        {
            _playbackService.MediaChanged += PlaybackService_MediaChanged;
            _ = _playbackService.InitializeAsync();

            TogglePlayCommand = new AsyncRelayCommand(() => _playbackService.TogglePlayPauseAsync());
            NextCommand = new AsyncRelayCommand(() => _playbackService.SkipNextAsync());
            PrevCommand = new AsyncRelayCommand(() => _playbackService.SkipPreviousAsync());

            // Initialize volume from system
            try
            {
                Volume = VolumeService.GetVolume();
                IsMuted = VolumeService.GetMute();
            }
            catch
            {
                Volume = 50;
                IsMuted = false;
            }

            UpdateMediaText();
            LocalizationService.Instance.PropertyChanged += (sender, args) =>
            {
                UpdateMediaText();
            };

            // DispatcherTimer to periodically refresh the list of all active audio sources
            _sessionRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            _sessionRefreshTimer.Tick += (s, e) => RefreshActiveSessions();
            _sessionRefreshTimer.Start();
        }

        partial void OnVolumeChanged(double value)
        {
            try
            {
                VolumeService.SetVolume((float)value);
            }
            catch { }
        }

        partial void OnIsMutedChanged(bool value)
        {
            try
            {
                VolumeService.SetMute(value);
            }
            catch { }
        }

        [RelayCommand]
        public void ToggleMute()
        {
            IsMuted = !IsMuted;
        }

        private void UpdateMediaText()
        {
            if (HasMedia)
            {
                MediaText = string.IsNullOrEmpty(Artist) ? Title : $"{Title} — {Artist}";
            }
            else
            {
                MediaText = LocalizationService.Instance.CurrentLanguage switch
                {
                    "EN" => "Nothing playing",
                    "ZH" => "无媒体播放",
                    _ => "Ничего не играет"
                };
            }
        }

        private void PlaybackService_MediaChanged(string title, string artist, bool isPlaying, string player)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Title = title;
                Artist = artist;
                IsPlaying = isPlaying;
                PlayerName = player;
                HasMedia = !string.IsNullOrEmpty(title);
                
                UpdateMediaText();
            });
        }

        private async void RefreshActiveSessions()
        {
            var sessions = _playbackService.GetSessions();
            var newList = new ObservableCollection<MediaSessionInfo>();

            foreach (var session in sessions)
            {
                try
                {
                    var props = await session.TryGetMediaPropertiesAsync();
                    var info = session.GetPlaybackInfo();
                    
                    bool isPlaying = info != null && info.PlaybackStatus == Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                    string appId = session.SourceAppUserModelId;
                    
                    // Refine source name
                    string player = appId;
                    if (player.Contains("Spotify", StringComparison.OrdinalIgnoreCase)) player = "Spotify";
                    else if (player.Contains("Chrome", StringComparison.OrdinalIgnoreCase)) player = "Google Chrome";
                    else if (player.Contains("YandexMusic", StringComparison.OrdinalIgnoreCase)) player = "Яндекс.Музыка";
                    else if (player.Contains("VLC", StringComparison.OrdinalIgnoreCase)) player = "VLC Media Player";
                    else if (player.Contains("Telegram", StringComparison.OrdinalIgnoreCase)) player = "Telegram";
                    else if (player.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = player.LastIndexOf('\\');
                        if (idx >= 0) player = player.Substring(idx + 1);
                        if (player.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            player = player.Substring(0, player.Length - 4);
                    }

                    newList.Add(new MediaSessionInfo
                    {
                        AppId = appId,
                        Name = player,
                        Title = props.Title ?? "Unknown Track",
                        Artist = props.Artist ?? "Unknown Artist",
                        IsPlaying = isPlaying,
                        TogglePlayCommand = new AsyncRelayCommand(() => _playbackService.TogglePlayPauseSessionAsync(appId)),
                        NextCommand = new AsyncRelayCommand(() => _playbackService.SkipNextSessionAsync(appId)),
                        PrevCommand = new AsyncRelayCommand(() => _playbackService.SkipPreviousSessionAsync(appId))
                    });
                }
                catch { }
            }

            Dispatcher.UIThread.Post(() =>
            {
                ActiveSessions = newList;
            });
        }
    }

    public class MediaSessionInfo
    {
        public string AppId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public bool IsPlaying { get; set; }
        public IAsyncRelayCommand TogglePlayCommand { get; set; } = null!;
        public IAsyncRelayCommand NextCommand { get; set; } = null!;
        public IAsyncRelayCommand PrevCommand { get; set; } = null!;
    }
}
