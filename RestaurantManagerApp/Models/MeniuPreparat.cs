using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantManagerApp.Models
{
    public class MeniuPreparat
    {
        // Chei Externe care formează Cheia Primară Compusă
        public int MeniuID { get; set; }
        public int PreparatID { get; set; }

        // Proprietăți suplimentare din tabela de joncțiune
        public string CantitateInMeniu { get; set; }
        public string UnitateMasuraCantitateInMeniu { get; set; }

        // Proprietăți de navigare către entitățile Meniu și Preparat
        public virtual Meniu Meniu { get; set; } = null!;
        public virtual Preparat Preparat { get; set; } = null!;

        public MeniuPreparat()
        {
            CantitateInMeniu = string.Empty;
            UnitateMasuraCantitateInMeniu = "g";
        }
    }
}
