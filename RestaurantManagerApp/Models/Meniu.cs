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

        // Proprietăți de navigare
        public virtual Categorie? Categorie { get; set; }
        public virtual ICollection<PreparatInMeniu> PreparateInMeniu { get; set; } // Lista de preparate din meniu, cu cantitățile specifice

        // Proprietate calculată (nu se mapează direct la o coloană din tblMeniuri)
        // Va fi calculat în BLL/ViewModel
        public decimal PretCalculat { get; set; }

        public Meniu()
        {
            Nume = string.Empty;
            EsteActiv = true;
            PreparateInMeniu = new List<PreparatInMeniu>();
        }
    }

    // Clasa ajutătoare pentru a reprezenta un preparat într-un meniu, cu cantitatea sa specifică
    // Aceasta nu corespunde direct unei tabele separate (MeniuPreparate este tabela de legătură),
    // dar este utilă pentru a popula ICollection<PreparatInMeniu> din clasa Meniu.
    public class PreparatInMeniu
    {
        public int PreparatID { get; set; }
        public virtual Preparat? Preparat { get; set; } // Detaliile preparatului
        public string CantitateInMeniu { get; set; }
        public string UnitateMasuraCantitateInMeniu { get; set; }

        public PreparatInMeniu()
        {
            CantitateInMeniu = string.Empty;
            UnitateMasuraCantitateInMeniu = "g";
        }
    }
}