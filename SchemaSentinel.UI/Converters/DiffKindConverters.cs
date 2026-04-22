using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SchemaSentinel.Core.Comparison;

namespace SchemaSentinel.UI.Converters;

public class DiffKindToLeftBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DiffKind kind ? kind switch
        {
            DiffKind.Removed  => new SolidColorBrush(Color.FromRgb(0xFF, 0xEE, 0xEE)),
            DiffKind.Modified => new SolidColorBrush(Color.FromRgb(0xFF, 0xEE, 0xEE)),
            DiffKind.Added    => new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
            _                 => Brushes.White
        } : Brushes.White;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class DiffKindToRightBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DiffKind kind ? kind switch
        {
            DiffKind.Added    => new SolidColorBrush(Color.FromRgb(0xEE, 0xFF, 0xEE)),
            DiffKind.Modified => new SolidColorBrush(Color.FromRgb(0xEE, 0xFF, 0xEE)),
            DiffKind.Removed  => new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
            _                 => Brushes.White
        } : Brushes.White;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
