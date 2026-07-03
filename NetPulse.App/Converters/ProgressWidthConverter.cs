using System.Globalization;
using System.Windows.Data;

namespace NetPulse.App.Converters;

public sealed class ProgressWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3 ||
            values[0] is not double totalWidth ||
            values[1] is not double value ||
            values[2] is not double maximum ||
            totalWidth <= 0 ||
            maximum <= 0)
        {
            return 0d;
        }

        double ratio = Math.Clamp(value / maximum, 0d, 1d);
        return totalWidth * ratio;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
