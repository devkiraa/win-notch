using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WinNotch.Services;
using WinNotch.ViewModels;

namespace WinNotch
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly ClipboardService _clipboardService;
        private readonly SystemStatusService _systemStatusService;
        private readonly CodingAgentService _codingAgentService;
        private readonly DispatcherTimer _equalizerTimer;
        private readonly Random _random = new();
        private Storyboard? _vinylStoryboard;
        private bool _isVinylInitialized;

        public static readonly DependencyProperty CurrentCornerRadiusProperty =
            DependencyProperty.Register(nameof(CurrentCornerRadius), typeof(double), typeof(MainWindow), new PropertyMetadata(16.0, (d, e) =>
            {
                if (d is MainWindow w && w.NotchBorder != null)
                {
                    w.NotchBorder.CornerRadius = new CornerRadius((double)e.NewValue);
                }
            }));

        public double CurrentCornerRadius
        {
            get => (double)GetValue(CurrentCornerRadiusProperty);
            set => SetValue(CurrentCornerRadiusProperty, value);
        }

        public MainWindow(
            MainViewModel viewModel,
            ClipboardService clipboardService,
            SystemStatusService systemStatusService,
            CodingAgentService codingAgentService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _clipboardService = clipboardService;
            _systemStatusService = systemStatusService;
            _codingAgentService = codingAgentService;
            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.RequestResetPosition += ResetToDefaultPosition;

            // Equalizer animation timer
            _equalizerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };
            _equalizerTimer.Tick += EqualizerTimer_Tick;

            _viewModel.Media.PropertyChanged += Media_PropertyChanged;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Position top-center of primary display
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            Left = (screenWidth - Width) / 2.0;
            Top = 0;

            // Set initial notch size and corner radius
            NotchBorder.Width = _viewModel.NotchWidth;
            NotchBorder.Height = _viewModel.NotchHeight;
            NotchBorder.CornerRadius = new CornerRadius(_viewModel.CornerRadiusValue);
            CurrentCornerRadius = _viewModel.CornerRadiusValue;

            InitializeVinylAnimation();
            UpdateEqualizer();
            UpdateVinylRotation();

            // Initialize Win32 services with Window Handle
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            var hwnd = helper.Handle;

            _systemStatusService.Start(hwnd);
            _systemStatusService.FullscreenChanged += OnFullscreenChanged;

            _clipboardService.Start(hwnd);
            _clipboardService.ClipboardTextChanged += text => _viewModel.Clips.AddClip(text);

            _codingAgentService.Start();
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            Dispatcher.Invoke(() =>
            {
                var anim = new DoubleAnimation
                {
                    To = isFullscreen ? 0.0 : 1.0,
                    Duration = TimeSpan.FromMilliseconds(220),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                BeginAnimation(OpacityProperty, anim);
                IsHitTestVisible = !isFullscreen;
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _systemStatusService.Stop();
            _clipboardService.Stop();
            _codingAgentService.Stop();
        }

        private void InitializeVinylAnimation()
        {
            if (_isVinylInitialized) return;

            var rotateAnim = new DoubleAnimation
            {
                From = 0.0,
                To = 360.0,
                Duration = TimeSpan.FromSeconds(4),
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(rotateAnim, VinylDisc);
            Storyboard.SetTargetProperty(rotateAnim, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));

            var miniRotateAnim = new DoubleAnimation
            {
                From = 0.0,
                To = 360.0,
                Duration = TimeSpan.FromSeconds(4),
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(miniRotateAnim, MiniVinylDisc);
            Storyboard.SetTargetProperty(miniRotateAnim, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));

            _vinylStoryboard = new Storyboard();
            _vinylStoryboard.Children.Add(rotateAnim);
            _vinylStoryboard.Children.Add(miniRotateAnim);

            // Bind storyboard clock to window instance
            _vinylStoryboard.Begin(this, isControllable: true);
            _isVinylInitialized = true;

            // Immediately pause if playback is not currently active
            if (!_viewModel.Media.IsPlaying)
            {
                _vinylStoryboard.Pause(this);
            }
        }

        private void UpdateVinylRotation()
        {
            if (_vinylStoryboard == null || !_isVinylInitialized) return;

            if (_viewModel.Media.IsPlaying)
            {
                _vinylStoryboard.Resume(this);
            }
            else
            {
                _vinylStoryboard.Pause(this);
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.NotchWidth) ||
                e.PropertyName == nameof(MainViewModel.NotchHeight) ||
                e.PropertyName == nameof(MainViewModel.CornerRadiusValue))
            {
                AnimateNotchDimensions(_viewModel.NotchWidth, _viewModel.NotchHeight, _viewModel.CornerRadiusValue);
            }
        }

        private void Media_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MediaViewModel.IsPlaying))
            {
                UpdateEqualizer();
                UpdateVinylRotation();
            }
        }

        private void UpdateEqualizer()
        {
            if (_viewModel.Media.IsPlaying && !_viewModel.IsExpanded)
            {
                _equalizerTimer.Start();
            }
            else
            {
                _equalizerTimer.Stop();
                AnimateEqBar(EqBar1, 8);
                AnimateEqBar(EqBar2, 14);
                AnimateEqBar(EqBar3, 6);
            }
        }

        private void EqualizerTimer_Tick(object? sender, EventArgs e)
        {
            AnimateEqBar(EqBar1, _random.Next(4, 13));
            AnimateEqBar(EqBar2, _random.Next(8, 18));
            AnimateEqBar(EqBar3, _random.Next(3, 11));
        }

        private static void AnimateEqBar(FrameworkElement bar, double targetHeight)
        {
            var anim = new DoubleAnimation
            {
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(110),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            bar.BeginAnimation(FrameworkElement.HeightProperty, anim);
        }

        private void AnimateNotchDimensions(double targetWidth, double targetHeight, double targetRadius)
        {
            var duration = TimeSpan.FromMilliseconds(280);
            var easing = new QuarticEase { EasingMode = EasingMode.EaseOut };

            var widthAnim = new DoubleAnimation
            {
                To = targetWidth,
                Duration = duration,
                EasingFunction = easing
            };

            var heightAnim = new DoubleAnimation
            {
                To = targetHeight,
                Duration = duration,
                EasingFunction = easing
            };

            var radiusAnim = new DoubleAnimation
            {
                To = targetRadius,
                Duration = duration,
                EasingFunction = easing
            };

            NotchBorder.BeginAnimation(WidthProperty, widthAnim);
            NotchBorder.BeginAnimation(HeightProperty, heightAnim);
            BeginAnimation(CurrentCornerRadiusProperty, radiusAnim);

            // Smooth cross-fade and entrance translation
            if (_viewModel.IsExpanded)
            {
                var fadeOutCollapsed = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(70));
                CollapsedView.BeginAnimation(OpacityProperty, fadeOutCollapsed);

                var fadeInExpanded = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200))
                {
                    BeginTime = TimeSpan.FromMilliseconds(60),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                ExpandedView.BeginAnimation(OpacityProperty, fadeInExpanded);

                var slideInExpanded = new DoubleAnimation(-8.0, 0.0, TimeSpan.FromMilliseconds(240))
                {
                    BeginTime = TimeSpan.FromMilliseconds(40),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                ExpandedTranslate.BeginAnimation(TranslateTransform.YProperty, slideInExpanded);
            }
            else
            {
                var fadeOutExpanded = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(70));
                ExpandedView.BeginAnimation(OpacityProperty, fadeOutExpanded);

                var fadeInCollapsed = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200))
                {
                    BeginTime = TimeSpan.FromMilliseconds(60),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                CollapsedView.BeginAnimation(OpacityProperty, fadeInCollapsed);
            }
        }

        public void ResetToDefaultPosition()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var targetLeft = (screenWidth - Width) / 2.0;
            var targetTop = 0.0;

            var duration = TimeSpan.FromMilliseconds(280);
            var easing = new QuarticEase { EasingMode = EasingMode.EaseOut };

            var leftAnim = new DoubleAnimation(targetLeft, duration) { EasingFunction = easing };
            var topAnim = new DoubleAnimation(targetTop, duration) { EasingFunction = easing };

            BeginAnimation(LeftProperty, leftAnim);
            BeginAnimation(TopProperty, topAnim);
        }

        private void NotchBorder_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                SystemStatusService.VolumeUp();
            }
            else if (e.Delta < 0)
            {
                SystemStatusService.VolumeDown();
            }
            e.Handled = true;
        }

        private void ProgressBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement bar && _viewModel.Media.HasMedia)
            {
                var x = e.GetPosition(bar).X;
                var width = bar.ActualWidth;
                if (width > 0)
                {
                    var pct = Math.Clamp((x / width) * 100.0, 0, 100);
                    _ = _viewModel.Media.SeekCommand.ExecuteAsync(pct);
                    e.Handled = true;
                }
            }
        }

        private void NotchBorder_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && _viewModel.IsExpanded && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                DragMove();
                e.Handled = true;
            }
        }

        private void NotchBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (!_viewModel.IsExpanded)
                {
                    _viewModel.IsExpanded = true;
                    e.Handled = true;
                }
            }
        }

        private void NotchBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (_viewModel.IsExpanded)
            {
                NotchBorder.Cursor = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control 
                    ? Cursors.SizeAll 
                    : Cursors.Arrow;
            }
            else
            {
                NotchBorder.Cursor = Cursors.Hand;
            }
        }

        private void NotchBorder_MouseEnter(object sender, MouseEventArgs e)
        {
        }

        private void NotchBorder_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void OuterGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.IsExpanded)
            {
                _viewModel.CollapseCommand.Execute(null);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _viewModel.IsExpanded)
            {
                _viewModel.CollapseCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (_viewModel.IsExpanded && (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl))
            {
                NotchBorder.Cursor = Cursors.SizeAll;
            }
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                NotchBorder.Cursor = _viewModel.IsExpanded ? Cursors.Arrow : Cursors.Hand;
            }
        }

        private void TodoInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.Todo.AddTodoCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
