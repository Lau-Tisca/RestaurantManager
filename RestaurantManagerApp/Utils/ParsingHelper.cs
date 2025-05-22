using System.Globalization;
using System.Text.RegularExpressions;

namespace RestaurantManagerApp.Utils
{
    public static class ParsingHelper
    {
        public static bool TryParseQuantityString(string? quantityString, out decimal numericValue, out string? unit)
        {
            numericValue = 0;
            unit = null;

            if (string.IsNullOrWhiteSpace(quantityString))
            {
                return false;
            }

            // Expresie regulată pentru a extrage numărul și opțional unitatea
            // Permite numere întregi sau zecimale (cu . sau ,), urmate opțional de spațiu și litere.
            // Ex: "150g", "1.5 buc", "200 ml", "1"
            Match match = Regex.Match(quantityString.Trim(), @"^(\d+([.,]\d+)?)\s*([a-zA-Z]*)?$");

            if (match.Success)
            {
                string numberPart = match.Groups[1].Value;
                if (decimal.TryParse(numberPart.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out numericValue))
                {
                    if (match.Groups[3].Success && !string.IsNullOrWhiteSpace(match.Groups[3].Value))
                    {
                        unit = match.Groups[3].Value.ToLowerInvariant();
                    }
                    else
                    {
                        unit = "buc"; // Presupunem "bucăți" dacă nu e specificată unitatea
                    }
                    return true;
                }
            }
            return false;
        }
    }
}