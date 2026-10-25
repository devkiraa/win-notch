using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace WinNotch
{
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }
            if (value != null) // If object is not null (e.g. image source or string)
            {
                if (value is int count) return count == 0 ? Visibility.Visible : Visibility.Collapsed;
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TodoStrikeThroughConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is true)
            {
                return TextDecorations.Strikethrough;
            }
            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CompletedColorConverter : MarkupExtension, IValueConverter
    {
        private static readonly SolidColorBrush ActiveBrush = new(Color.FromRgb(0xF3, 0xF4, 0xF6));
        private static readonly SolidColorBrush CompletedBrush = new(Color.FromRgb(0x6B, 0x72, 0x80));

        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is true ? CompletedBrush : ActiveBrush;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TabToVisibilityConverter : MarkupExtension, IValueConverter
    {
        public string CurrentTab { get; set; } = string.Empty;

        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string activeTab && string.Equals(activeTab, CurrentTab, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DoubleToCornerRadiusConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return new CornerRadius(d);
            }
            return new CornerRadius(16);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TabToActiveBrushConverter : MarkupExtension, IValueConverter
    {
        public string TargetTab { get; set; } = string.Empty;
        private static readonly SolidColorBrush ActiveBg = new(Color.FromRgb(0x1C, 0x1C, 0x1E));
        private static readonly SolidColorBrush InactiveBg = new(Colors.Transparent);

        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string activeTab && string.Equals(activeTab, TargetTab, StringComparison.OrdinalIgnoreCase))
            {
                return ActiveBg;
            }
            return InactiveBg;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TabToActiveForegroundConverter : MarkupExtension, IValueConverter
    {
        public string TargetTab { get; set; } = string.Empty;
        private static readonly SolidColorBrush ActiveFg = new(Color.FromRgb(0xF5, 0xF5, 0xF7));
        private static readonly SolidColorBrush InactiveFg = new(Color.FromRgb(0x71, 0x71, 0x7A));

        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string activeTab && string.Equals(activeTab, TargetTab, StringComparison.OrdinalIgnoreCase))
            {
                return ActiveFg;
            }
            return InactiveFg;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HexToBrushConverter : MarkupExtension, IValueConverter
    {
        private static readonly BrushConverter _bc = new();
        private static readonly SolidColorBrush _fallback = new(Color.FromRgb(0x27, 0x27, 0x2A));

        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try
                {
                    var converted = _bc.ConvertFromString(hex);
                    if (converted is Brush brush)
                    {
                        return brush;
                    }
                }
                catch { }
            }
            return _fallback;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
