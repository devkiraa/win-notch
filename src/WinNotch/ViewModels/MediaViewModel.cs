using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinNotch.Services;

namespace WinNotch.ViewModels
{
    public partial class MediaViewModel : ObservableObject
    {
        private readonly MediaService _mediaService;

        [ObservableProperty]
        private string _title = "Ready to Play";

        [ObservableProperty]
        private string _artist = "No active media";

        [ObservableProperty]
        private string _albumTitle = string.Empty;

        [ObservableProperty]
        private BitmapSource? _thumbnail;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private bool _hasMedia;

        [ObservableProperty]
        private double _progressValue;

        [ObservableProperty]
        private string _timeDisplay = "0:00 / 0:00";

        [ObservableProperty]
        private string _elapsedText = "0:00";

        [ObservableProperty]
        private string _totalText = "0:00";

        [ObservableProperty]
        private TimeSpan _totalDuration = TimeSpan.Zero;

        [ObservableProperty]
        private TimeSpan _currentPosition = TimeSpan.Zero;

        public MediaViewModel(MediaService mediaService)
        {
            _mediaService = mediaService;
            _mediaService.MediaStateChanged += OnMediaStateChanged;
        }

        private void OnMediaStateChanged(object? sender, MediaStateEventArgs e)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                HasMedia = e.HasMedia;
                if (e.HasMedia)
                {
                    Title = string.IsNullOrWhiteSpace(e.Title) ? "Unknown Track" : e.Title;
                    Artist = string.IsNullOrWhiteSpace(e.Artist) ? "Unknown Artist" : e.Artist;
                    AlbumTitle = e.AlbumTitle;
                    Thumbnail = e.Thumbnail;
                    IsPlaying = e.IsPlaying;
                    TotalDuration = e.EndTime;
                    CurrentPosition = e.Position;

                    if (e.EndTime > TimeSpan.Zero)
                    {
                        ProgressValue = Math.Clamp(e.Position.TotalSeconds / e.EndTime.TotalSeconds * 100.0, 0, 100);
                        ElapsedText = FormatTime(e.Position);
                        TotalText = FormatTime(e.EndTime);
                        TimeDisplay = $"{ElapsedText} / {TotalText}";
                    }
                    else
                    {
                        ProgressValue = 0;
                        ElapsedText = "0:00";
                        TotalText = "Live";
                        TimeDisplay = "Live";
                    }
                }
                else
                {
                    Title = "Ready to Play";
                    Artist = "Open Spotify, YouTube, or Music";
                    Thumbnail = null;
                    IsPlaying = false;
                    ProgressValue = 0;
                    TotalDuration = TimeSpan.Zero;
                    CurrentPosition = TimeSpan.Zero;
                    ElapsedText = "0:00";
                    TotalText = "0:00";
                    TimeDisplay = "0:00";
                }
            });
        }

        private static string FormatTime(TimeSpan t)
        {
            return t.TotalHours >= 1
                ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
                : $"{t.Minutes}:{t.Seconds:D2}";
        }

        [RelayCommand]
        public async Task SeekAsync(double percentage)
        {
            if (TotalDuration <= TimeSpan.Zero) return;
            var targetSec = TotalDuration.TotalSeconds * (Math.Clamp(percentage, 0, 100) / 100.0);
            var targetPos = TimeSpan.FromSeconds(targetSec);
            CurrentPosition = targetPos;
            ProgressValue = percentage;
            ElapsedText = FormatTime(targetPos);
            TimeDisplay = $"{ElapsedText} / {TotalText}";
            await _mediaService.SeekAsync(targetPos);
        }

        [RelayCommand]
        private async Task PlayPauseAsync()
        {
            await _mediaService.PlayPauseAsync();
        }

        [RelayCommand]
        private async Task NextAsync()
        {
            await _mediaService.NextAsync();
        }

        [RelayCommand]
        private async Task PreviousAsync()
        {
            await _mediaService.PreviousAsync();
        }
    }
}
