using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace WinNotch.Services
{
    public class MediaStateEventArgs : EventArgs
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string AlbumTitle { get; set; } = string.Empty;
        public BitmapSource? Thumbnail { get; set; }
        public bool IsPlaying { get; set; }
        public bool HasMedia { get; set; }
        public TimeSpan Position { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class MediaService
    {
        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
        private GlobalSystemMediaTransportControlsSession? _currentSession;

        public event EventHandler<MediaStateEventArgs>? MediaStateChanged;

        public async Task InitializeAsync()
        {
            try
            {
                _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                if (_sessionManager != null)
                {
                    _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
                    HookCurrentSession();
                    await RefreshMediaStateAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MediaService Init error: {ex.Message}");
            }
        }

        private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            HookCurrentSession();
            _ = RefreshMediaStateAsync();
        }

        private void HookCurrentSession()
        {
            if (_sessionManager == null) return;

            try
            {
                if (_currentSession != null)
                {
                    _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                    _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                    _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
                }

                _currentSession = _sessionManager.GetCurrentSession();
                if (_currentSession != null)
                {
                    _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                    _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                    _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HookCurrentSession error: {ex.Message}");
            }
        }

        private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            _ = RefreshMediaStateAsync();
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            _ = RefreshMediaStateAsync();
        }

        private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            _ = RefreshMediaStateAsync();
        }

        private BitmapSource? _lastThumbnail;
        private string _lastTrackKey = string.Empty;
        private DispatcherTimer? _heartbeatTimer;

        private void StartHeartbeat()
        {
            if (_heartbeatTimer != null) return;
            _heartbeatTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2.5)
            };
            _heartbeatTimer.Tick += async (s, e) =>
            {
                if (_currentSession != null)
                {
                    await RefreshMediaStateAsync();
                }
            };
            _heartbeatTimer.Start();
        }

        public async Task RefreshMediaStateAsync()
        {
            StartHeartbeat();

            if (_currentSession == null)
            {
                _lastThumbnail = null;
                _lastTrackKey = string.Empty;
                MediaStateChanged?.Invoke(this, new MediaStateEventArgs { HasMedia = false });
                return;
            }

            try
            {
                var mediaProps = await _currentSession.TryGetMediaPropertiesAsync();
                var playbackInfo = _currentSession.GetPlaybackInfo();
                var timeline = _currentSession.GetTimelineProperties();

                bool isPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                string title = mediaProps?.Title ?? string.Empty;
                string artist = mediaProps?.Artist ?? string.Empty;
                string album = mediaProps?.AlbumTitle ?? string.Empty;
                bool hasMedia = !string.IsNullOrWhiteSpace(title);

                string trackKey = $"{title}::{artist}";
                if (trackKey != _lastTrackKey)
                {
                    _lastTrackKey = trackKey;
                    _lastThumbnail = null;
                }

                BitmapSource? thumbnail = null;
                if (mediaProps?.Thumbnail != null)
                {
                    thumbnail = await LoadThumbnailAsync(mediaProps.Thumbnail);
                    if (thumbnail != null)
                    {
                        _lastThumbnail = thumbnail;
                    }
                }

                // If currently null but we already loaded thumbnail for this track, preserve it!
                if (thumbnail == null && _lastThumbnail != null)
                {
                    thumbnail = _lastThumbnail;
                }

                // If still null and we have media, poll for delayed thumbnail from OS
                if (thumbnail == null && hasMedia)
                {
                    _ = PollThumbnailAsync(title);
                }

                MediaStateChanged?.Invoke(this, new MediaStateEventArgs
                {
                    HasMedia = hasMedia,
                    Title = title,
                    Artist = artist,
                    AlbumTitle = album,
                    Thumbnail = thumbnail,
                    IsPlaying = isPlaying,
                    Position = timeline?.Position ?? TimeSpan.Zero,
                    EndTime = timeline?.EndTime ?? TimeSpan.Zero
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshMediaState error: {ex.Message}");
            }
        }

        private static async Task<BitmapSource?> LoadThumbnailAsync(IRandomAccessStreamReference? thumbRef)
        {
            if (thumbRef == null) return null;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    using var streamRef = await thumbRef.OpenReadAsync();
                    if (streamRef == null || streamRef.Size == 0)
                    {
                        if (attempt < 3) await Task.Delay(150 * attempt);
                        continue;
                    }

                    using var netStream = streamRef.AsStreamForRead();
                    using var memStream = new MemoryStream();
                    await netStream.CopyToAsync(memStream);

                    if (memStream.Length == 0)
                    {
                        if (attempt < 3) await Task.Delay(150 * attempt);
                        continue;
                    }

                    memStream.Seek(0, SeekOrigin.Begin);

                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache | BitmapCreateOptions.PreservePixelFormat;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = memStream;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MediaService] Attempt {attempt} load thumbnail error: {ex.Message}");
                    if (attempt < 3) await Task.Delay(200 * attempt);
                }
            }
            return null;
        }

        private async Task PollThumbnailAsync(string? expectedTitle)
        {
            if (string.IsNullOrWhiteSpace(expectedTitle)) return;

            int[] delays = { 300, 700, 1500, 3000 };
            foreach (var delay in delays)
            {
                await Task.Delay(delay);
                if (_currentSession == null) return;

                try
                {
                    var props = await _currentSession.TryGetMediaPropertiesAsync();
                    if (props == null || props.Title != expectedTitle) return;

                    if (props.Thumbnail != null)
                    {
                        var thumb = await LoadThumbnailAsync(props.Thumbnail);
                        if (thumb != null)
                        {
                            _lastThumbnail = thumb;
                            var playback = _currentSession.GetPlaybackInfo();
                            var timeline = _currentSession.GetTimelineProperties();
                            bool isPlaying = playback?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                            MediaStateChanged?.Invoke(this, new MediaStateEventArgs
                            {
                                HasMedia = true,
                                Title = props.Title,
                                Artist = props.Artist ?? string.Empty,
                                AlbumTitle = props.AlbumTitle ?? string.Empty,
                                Thumbnail = thumb,
                                IsPlaying = isPlaying,
                                Position = timeline?.Position ?? TimeSpan.Zero,
                                EndTime = timeline?.EndTime ?? TimeSpan.Zero
                            });
                            return;
                        }
                    }
                }
                catch { }
            }
        }

        public async Task PlayPauseAsync()
        {
            if (_currentSession == null) return;
            try
            {
                await _currentSession.TryTogglePlayPauseAsync();
                await Task.Delay(150);
                await RefreshMediaStateAsync();
            }
            catch { }
        }

        public async Task NextAsync()
        {
            if (_currentSession == null) return;
            try
            {
                await _currentSession.TrySkipNextAsync();
                await Task.Delay(250);
                await RefreshMediaStateAsync();
            }
            catch { }
        }

        public async Task SeekAsync(TimeSpan position)
        {
            if (_currentSession == null) return;
            try
            {
                await _currentSession.TryChangePlaybackPositionAsync(position.Ticks);
                await Task.Delay(50);
                await RefreshMediaStateAsync();
            }
            catch { }
        }

        public async Task PreviousAsync()
        {
            if (_currentSession == null) return;
            try
            {
                await _currentSession.TrySkipPreviousAsync();
                await Task.Delay(250);
                await RefreshMediaStateAsync();
            }
            catch { }
        }
    }
}
