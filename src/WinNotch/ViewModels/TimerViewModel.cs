using System;
using System.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinNotch.ViewModels
{
    public partial class TimerViewModel : ObservableObject
    {
        private readonly DispatcherTimer _timer;

        [ObservableProperty]
        private TimeSpan _timeRemaining = TimeSpan.FromMinutes(25);

        [ObservableProperty]
        private TimeSpan _totalTime = TimeSpan.FromMinutes(25);

        [ObservableProperty]
        private string _formattedTime = "25:00";

        [ObservableProperty]
        private double _progressValue = 100.0;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private int _selectedMinutes = 25;

        [ObservableProperty]
        private string _pillDisplay = "25:00";

        public event Action? TimerFinished;

        public TimerViewModel()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            UpdateDisplay();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (TimeRemaining > TimeSpan.Zero)
            {
                TimeRemaining = TimeRemaining.Subtract(TimeSpan.FromSeconds(1));
                UpdateDisplay();

                if (TimeRemaining <= TimeSpan.Zero)
                {
                    _timer.Stop();
                    IsRunning = false;
                    try
                    {
                        SystemSounds.Asterisk.Play();
                    }
                    catch { }
                    TimerFinished?.Invoke();
                }
            }
        }

        private void UpdateDisplay()
        {
            FormattedTime = $"{(int)TimeRemaining.TotalMinutes}:{TimeRemaining.Seconds:D2}";
            PillDisplay = FormattedTime;
            ProgressValue = TotalTime.TotalSeconds > 0
                ? Math.Clamp((TimeRemaining.TotalSeconds / TotalTime.TotalSeconds) * 100.0, 0, 100)
                : 0;
        }

        [RelayCommand]
        public void ToggleStartPause()
        {
            if (IsRunning)
            {
                _timer.Stop();
                IsRunning = false;
            }
            else
            {
                if (TimeRemaining <= TimeSpan.Zero)
                {
                    TimeRemaining = TotalTime;
                    UpdateDisplay();
                }
                _timer.Start();
                IsRunning = true;
            }
        }

        [RelayCommand]
        public void Reset()
        {
            _timer.Stop();
            IsRunning = false;
            TimeRemaining = TotalTime;
            UpdateDisplay();
        }

        [RelayCommand]
        public void SelectPreset(object? parameter)
        {
            if (parameter == null) return;
            int minutes = parameter is int i ? i : (int.TryParse(parameter.ToString(), out int parsed) ? parsed : 25);
            SelectedMinutes = minutes;
            TotalTime = TimeSpan.FromMinutes(minutes);
            TimeRemaining = TotalTime;
            if (IsRunning)
            {
                _timer.Stop();
                IsRunning = false;
            }
            UpdateDisplay();
        }
    }
}
