using System;
using System.Collections.Generic;

namespace RestaurantManagerApp.Models
{
    public class Comanda
    {
        public int ComandaID { get; set; }
        public int UtilizatorID { get; set; }
        public DateTime DataComanda { get; set; }
        public string CodUnic { get; set; }
        public string StareComanda { get; set; }
        public string AdresaLivrareComanda { get; set; }
        public string? NumarTelefonComanda { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAplicat { get; set; }
        public decimal CostTransport { get; set; }
        public decimal TotalGeneral { get; set; }
        public DateTime? OraEstimataLivrare { get; set; }
        public string? Observatii { get; set; }

        // Proprietăți de navigare
        public virtual Utilizator? Utilizator { get; set; }
        public virtual ICollection<ElementComanda> ElementeComanda { get; set; }

        public Comanda()
        {
            CodUnic = string.Empty;
            StareComanda = "Inregistrata";
            AdresaLivrareComanda = string.Empty;
            DataComanda = DateTime.Now;
            ElementeComanda = new List<ElementComanda>();
        }
    }
}