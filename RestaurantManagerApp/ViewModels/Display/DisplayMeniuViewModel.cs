// ViewModels/Display/DisplayMeniuViewModel.cs
using RestaurantManagerApp.Models;
using RestaurantManagerApp.Utils;
using System.Linq;
using System.Collections.Generic;
using System; // Pentru ArgumentNullException

namespace RestaurantManagerApp.ViewModels.Display
{
    public partial class DisplayMeniuViewModel : DisplayMenuItemViewModel
    {
        private readonly Meniu _meniu;
        public decimal CalculatedNumericPrice { get; private set; }


        public override bool EsteMeniuCompus => true;
        public override int OriginalId => _meniu.MeniuID;
        public override object OriginalItem => _meniu;

        public DisplayMeniuViewModel(Meniu meniu, ApplicationSettings appSettings)
        {
            _meniu = meniu ?? throw new ArgumentNullException(nameof(meniu));
            // _appSettings = appSettings ...

            Denumire = meniu.Denumire; // Corectat la Denumire
            Descriere = meniu.Descriere;
            CaleImagine = meniu.CaleImagine;

            // Setează noua proprietate pentru meniuri
            DetaliiCantitateAfisata = "1 Meniu"; // Sau "1 Porție", sau lasă gol dacă nu e relevant

            // ... (restul logicii pentru preț, alergeni, disponibilitate) ...
            decimal subtotalComponente = 0;
            bool toateComponenteleDisponibile = true;
            if (meniu.MeniuPreparate != null)
            {
                foreach (var componenta in meniu.MeniuPreparate)
                {
                    if (componenta.Preparat != null)
                    {
                        subtotalComponente += componenta.Preparat.Pret; // Simplificare, ar trebui să țină cont de cantitatea din MeniuPreparat
                        if (!componenta.Preparat.EsteActiv || componenta.Preparat.CantitateTotalaStoc <= 0)
                        {
                            toateComponenteleDisponibile = false;
                        }
                    }
                    else { toateComponenteleDisponibile = false; }
                }
            }
            else { toateComponenteleDisponibile = false; }

            decimal discount = appSettings.MenuDiscountPercentageX; // Asigură-te că appSettings e injectat și folosit
            decimal pretFinal = subtotalComponente * (1 - (discount / 100m));
            PretAfisat = $"{pretFinal:N2} RON";
            CalculatedNumericPrice = pretFinal;

            var alergeniUnici = new HashSet<string>();
            // ... (logica alergeni) ...
            AlergeniAfisati = alergeniUnici.Any() ? string.Join(", ", alergeniUnici) : "N/A";

            EsteDisponibil = meniu.EsteActiv && toateComponenteleDisponibile;
        }
    }
}