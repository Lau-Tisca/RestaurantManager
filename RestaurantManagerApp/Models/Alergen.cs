namespace RestaurantManagerApp.Models
{
    public class Alergen
    {
        public int AlergenID { get; set; }
        public string Nume { get; set; }
        public bool EsteActiv { get; set; }

        public Alergen()
        {
            Nume = string.Empty;
            EsteActiv = true;
        }
    }
}