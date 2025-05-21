using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantManagerApp.Converters
{
    public class NullOrEmptyToBooleanConverterForImage : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}