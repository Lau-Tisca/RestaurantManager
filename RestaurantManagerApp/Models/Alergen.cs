using System.Collections.Generic;

namespace RestaurantManagerApp.Models
{
    public class Alergen
    {
        public int AlergenID { get; set; }
        public string Nume { get; set; }
        public bool EsteActiv { get; set; }

        public virtual ICollection<Preparat> Preparate { get; set; }

        public Alergen()
        {
            Nume = string.Empty;
            EsteActiv = true;
            Preparate = new List<Preparat>();
        }
    }
}