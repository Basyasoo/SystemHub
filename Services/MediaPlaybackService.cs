using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace MacStyleHub.Services
{
    public class MediaPlaybackService
    {
        private GlobalSystemMediaTransportControlsSessionManager? _manager;

        public event Action<string, string, bool, string>? MediaChanged;
        public event Action? SessionsListChanged;

        private string? _selectedAppId;
        private GlobalSystemMediaTransportControlsSession? _activeSubscribedSession;

        public string? SelectedAppId
        {
            get => _selectedAppId;
            set
            {
                if (_selectedAppId != value)
                {
                    _selectedAppId = value;
                    UpdateCurrentSession();
                }
            }
        }

        public string? ActiveSessionAppId => _activeSubscribedSession?.SourceAppUserModelId;

        public async Task InitializeAsync()
        {
            try
            {
                _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                _manager.CurrentSessionChanged += OnCurrentSessionChanged;
                _manager.SessionsChanged += OnSessionsChanged;
                UpdateCurrentSession();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GSMTC Session Manager Error: " + ex.Message);
            }
        }

        private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            UpdateCurrentSession();
            SessionsListChanged?.Invoke();
        }

        private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            UpdateCurrentSession();
        }

        private void UpdateCurrentSession()
        {
            if (_manager == null) return;

            try
            {
                GlobalSystemMediaTransportControlsSession? session = null;
                if (!string.IsNullOrEmpty(_selectedAppId))
                {
                    foreach (var s in _manager.GetSessions())
                    {
                        if (s.SourceAppUserModelId == _selectedAppId)
                        {
                            session = s;
                            break;
                        }
                    }
                }

                // If selected app not found or not set, fallback to default current session
                if (session == null)
                {
                    session = _manager.GetCurrentSession();
                    if (!string.IsNullOrEmpty(_selectedAppId))
                    {
                        _selectedAppId = null;
                    }
                }

                // Unsubscribe from previous session if it's changing
                if (_activeSubscribedSession != session)
                {
                    if (_activeSubscribedSession != null)
                    {
                        try
                        {
                            _activeSubscribedSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                            _activeSubscribedSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                        }
                        catch { }
                    }

                    _activeSubscribedSession = session;

                    if (_activeSubscribedSession != null)
                    {
                        _activeSubscribedSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                        _activeSubscribedSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                    }
                }

                if (_activeSubscribedSession != null)
                {
                    _ = TriggerUpdateAsync(_activeSubscribedSession);
                }
                else
                {
                    MediaChanged?.Invoke("", "", false, "");
                }
            }
            catch
            {
                _activeSubscribedSession = null;
                MediaChanged?.Invoke("", "", false, "");
            }
        }

        private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            _ = TriggerUpdateAsync(sender);
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            _ = TriggerUpdateAsync(sender);
        }

        private async Task TriggerUpdateAsync(GlobalSystemMediaTransportControlsSession session)
        {
            try
            {
                var props = await session.TryGetMediaPropertiesAsync();
                var info = session.GetPlaybackInfo();
                
                bool isPlaying = info != null && info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                string player = session.SourceAppUserModelId ?? "";
                
                // Refine player display names
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

                MediaChanged?.Invoke(props.Title ?? "", props.Artist ?? "", isPlaying, player);
            }
            catch
            {
                MediaChanged?.Invoke("", "", false, "");
            }
        }

        // Win32 Keyboard Emulation for Media Keys (Fallback when no active media player session exists)
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

        private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
        private const byte VK_MEDIA_PREV_TRACK = 0xB1;
        private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private static void SendMediaKey(byte vk)
        {
            keybd_event(vk, 0, 0, IntPtr.Zero); // Key Down
            keybd_event(vk, 0, KEYEVENTF_KEYUP, IntPtr.Zero); // Key Up
        }

        public async Task TogglePlayPauseAsync()
        {
            try
            {
                var session = _activeSubscribedSession;
                if (session != null)
                {
                    await session.TryTogglePlayPauseAsync();
                }
                else
                {
                    SendMediaKey(VK_MEDIA_PLAY_PAUSE);
                }
            }
            catch
            {
                SendMediaKey(VK_MEDIA_PLAY_PAUSE);
            }
        }

        public async Task SkipNextAsync()
        {
            try
            {
                var session = _activeSubscribedSession;
                if (session != null)
                {
                    await session.TrySkipNextAsync();
                }
                else
                {
                    SendMediaKey(VK_MEDIA_NEXT_TRACK);
                }
            }
            catch
            {
                SendMediaKey(VK_MEDIA_NEXT_TRACK);
            }
        }

        public async Task SkipPreviousAsync()
        {
            try
            {
                var session = _activeSubscribedSession;
                if (session != null)
                {
                    await session.TrySkipPreviousAsync();
                }
                else
                {
                    SendMediaKey(VK_MEDIA_PREV_TRACK);
                }
            }
            catch
            {
                SendMediaKey(VK_MEDIA_PREV_TRACK);
            }
        }

        public System.Collections.Generic.IReadOnlyList<GlobalSystemMediaTransportControlsSession> GetSessions()
        {
            if (_manager == null)
                return Array.Empty<GlobalSystemMediaTransportControlsSession>();
            try
            {
                return _manager.GetSessions();
            }
            catch
            {
                return Array.Empty<GlobalSystemMediaTransportControlsSession>();
            }
        }

        public async Task TogglePlayPauseSessionAsync(string appId)
        {
            if (_manager == null) return;
            try
            {
                foreach (var session in _manager.GetSessions())
                {
                    if (session.SourceAppUserModelId == appId)
                    {
                        await session.TryTogglePlayPauseAsync();
                        break;
                    }
                }
            }
            catch { }
        }

        public async Task SkipNextSessionAsync(string appId)
        {
            if (_manager == null) return;
            try
            {
                foreach (var session in _manager.GetSessions())
                {
                    if (session.SourceAppUserModelId == appId)
                    {
                        await session.TrySkipNextAsync();
                        break;
                    }
                }
            }
            catch { }
        }

        public async Task SkipPreviousSessionAsync(string appId)
        {
            if (_manager == null) return;
            try
            {
                foreach (var session in _manager.GetSessions())
                {
                    if (session.SourceAppUserModelId == appId)
                    {
                        await session.TrySkipPreviousAsync();
                        break;
                    }
                }
            }
            catch { }
        }
    }
}
