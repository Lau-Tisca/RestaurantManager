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
        public virtual Meniu Meniu { get; set; }
        public virtual Preparat Preparat { get; set; }

        public MeniuPreparat()
        {
            CantitateInMeniu = string.Empty;
            UnitateMasuraCantitateInMeniu = "g";
            // Asigură-te că Meniu și Preparat nu sunt null aici dacă nu sunt încărcate
            // Acest constructor este pentru crearea de noi instanțe, nu pentru cele încărcate de EF
            Meniu = null!; // Folosim "null-forgiving operator" dacă avem nullable reference types activat și știm că va fi setat
            Preparat = null!;
        }
    }
}
