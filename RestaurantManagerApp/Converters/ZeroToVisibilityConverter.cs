using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RestaurantManagerApp.Converters
{
    public class ZeroToVisibilityConverter : IValueConverter
    {
        // isInverted = true  => 0 este Visible,   non-zero este Collapsed (pentru mesajul "Coș gol")
        // isInverted = false (default) => 0 este Collapsed, non-zero este Visible (pentru lista de iteme)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool makeVisibleWhenZero = parameter as string == "invert"; // Sau un nume mai bun, ex: "VisibleWhenZero"
            bool isZero = false;

            if (value is int intValue)
            {
                isZero = intValue == 0;
            }
            // Poți adăuga și suport pentru ICollection.Count aici dacă legi direct la colecție,
            // dar legarea la proprietatea ShoppingCartViewModel.TotalItems (care e int) e mai simplă.

            if (makeVisibleWhenZero) // Pentru mesajul "Coșul este gol"
            {
                return isZero ? Visibility.Visible : Visibility.Collapsed;
            }
            else // Pentru lista de iteme și secțiunea de totaluri
            {
                return isZero ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}