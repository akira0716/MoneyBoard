using System.Globalization;

namespace MoneyBoard.Converters
{
    public class HexToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hexCode && !string.IsNullOrEmpty(hexCode))
            {
                try
                {
                    return Color.FromArgb(hexCode);
                }
                catch
                {
                    return Colors.Gray;
                }
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return color.ToHex();
            }
            return "#808080";
        }
    }
}