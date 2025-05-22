using System;
using System.Globalization;
using System.IO; // Pentru Path
using System.Windows.Data;
using System.Windows.Media.Imaging; // Pentru BitmapImage

namespace RestaurantManagerApp.Converters
{
    public class PathToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? path = value as string;
            string placeholderPath = "pack://application:,,,/Resources/default_image_placeholder.png"; // Default placeholder

            if (parameter is string placeholderOverride) // Permite specificarea unui placeholder diferit din XAML
            {
                placeholderPath = placeholderOverride;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return new BitmapImage(new Uri(placeholderPath));
            }

            try
            {
                // Presupunem că 'path' este relativ la directorul de execuție dacă nu e absolut
                // sau dacă este un URI pack://
                Uri imageUri;
                if (Path.IsPathRooted(path) || path.StartsWith("pack://"))
                {
                    imageUri = new Uri(path, UriKind.Absolute);
                }
                else // Cale relativă la directorul de execuție
                {
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string absolutePath = Path.GetFullPath(Path.Combine(baseDirectory, path));
                    if (File.Exists(absolutePath))
                    {
                        imageUri = new Uri(absolutePath, UriKind.Absolute);
                    }
                    else
                    {
                        // Fișierul nu există la calea relativă, folosim placeholder
                        System.Diagnostics.Debug.WriteLine($"Imagine negăsită la calea relativă (convertită în absolut): {absolutePath}. Se folosește placeholder.");
                        return new BitmapImage(new Uri(placeholderPath));
                    }
                }

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = imageUri;
                image.CacheOption = BitmapCacheOption.OnLoad; // Încarcă imaginea imediat
                image.EndInit();
                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Eroare la încărcarea imaginii din calea '{path}': {ex.Message}. Se folosește placeholder.");
                return new BitmapImage(new Uri(placeholderPath)); // Fallback la placeholder în caz de eroare
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}