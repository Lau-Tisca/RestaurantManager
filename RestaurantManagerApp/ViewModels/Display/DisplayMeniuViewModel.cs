using RestaurantManagerApp.Models;
using RestaurantManagerApp.Utils; // Pentru ApplicationSettings
using System.Linq;
using System.Collections.Generic; // Pentru HashSet

namespace RestaurantManagerApp.ViewModels.Display
{
    public partial class DisplayMeniuViewModel : DisplayMenuItemViewModel
    {
        private readonly Meniu _meniu;
        private readonly ApplicationSettings _appSettings;
        public override bool EsteMeniuCompus => true;
        public override int OriginalId => _meniu.MeniuID;
        public override object OriginalItem => _meniu;

        public DisplayMeniuViewModel(Meniu meniu, ApplicationSettings appSettings)
        {
            _meniu = meniu ?? throw new ArgumentNullException(nameof(meniu));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

            Denumire = meniu.Denumire;
            Descriere = meniu.Descriere;
            CaleImagine = meniu.CaleImagine;
            CantitatePortie = "1 porție"; // Sau alt text relevant pentru un meniu compus

            // Calcul Preț
            decimal subtotalComponente = 0;
            bool toateComponenteleDisponibile = true;
            if (meniu.MeniuPreparate != null)
            {
                foreach (var componenta in meniu.MeniuPreparate)
                {
                    if (componenta.Preparat != null) // Asigură-te că Preparatul este încărcat
                    {
                        // Aici logica de calcul al prețului pe baza cantității din MeniuPreparat
                        // Momentan, presupunem că prețul din componenta.Preparat este pentru cantitatea din meniu
                        // sau că MeniuPreparat.CantitateInMeniu este un multiplicator numeric.
                        // Pentru simplitate, vom aduna prețurile preparatelor.
                        // Într-un scenariu real, CantitateInMeniu ar trebui parsat.
                        subtotalComponente += componenta.Preparat.Pret; // Simplificare
                        if (!componenta.Preparat.EsteActiv || componenta.Preparat.CantitateTotalaStoc <= 0)
                        {
                            toateComponenteleDisponibile = false;
                        }
                    }
                    else
                    {
                        toateComponenteleDisponibile = false; // Dacă o componentă lipsește, meniul e indisponibil
                    }
                }
            }
            else
            {
                toateComponenteleDisponibile = false; // Meniu fără componente
            }

            decimal discount = _appSettings.MenuDiscountPercentageX;
            decimal pretFinal = subtotalComponente * (1 - (discount / 100m));
            PretAfisat = $"{pretFinal:N2} RON";

            // Calcul Alergeni (uniunea alergenilor din toate componentele)
            var alergeniUnici = new HashSet<string>();
            if (meniu.MeniuPreparate != null)
            {
                foreach (var componenta in meniu.MeniuPreparate)
                {
                    if (componenta.Preparat?.Alergeni != null)
                    {
                        foreach (var alergen in componenta.Preparat.Alergeni)
                        {
                            alergeniUnici.Add(alergen.Nume);
                        }
                    }
                }
            }
            AlergeniAfisati = alergeniUnici.Any() ? string.Join(", ", alergeniUnici) : "N/A";

            EsteDisponibil = meniu.EsteActiv && toateComponenteleDisponibile;
        }
    }
}