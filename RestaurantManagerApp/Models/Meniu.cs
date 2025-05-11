using System.Collections.Generic;

namespace RestaurantManagerApp.Models
{
    public class Meniu
    {
        public int MeniuID { get; set; }
        public string Nume { get; set; }
        public int CategorieID { get; set; }
        public string? Descriere { get; set; }
        public string? CaleImagine { get; set; }
        public bool EsteActiv { get; set; }

        public virtual Categorie? Categorie { get; set; }
        // Colecția va fi de tipul entității de joncțiune
        public virtual ICollection<MeniuPreparat> MeniuPreparate { get; set; }

        public decimal PretCalculat { get; set; }

        public Meniu()
        {
            Nume = string.Empty;
            EsteActiv = true;
            MeniuPreparate = new List<MeniuPreparat>(); // Inițializăm colecția
        }
    }
}