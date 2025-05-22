// ViewModels/Display/DisplayPreparatViewModel.cs
using RestaurantManagerApp.Models;
using System.Linq;
using System.Text.RegularExpressions;
using System; // Pentru StringComparison

namespace RestaurantManagerApp.ViewModels.Display
{
    public partial class DisplayPreparatViewModel : DisplayMenuItemViewModel
    {
        private readonly Preparat _preparat;
        public decimal StocDisponibilLaMomentulAfisarii { get; private set; }
        public override bool EsteMeniuCompus => false;
        public override int OriginalId => _preparat.PreparatID;
        public override object OriginalItem => _preparat;

        public DisplayPreparatViewModel(Preparat preparat)
        {
            _preparat = preparat ?? throw new ArgumentNullException(nameof(preparat));

            Denumire = preparat.Denumire;
            PretAfisat = $"{preparat.Pret:N2} RON";
            Descriere = preparat.Descriere;
            CaleImagine = preparat.CaleImagine;
            DetaliiCantitateAfisata = ExtractGramsForDisplay(preparat.CantitatePortie, preparat.UnitateMasuraStoc);
            StocDisponibilSnapshot = preparat.CantitateTotalaStoc;

            if (preparat.Alergeni != null && preparat.Alergeni.Any())
            {
                AlergeniAfisati = string.Join(", ", preparat.Alergeni.Select(a => a.Nume));
            }
            else
            {
                AlergeniAfisati = "N/A";
            }
            EsteDisponibil = preparat.EsteActiv && StocDisponibilSnapshot > 0;
        }

        private string ExtractGramsForDisplay(string cantitatePortieOriginala, string unitateStoc)
        {
            if (string.IsNullOrWhiteSpace(cantitatePortieOriginala)) return "N/A";

            // Încercare de a găsi explicit "g" sau "grame" etc.
            Match gMatch = Regex.Match(cantitatePortieOriginala, @"(\d+[\.,]?\d*)\s*(g|gr|gram|grame)\b", RegexOptions.IgnoreCase);
            if (gMatch.Success)
            {
                return $"{gMatch.Groups[1].Value.Trim()}g"; // Curăță spațiile din număr
            }

            // Dacă unitatea de stoc principală este "g" și CantitatePortie conține un număr, presupunem că sunt grame
            if (unitateStoc.Equals("g", StringComparison.OrdinalIgnoreCase))
            {
                Match numMatch = Regex.Match(cantitatePortieOriginala, @"\d+[\.,]?\d*");
                if (numMatch.Success)
                {
                    return $"{numMatch.Value.Trim()}g";
                }
            }

            // Fallback: dacă nu sunt identificate grame, afișează cantitatea originală a porției
            // sau "N/A" dacă vrei să afișezi gramajul doar dacă e explicit "g"
            // return "N/A"; // Dacă vrei să arăți doar dacă sunt grame
            return cantitatePortieOriginala; // Afișează ce era în CantitatePortie
        }
    }
}