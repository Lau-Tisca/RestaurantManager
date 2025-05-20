using RestaurantManagerApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantManagerApp.Utils
{

    // Extindem clasa Utilizator pentru a avea NumeComplet, dacă nu există deja
    public static class UtilizatorExtensions
    {
        public static string NumeComplet(this Utilizator utilizator)
        {
            if (utilizator == null)
            {
                return string.Empty;
            }
            return $"{utilizator.Nume} {utilizator.Prenume}";
        }
    }
}
