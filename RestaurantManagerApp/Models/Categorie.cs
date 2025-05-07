namespace RestaurantManagerApp.Models
{
    public class Categorie
    {
        public int CategorieID { get; set; }
        public string Nume { get; set; }
        public bool EsteActiv { get; set; }

        // Constructor implicit
        public Categorie()
        {
            Nume = string.Empty; // Inițializare pentru a evita null-uri dacă e nevoie
            EsteActiv = true;
        }
    }
}