using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WbTnvedManager.Models;

namespace WbTnvedManager.Converters
{
    public class AuditStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AuditStatus status)
            {
                return status switch
                {
                    AuditStatus.MatchOk => new SolidColorBrush(Color.FromRgb(34, 197, 94)),      // Green
                    AuditStatus.UpdatedSuccess => new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Emerald
                    AuditStatus.TnvedMismatch => new SolidColorBrush(Color.FromRgb(249, 115, 22)), // Orange
                    AuditStatus.GenderMismatch => new SolidColorBrush(Color.FromRgb(234, 179, 8)), // Yellow/Amber
                    AuditStatus.BothMismatch => new SolidColorBrush(Color.FromRgb(239, 68, 68)),   // Red
                    AuditStatus.UpdateFailed => new SolidColorBrush(Color.FromRgb(220, 38, 38)),   // Dark Red
                    AuditStatus.NoMatrixMatch => new SolidColorBrush(Color.FromRgb(156, 163, 175)),// Gray
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw exotic();
        private Exception exotic() => new NotImplementedException();
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool flag && flag;
            if (Invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
            {
                bool b = v == Visibility.Visible;
                return Invert ? !b : b;
            }
            return false;
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }
    }
}
