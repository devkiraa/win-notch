using System.Windows;
using System.Windows.Controls;

namespace WinNotch.Controls
{
    public partial class AgentLogoControl : UserControl
    {
        public static readonly DependencyProperty LogoKeyProperty =
            DependencyProperty.Register(nameof(LogoKey), typeof(string), typeof(AgentLogoControl), new PropertyMetadata("Default"));

        public static readonly DependencyProperty AccentColorProperty =
            DependencyProperty.Register(nameof(AccentColor), typeof(string), typeof(AgentLogoControl), new PropertyMetadata("#8B5CF6"));

        public static readonly DependencyProperty BgColorProperty =
            DependencyProperty.Register(nameof(BgColor), typeof(string), typeof(AgentLogoControl), new PropertyMetadata("#140E28"));

        public static readonly DependencyProperty BoxSizeProperty =
            DependencyProperty.Register(nameof(BoxSize), typeof(double), typeof(AgentLogoControl),
                new PropertyMetadata(18.0, OnBoxSizeChanged));

        public static readonly DependencyProperty BoxCornerRadiusProperty =
            DependencyProperty.Register(nameof(BoxCornerRadius), typeof(CornerRadius), typeof(AgentLogoControl),
                new PropertyMetadata(new CornerRadius(6)));

        public static readonly DependencyProperty ShowStatusDotProperty =
            DependencyProperty.Register(nameof(ShowStatusDot), typeof(bool), typeof(AgentLogoControl), new PropertyMetadata(false));

        public static readonly DependencyProperty IsRunningProperty =
            DependencyProperty.Register(nameof(IsRunning), typeof(bool), typeof(AgentLogoControl), new PropertyMetadata(true));

        public string LogoKey
        {
            get => (string)GetValue(LogoKeyProperty);
            set => SetValue(LogoKeyProperty, value);
        }

        public string AccentColor
        {
            get => (string)GetValue(AccentColorProperty);
            set => SetValue(AccentColorProperty, value);
        }

        public string BgColor
        {
            get => (string)GetValue(BgColorProperty);
            set => SetValue(BgColorProperty, value);
        }

        public double BoxSize
        {
            get => (double)GetValue(BoxSizeProperty);
            set => SetValue(BoxSizeProperty, value);
        }

        public CornerRadius BoxCornerRadius
        {
            get => (CornerRadius)GetValue(BoxCornerRadiusProperty);
            set => SetValue(BoxCornerRadiusProperty, value);
        }

        public bool ShowStatusDot
        {
            get => (bool)GetValue(ShowStatusDotProperty);
            set => SetValue(ShowStatusDotProperty, value);
        }

        public bool IsRunning
        {
            get => (bool)GetValue(IsRunningProperty);
            set => SetValue(IsRunningProperty, value);
        }

        private static void OnBoxSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AgentLogoControl ctrl && e.NewValue is double size)
            {
                ctrl.BoxCornerRadius = new CornerRadius(System.Math.Max(4, size * 0.3));
            }
        }

        public AgentLogoControl()
        {
            InitializeComponent();
        }
    }
}
