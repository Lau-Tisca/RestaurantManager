using RestaurantManagerApp.Models;
using System.Linq;

namespace RestaurantManagerApp.ViewModels.Display
{
    public partial class DisplayPreparatViewModel : DisplayMenuItemViewModel
    {
        private readonly Preparat _preparat;
        public override bool EsteMeniuCompus => false;
        public override int OriginalId => _preparat.PreparatID;
        public override object OriginalItem => _preparat;

        public DisplayPreparatViewModel(Preparat preparat)
        {
            _preparat = preparat ?? throw new ArgumentNullException(nameof(preparat));

            Denumire = preparat.Denumire;
            PretAfisat = $"{preparat.Pret:N2} RON"; // N2 formatează cu 2 zecimale
            Descriere = preparat.Descriere;
            CaleImagine = preparat.CaleImagine; // Ar trebui să fie o cale relativă la un folder de imagini
            CantitatePortie = preparat.CantitatePortie;

            if (preparat.Alergeni != null && preparat.Alergeni.Any())
            {
                AlergeniAfisati = string.Join(", ", preparat.Alergeni.Select(a => a.Nume));
            }
            else
            {
                AlergeniAfisati = "N/A"; // Sau "Fără alergeni specificați"
            }

            // Logica de disponibilitate:
            // De exemplu, dacă stocul e numeric și avem un prag.
            // Sau dacă e un flag direct pe preparat.
            // Momentan, vom presupune că dacă e activ, e disponibil.
            // Stocul va fi verificat la adăugarea în coș.
            EsteDisponibil = preparat.EsteActiv && preparat.CantitateTotalaStoc > 0; // Exemplu simplu
        }
    }
}