using System;
using System.Collections.Generic;

namespace RestaurantManagerApp.Models
{
    public class Utilizator
    {
        public int UtilizatorID { get; set; }
        public string Nume { get; set; }
        public string Prenume { get; set; }
        public string Email { get; set; }
        public string? NumarTelefon { get; set; }
        public string? AdresaLivrare { get; set; }
        public string ParolaHash { get; set; }
        public string TipUtilizator { get; set; } // "Client" sau "Angajat"
        public DateTime DataInregistrare { get; set; }
        public bool EsteActiv { get; set; }

        // Proprietate de navigare
        public virtual ICollection<Comanda> Comenzi { get; set; }

        public Utilizator()
        {
            Nume = string.Empty;
            Prenume = string.Empty;
            Email = string.Empty;
            ParolaHash = string.Empty;
            TipUtilizator = "Client";
            DataInregistrare = DateTime.Now;
            EsteActiv = true;
            Comenzi = new List<Comanda>();
        }
    }
}