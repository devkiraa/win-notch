using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
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

        public async Task RefreshMediaStateAsync()
        {
            if (_currentSession == null)
            {
                MediaStateChanged?.Invoke(this, new MediaStateEventArgs { HasMedia = false });
                return;
            }

            try
            {
                var mediaProps = await _currentSession.TryGetMediaPropertiesAsync();
                var playbackInfo = _currentSession.GetPlaybackInfo();
                var timeline = _currentSession.GetTimelineProperties();

                bool isPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                BitmapSource? thumbnail = null;
                if (mediaProps?.Thumbnail != null)
                {
                    try
                    {
                        using var streamRef = await mediaProps.Thumbnail.OpenReadAsync();
                        using var netStream = streamRef.AsStreamForRead();
                        var memStream = new MemoryStream();
                        await netStream.CopyToAsync(memStream);
                        memStream.Position = 0;

                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = memStream;
                        bmp.EndInit();
                        bmp.Freeze();
                        thumbnail = bmp;
                    }
                    catch
                    {
                        thumbnail = null;
                    }
                }

                bool hasMedia = !string.IsNullOrWhiteSpace(mediaProps?.Title);

                MediaStateChanged?.Invoke(this, new MediaStateEventArgs
                {
                    HasMedia = hasMedia,
                    Title = mediaProps?.Title ?? string.Empty,
                    Artist = mediaProps?.Artist ?? string.Empty,
                    AlbumTitle = mediaProps?.AlbumTitle ?? string.Empty,
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
