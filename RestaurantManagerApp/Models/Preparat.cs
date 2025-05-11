using System.Collections.Generic;

namespace RestaurantManagerApp.Models
{
    public class Preparat
    {
        public int PreparatID { get; set; }
        public string Denumire { get; set; }
        public decimal Pret { get; set; }
        public string CantitatePortie { get; set; }
        public decimal CantitateTotalaStoc { get; set; }
        public string UnitateMasuraStoc { get; set; }
        public int CategorieID { get; set; }
        public string? Descriere { get; set; }
        public string? CaleImagine { get; set; }
        public bool EsteActiv { get; set; }

        // Proprietăți de navigare (pentru a încărca datele relaționate mai ușor)
        public virtual Categorie? Categorie { get; set; } // Obiectul Categorie asociat
        public virtual ICollection<Alergen> Alergeni { get; set; } // Lista de Alergeni ai preparatului

        public virtual ICollection<MeniuPreparat> MeniuPreparate { get; set; }

        public Preparat()
        {
            Denumire = string.Empty;
            CantitatePortie = string.Empty;
            UnitateMasuraStoc = "g";
            EsteActiv = true;
            Alergeni = new List<Alergen>(); // Inițializăm lista pentru a evita NullReferenceException
            MeniuPreparate = new List<MeniuPreparat>();
        }
    }
}