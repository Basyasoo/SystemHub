using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
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
        private Bitmap? _albumArt;

        [ObservableProperty]
        private Avalonia.Media.Geometry? _appIconGeometry;

        private const string PathIconMusic = "M216,40V168a48,48,0,1,1-48-48,47.4,47.4,0,0,1,32,12.33V80L96,96v88a48,48,0,1,1-48-48,47.4,47.4,0,0,1,32,12.33V56a16,16,0,0,1,12.48-15.6l96-24A16,16,0,0,1,216,40ZM120,184a32,32,0,1,0,32,32A32,32,0,0,0,120,184ZM48,184a32,32,0,1,0,32,32A32,32,0,0,0,48,184Z";
        private const string PathSpotifyLogo = "M8 0a8 8 0 1 0 0 16A8 8 0 0 0 8 0m3.669 11.538a.5.5 0 0 1-.686.165c-1.879-1.147-4.243-1.407-7.028-.77a.499.499 0 0 1-.222-.973c3.048-.696 5.662-.397 7.77.892a.5.5 0 0 1 .166.686m.979-2.178a.624.624 0 0 1-.858.205c-2.15-1.321-5.428-1.704-7.972-.932a.625.625 0 0 1-.362-1.194c2.905-.881 6.517-.454 8.986 1.063a.624.624 0 0 1 .206.858m.084-2.268C10.154 5.56 5.9 5.419 3.438 6.166a.748.748 0 1 1-.434-1.432c2.825-.857 7.523-.692 10.492 1.07a.747.747 0 1 1-.764 1.288";
        private const string PathYandexMusicLogo = "m 51.62106,27.246576 -0.15602,-0.775668 -6.59622,-1.152272 3.832962,-5.185702 -0.445616,-0.487482 -5.637864,2.70361 0.712978,-7.180076 -0.57941,-0.332162 -3.431792,5.805814 -3.85495,-8.642522 h -0.668534 l 0.913678,8.354572 -9.69351,-7.756214 -0.824558,0.243742 7.464988,9.37398 -14.774185,-4.919736 -0.668534,0.753444 13.192207,7.512706 -18.206214,1.506424 -0.200467,1.13052 18.919191,2.060806 -15.77722,13.05256 0.668534,0.908524 18.785388,-10.215842 -3.721146,17.994282 h 1.1366 l 7.19762,-16.930662 4.389916,13.252084 0.78011,-0.598354 -1.805132,-13.473608 6.841128,7.756212 0.445846,-0.709238 -5.236928,-9.617718 7.309196,2.703608 0.0669,-0.819878 -6.551544,-4.831084 6.172832,-1.484668 z";
        private const string PathChromeLogo = "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm0,16a87.65,87.65,0,0,1,65.3,28.7H62.7A87.65,87.65,0,0,1,128,40ZM41.87,144A88.13,88.13,0,0,1,40,128a87.35,87.35,0,0,1,6.59-33.1L92.7,174.5A88,88,0,0,1,41.87,144Zm12.63-64H178a87.79,87.79,0,0,1,26.47,40H144.38ZM128,216a87.65,87.65,0,0,1-65.3-28.7H86.44l29.8-51.62a8,8,0,0,1,13.86,0L160,186.29A87.77,87.77,0,0,1,128,216Zm47.74-42.5-38.36-66.42A23.9,23.9,0,0,0,116.8,96H104v64H90.15l-33-57.17A88.08,88.08,0,0,1,190.13,144ZM214,144H160.38L128,87.89l33.15-57.41A88.06,88.06,0,0,1,214,144Z";
        private const string PathTelegramLogo = "M227.32,28.68a16,16,0,0,0-14.86-3.75l-176,48a16,16,0,0,0-2.82,29.84l65.6,28.11,28.11,65.6a15.89,15.89,0,0,0,14.84,9.52h.16a15.93,15.93,0,0,0,14.84-11l48-176A16,16,0,0,0,227.32,28.68ZM129.58,187,104,127.31l76.69-76.69a8,8,0,0,0-11.31-11.31L92.69,116,33,90.42l176-48Z";

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
            _appIconGeometry = Avalonia.Media.Geometry.Parse(PathIconMusic);
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

        [ObservableProperty]
        private bool _isEqualizerExpanded = false;

        [RelayCommand]
        public void ToggleEqualizer()
        {
            IsEqualizerExpanded = !IsEqualizerExpanded;
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

        private void PlaybackService_MediaChanged(string title, string artist, bool isPlaying, string player, byte[]? artBytes)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Title = title;
                Artist = artist;
                IsPlaying = isPlaying;
                PlayerName = player;
                HasMedia = !string.IsNullOrEmpty(title);

                string appLower = (player ?? "").ToLower();
                string selectedPath = PathIconMusic;
                if (appLower.Contains("spotify"))
                {
                    selectedPath = PathSpotifyLogo;
                }
                else if (appLower.Contains("yandex") || appLower.Contains("яндекс"))
                {
                    selectedPath = PathYandexMusicLogo;
                }
                else if (appLower.Contains("chrome") || appLower.Contains("browser") || appLower.Contains("edge") || appLower.Contains("opera") || appLower.Contains("firefox"))
                {
                    selectedPath = PathChromeLogo;
                }
                else if (appLower.Contains("telegram"))
                {
                    selectedPath = PathTelegramLogo;
                }

                try
                {
                    AppIconGeometry = Avalonia.Media.Geometry.Parse(selectedPath);
                }
                catch
                {
                    AppIconGeometry = Avalonia.Media.Geometry.Parse(PathIconMusic);
                }
                
                if (artBytes != null && artBytes.Length > 0)
                {
                    try
                    {
                        using (var ms = new System.IO.MemoryStream(artBytes))
                        {
                            AlbumArt = new Bitmap(ms);
                        }
                    }
                    catch
                    {
                        AlbumArt = null;
                    }
                }
                else
                {
                    AlbumArt = null;
                }
                
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
