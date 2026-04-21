using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SchemaSentinel.Core.Comparison;

namespace SchemaSentinel.UI.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DiffStatus status
            ? status switch
            {
                DiffStatus.Identical => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                DiffStatus.Changed => new SolidColorBrush(Color.FromRgb(230, 126, 34)),
                DiffStatus.MissingInSource => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                DiffStatus.MissingInTarget => new SolidColorBrush(Color.FromRgb(192, 57, 43)),
                _ => Brushes.Gray
            }
            : Brushes.Gray;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class StatusToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DiffStatus status
            ? status switch
            {
                DiffStatus.Identical => new SolidColorBrush(Color.FromRgb(234, 250, 241)),
                DiffStatus.Changed => new SolidColorBrush(Color.FromRgb(254, 249, 231)),
                DiffStatus.MissingInSource => new SolidColorBrush(Color.FromRgb(253, 237, 236)),
                DiffStatus.MissingInTarget => new SolidColorBrush(Color.FromRgb(253, 237, 236)),
                _ => Brushes.Transparent
            }
            : Brushes.Transparent;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
