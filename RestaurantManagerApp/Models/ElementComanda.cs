namespace RestaurantManagerApp.Models
{
    public class ElementComanda
    {
        public int ElementComandaID { get; set; }
        public int ComandaID { get; set; }
        public int? PreparatID { get; set; } // Nullable, deoarece poate fi un meniu
        public int? MeniuID { get; set; }    // Nullable, deoarece poate fi un preparat
        public int Cantitate { get; set; }
        public decimal PretUnitarLaMomentulComenzii { get; set; }
        public decimal SubtotalElement { get; set; }

        // Proprietăți de navigare (opționale, dar utile)
        public virtual Preparat? Preparat { get; set; }
        public virtual Meniu? Meniu { get; set; }
        // public virtual Comanda Comanda { get; set; } // Poate crea referințe ciclice dacă nu e gestionat atent

        public ElementComanda()
        {
        }
    }
}