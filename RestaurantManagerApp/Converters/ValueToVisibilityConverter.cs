using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RestaurantManagerApp.Converters
{
    public class ValueToVisibilityConverter : IValueConverter
    {
        // Convertește o valoare numerică în Visibility.
        // Dacă valoarea este egală cu ConverterParameter (default 0), returnează Collapsed. Altfel, Visible.
        // Dacă parametrul este "invert", logica se inversează.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal threshold = 0;
            if (parameter != null && decimal.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal paramThreshold))
            {
                threshold = paramThreshold;
            }

            bool shouldBeVisible;
            if (value is decimal decValue)
            {
                shouldBeVisible = decValue != threshold;
            }
            else if (value is int intValue)
            {
                shouldBeVisible = intValue != (int)threshold;
            }
            // Adaugă alte tipuri numerice dacă e necesar
            else
            {
                shouldBeVisible = false; // Sau true, depinde de fallback
            }

            // Logica de inversare, deși pentru acest caz specific nu am folosit "invert" în XAML
            // bool isInverted = (parameter as string)?.ToLowerInvariant().Contains("invert") ?? false;
            // if (isInverted) shouldBeVisible = !shouldBeVisible;

            return shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}