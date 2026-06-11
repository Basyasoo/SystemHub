using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacStyleHub.Services;

namespace MacStyleHub.ViewModels
{
    public partial class AppVolumeSessionViewModel : ObservableObject
    {
        public string SessionInstanceId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string ProcessName { get; set; } = "";
        public bool IsSystemSounds { get; set; }

        [ObservableProperty]
        private double _volume;

        [ObservableProperty]
        private bool _isMuted;

        private bool _isUpdatingInternally;

        public AppVolumeSessionViewModel()
        {
            ToggleMuteCommand = new RelayCommand(ToggleMute);
        }

        public IRelayCommand ToggleMuteCommand { get; }

        public string LocalizedDisplayName
        {
            get
            {
                if (IsSystemSounds || DisplayName == "System Sounds" || DisplayName == "Системные звуки" || DisplayName == "系统声音")
                {
                    return LocalizationService.Instance.CurrentLanguage switch
                    {
                        "EN" => "System Sounds",
                        "ZH" => "系统声音",
                        _ => "Системные звуки"
                    };
                }
                return DisplayName;
            }
        }

        public void NotifyLocalizationChanged()
        {
            OnPropertyChanged(nameof(LocalizedDisplayName));
        }

        private void ToggleMute()
        {
            IsMuted = !IsMuted;
        }

        partial void OnVolumeChanged(double value)
        {
            if (_isUpdatingInternally) return;
            VolumeService.SetAppSessionVolume(SessionInstanceId, (float)value);
        }

        partial void OnIsMutedChanged(bool value)
        {
            if (_isUpdatingInternally) return;
            VolumeService.SetAppSessionMute(SessionInstanceId, value);
        }

        public void UpdateFromModel(AppAudioSession model)
        {
            _isUpdatingInternally = true;
            Volume = model.Volume;
            IsMuted = model.IsMuted;
            _isUpdatingInternally = false;
        }
    }

    public partial class MediaPlaybackViewModel : ViewModelBase
    {
        private readonly MediaPlaybackService _playbackService = new();
        private readonly DispatcherTimer _sessionRefreshTimer;
        private readonly DispatcherTimer _visualizerTimer;
        private double _smoothPeak = 0.0;
        private double _maxRecentPeak = 0.05;
        private readonly Queue<double> _peakHistory = new();

        [ObservableProperty]
        private double _glowRingScale = 1.0;

        [ObservableProperty]
        private double _glowRingOpacity = 0.4;

        [ObservableProperty]
        private double _vinylDiskScale = 1.0;

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
        private ObservableCollection<AppVolumeSessionViewModel> _appVolumeSessions = new();

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

            _displayVolume = IsMuted ? 0 : Volume;

            UpdateMediaText();
            LocalizationService.Instance.PropertyChanged += (sender, args) =>
            {
                UpdateMediaText();
                foreach (var s in AppVolumeSessions)
                {
                    s.NotifyLocalizationChanged();
                }
            };

            // Initial refresh of volume sessions
            RefreshVolumeSessions();

