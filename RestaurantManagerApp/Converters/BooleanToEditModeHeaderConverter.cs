using System;
using System.Globalization;
using System.Windows.Data; // Necesar pentru IValueConverter

namespace RestaurantManagerApp.Converters
{
    public class BooleanToEditModeHeaderConverter : IValueConverter
    {
        public string TrueHeader { get; set; } = "Editează"; // Valori default
        public string FalseHeader { get; set; } = "Adaugă Nou";
        public string FallbackHeader { get; set; } = "Formular";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEditMode)
            {
                return isEditMode ? TrueHeader : FalseHeader;
            }
            return FallbackHeader;
        }

        //public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        //{
        //    if (value is bool isEditMode)
        //    {
        //        return isEditMode ? "Editează Categorie" : "Adaugă Categorie Nouă";
        //    }
        //    return "Formular Categorie"; // Fallback
        //}

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // De obicei nu e necesar pentru convertoare one-way
            throw new NotImplementedException();
        }
    }
}