            // DispatcherTimer to periodically refresh the list of all active audio sources
            _sessionRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.2)
            };
            _sessionRefreshTimer.Tick += (s, e) =>
            {
                RefreshActiveSessions();
                RefreshVolumeSessions();
            };
            _sessionRefreshTimer.Start();

            _visualizerTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(25)
            };
            _visualizerTimer.Tick += VisualizerTimer_Tick;
            _visualizerTimer.Start();
        }

        private AppVolumeSessionViewModel? GetActivePlayerSession()
        {
            if (!HasMedia || string.IsNullOrEmpty(PlayerName)) return null;

            string name = PlayerName.ToLower();

            // Explicit check for Yandex Music
            if (name.Contains("яндекс") || name.Contains("yandex"))
            {
                var yandexSession = AppVolumeSessions.FirstOrDefault(s => 
                    s.ProcessName.ToLower().Contains("yandex") || 
                    s.ProcessName.ToLower().Contains("яндекс")
                );
                if (yandexSession != null) return yandexSession;
            }

            return AppVolumeSessions.FirstOrDefault(s => 
                s.ProcessName.ToLower().Contains(name) || 
                s.DisplayName.ToLower().Contains(name) ||
                name.Contains(s.ProcessName.ToLower()) ||
                name.Contains(s.DisplayName.ToLower())
            );
        }

        private bool _isUpdatingVolumeFromSystem;

        private void UpdateVolumeFromSystem()
        {
            try
            {
                var activeSession = GetActivePlayerSession();
                if (activeSession != null)
                {
                    var activeVol = activeSession.Volume;
                    var activeMute = activeSession.IsMuted;
                    
                    if (Math.Abs(activeVol - Volume) > 1.0 || activeMute != IsMuted)
                    {
                        _isUpdatingVolumeFromSystem = true;
                        Volume = activeVol;
                        IsMuted = activeMute;
                        _isUpdatingVolumeFromSystem = false;
                    }
                }
                else
                {
                    var sysVol = VolumeService.GetVolume();
                    var sysMute = VolumeService.GetMute();
                    
                    if (Math.Abs(sysVol - Volume) > 1.0 || sysMute != IsMuted)
                    {
                        _isUpdatingVolumeFromSystem = true;
                        Volume = sysVol;
                        IsMuted = sysMute;
                        _isUpdatingVolumeFromSystem = false;
                    }
                }
            }
            catch {}
        }

        private bool _isMutingOrUnmuting;
        private double _displayVolume;
        private System.Threading.CancellationTokenSource? _animCts;

        public double DisplayVolume
        {
            get => _displayVolume;
            set
            {
                if (Math.Abs(_displayVolume - value) < 0.01) return;
                _displayVolume = value;
                OnPropertyChanged(nameof(DisplayVolume));

                if (!_isMutingOrUnmuting && !_isUpdatingVolumeFromSystem)
                {
                    if (_displayVolume > 0 && IsMuted)
                    {
                        IsMuted = false;
                    }
                    Volume = _displayVolume;
                }
            }
        }

        private void AnimateVolume(double from, double to)
        {
            _animCts?.Cancel();
            _animCts = new System.Threading.CancellationTokenSource();
            var token = _animCts.Token;

            _isMutingOrUnmuting = true;

            Task.Run(async () =>
            {
                int steps = 12;
                int duration = 180; // 180 ms
                int delay = duration / steps;

                for (int i = 1; i <= steps; i++)
                {
                    if (token.IsCancellationRequested) return;

                    double progress = (double)i / steps;
                    double t = 1 - Math.Pow(1 - progress, 3); // cubic ease out
                    double current = from + (to - from) * t;

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _displayVolume = current;
                        OnPropertyChanged(nameof(DisplayVolume));
                    });

                    await Task.Delay(delay);
                }

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _displayVolume = to;
                    OnPropertyChanged(nameof(DisplayVolume));
                    _isMutingOrUnmuting = false;
                });
            }, token);
        }

        partial void OnVolumeChanged(double value)
        {
            if (!_isMutingOrUnmuting)
            {
                _displayVolume = IsMuted ? 0 : value;
                OnPropertyChanged(nameof(DisplayVolume));
            }
            if (_isUpdatingVolumeFromSystem) return;
            try
            {
                var activeSession = GetActivePlayerSession();
                if (activeSession != null)
                {
                    activeSession.Volume = value;
                }
                else
                {
                    VolumeService.SetVolume((float)value);
                }
            }
            catch { }
        }

        partial void OnIsMutedChanged(bool value)
        {
            if (!_isUpdatingVolumeFromSystem)
            {
                try
                {
                    var activeSession = GetActivePlayerSession();
                    if (activeSession != null)
                    {
                        activeSession.IsMuted = value;
                    }
                    else
                    {
                        VolumeService.SetMute(value);
                    }
                }
                catch { }
            }

            if (value)
            {
                AnimateVolume(Volume, 0);
            }
            else
            {
                AnimateVolume(0, Volume);
            }
        }

        [ObservableProperty]
        private bool _isSessionsExpanded = true;

        [RelayCommand]
        public void ToggleSessions()
        {
            IsSessionsExpanded = !IsSessionsExpanded;
        }

        [RelayCommand]
        public void LaunchSpotifyApp()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("spotify:") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Launch Spotify App failed: " + ex.Message);
                LaunchSpotifyWeb(); // Fallback to web
            }
        }

        [RelayCommand]
        public void LaunchSpotifyWeb()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://open.spotify.com") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Launch Spotify Web failed: " + ex.Message);
            }
        }

        [RelayCommand]
        public void LaunchYandexMusicApp()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("yandexmusic:") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Launch Yandex Music App failed: " + ex.Message);
                LaunchYandexMusicWeb(); // Fallback to web
            }
        }

        [RelayCommand]
        public void LaunchYandexMusicWeb()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://music.yandex.ru") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Launch Yandex Music Web failed: " + ex.Message);
            }
        }

        [RelayCommand]
        public void ToggleMute()
        {
            IsMuted = !IsMuted;
        }

        [RelayCommand]
        public void SelectSession(string appId)
        {
            _playbackService.SelectedAppId = appId;
            
            // Immediately update selection status in our ActiveSessions list
            foreach (var s in ActiveSessions)
            {
                s.IsSelected = (s.AppId == appId);
            }
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
                    else if (player.Contains("YandexMusic", StringComparison.OrdinalIgnoreCase) || (player.Contains("Yandex", StringComparison.OrdinalIgnoreCase) && player.Contains("music", StringComparison.OrdinalIgnoreCase))) player = "Яндекс.Музыка";
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
                        IsSelected = appId == _playbackService.ActiveSessionAppId,
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

        private void RefreshVolumeSessions()
        {
            try
            {
                // Synchronize Master Volume first
                UpdateVolumeFromSystem();

                var currentSessions = VolumeService.GetAppAudioSessions();
                
                // Use UI thread to modify ObservableCollection
                Dispatcher.UIThread.Post(() =>
                {
                    // Remove sessions that no longer exist
                    for (int i = AppVolumeSessions.Count - 1; i >= 0; i--)
                    {
                        var vm = AppVolumeSessions[i];
                        if (!currentSessions.Any(s => s.SessionInstanceId == vm.SessionInstanceId))
                        {
                            AppVolumeSessions.RemoveAt(i);
                        }
                    }

                    // Add or update sessions
                    foreach (var session in currentSessions)
                    {
                        var existing = AppVolumeSessions.FirstOrDefault(s => s.SessionInstanceId == session.SessionInstanceId);
                        if (existing != null)
                        {
                            existing.UpdateFromModel(session);
                        }
                        else
                        {
                            var newVm = new AppVolumeSessionViewModel
                            {
                                SessionInstanceId = session.SessionInstanceId,
                                DisplayName = session.DisplayName,
                                ProcessName = session.ProcessName,
                                IsSystemSounds = session.IsSystemSounds
                            };
                            newVm.UpdateFromModel(session);
                            AppVolumeSessions.Add(newVm);
                        }
                    }
                });
            }
            catch { }
        }

        private void VisualizerTimer_Tick(object? sender, EventArgs e)
        {
            if (!IsPlaying || !HasMedia)
            {
                if (_smoothPeak > 0.01)
                {
                    _smoothPeak *= 0.85;
                }
                else
                {
                    _smoothPeak = 0.0;
                }
            }
            else
            {
                try
                {
                    double rawPeak = 0.0;
                    var activeSession = GetActivePlayerSession();
                    if (activeSession != null && !string.IsNullOrEmpty(activeSession.ProcessName))
                    {
                        rawPeak = VolumeService.GetAudioPeakForProcess(activeSession.ProcessName);
                    }
                    else
                    {
                        rawPeak = VolumeService.GetAudioPeak();
                    }

                    // Maintain a history of the last 2 seconds of peaks (80 ticks at 25ms interval)
                    _peakHistory.Enqueue(rawPeak);
                    if (_peakHistory.Count > 80)
                    {
                        _peakHistory.Dequeue();
                    }

                    // Find the max peak in the recent history (with a minimum floor to avoid noise boosting)
                    double maxInHistory = _peakHistory.Max();
                    _maxRecentPeak = Math.Max(0.05, maxInHistory);

                    // Normalize the current peak relative to the recent maximum
                    double normalizedPeak = Math.Min(1.0, rawPeak / _maxRecentPeak);

                    // Mix 60% linear tracking (for smooth melody, vocals, and mids)
                    // and 40% squared tracking (for sharp, punchy bass and drum hits)
                    double melodySignal = normalizedPeak;
                    double bassSignal = Math.Pow(normalizedPeak, 2.0);
                    double mixedPeak = (melodySignal * 0.6) + (bassSignal * 0.4);

                    if (mixedPeak > _smoothPeak)
                    {
                        _smoothPeak = mixedPeak; // Fast attack
                    }
                    else
                    {
                        _smoothPeak = _smoothPeak * 0.76 + mixedPeak * 0.24; // Faster decay for responsive melody tracking
                    }
                }
                catch
                {
                    _smoothPeak = 0.0;
                }
            }

            // Exaggerated multipliers for strong visual beats
            GlowRingScale = 1.0 + (_smoothPeak * 0.28);
            GlowRingOpacity = 0.35 + (_smoothPeak * 0.65);
            VinylDiskScale = 1.0 + (_smoothPeak * 0.08);
        }
    }

    public partial class MediaSessionInfo : ObservableObject
    {
        public string AppId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public bool IsPlaying { get; set; }

        [ObservableProperty]
        private bool _isSelected;

        public IAsyncRelayCommand TogglePlayCommand { get; set; } = null!;
        public IAsyncRelayCommand NextCommand { get; set; } = null!;
        public IAsyncRelayCommand PrevCommand { get; set; } = null!;
    }
}
