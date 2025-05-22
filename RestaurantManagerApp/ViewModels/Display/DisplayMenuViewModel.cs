// ViewModels/Display/DisplayMeniuViewModel.cs
using RestaurantManagerApp.Models;
using RestaurantManagerApp.Utils;
using System; // Pentru ArgumentNullException
using System.Collections.Generic;
using System.Linq;
using System.Text; // Pentru StringBuilder
using System.Globalization; // Pentru NumberStyles

namespace RestaurantManagerApp.ViewModels.Display
{
    public partial class DisplayMenuViewModel : DisplayMenuItemViewModel
    {
        private readonly Meniu _meniu;

        private readonly ApplicationSettings _appSettings;
        public decimal CalculatedNumericPrice { get; private set; }
        public decimal StocDisponibilLaMomentulAfisarii { get; private set; }
        public override bool EsteMeniuCompus => true;
        public override int OriginalId => _meniu.MeniuID;
        public override object OriginalItem => _meniu;

        public DisplayMenuViewModel(Meniu meniu, ApplicationSettings appSettings)
        {
            _meniu = meniu ?? throw new ArgumentNullException(nameof(meniu));
            // _appSettings = appSettings ...

            Denumire = meniu.Denumire; // Corectat la Denumire
            Descriere = meniu.Descriere;
            CaleImagine = meniu.CaleImagine;

            // --- Populează DetaliiCantitateAfisata (sau TitluPrincipalAfisat) cu componentele ---
            StringBuilder sbDetalii = new StringBuilder();
            if (_meniu.MeniuPreparate != null && _meniu.MeniuPreparate.Any())
            {
                foreach (var componenta in _meniu.MeniuPreparate)
                {
                    if (componenta.Preparat != null) // Asigură-te că Preparatul e încărcat
                    {
                        if (sbDetalii.Length > 0)
                        {
                            sbDetalii.Append(" | ");
                        }

                        // Extrage cantitatea numerică din MeniuPreparat.CantitateInMeniu
                        // Presupunem că MeniuPreparat.CantitateInMeniu stochează numărul de "porții standard" ale preparatului component.
                        // De ex., dacă MeniuPreparat.CantitateInMeniu este "2" și Preparat.CantitatePortie este "100g",
                        // atunci afișăm "2x Nume Preparat (100g)".

                        string cantitateComponentaText = componenta.CantitateInMeniu; // Ex: "2", "1.5"
                        string unitateComponenta = componenta.UnitateMasuraCantitateInMeniu; // Ex: "buc"
                        string gramajPerUnitateComponenta = componenta.Preparat.CantitatePortie; // Ex: "100g", "1 bucată"
                        string descrierePortieComponenta = $"{componenta.Preparat.CantitatePortie}{componenta.Preparat.UnitateMasuraStoc}"; // Ex: "100g"

                        // Formatare: "2x Nume Preparat (100g)"
                        // Dacă CantitateInMeniu este doar un număr, putem adăuga "x"
                        // Dacă CantitateInMeniu este deja "2 buc", poate vrem să o lăsăm așa.
                        // Să facem o încercare de a detecta dacă e doar numeric.

                        string prefixCantitate = cantitateComponentaText;
                        if (decimal.TryParse(cantitateComponentaText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                        {
                            // Dacă este un număr simplu, adăugăm "x"
                            // și putem folosi UnitateMasuraCantitateInMeniu dacă e relevantă (ex: "2x" sau "2 buc x")
                            // Pentru exemplul tău "2x Nume (100g)", pare că vrei doar numărul.
                            prefixCantitate = $"{cantitateComponentaText}x";
                        }
                        // Altfel, dacă CantitateInMeniu este "2 buc", prefixCantitate va fi "2 buc"

                        sbDetalii.Append($"{prefixCantitate} {componenta.Preparat.Denumire} ({descrierePortieComponenta})");
                    }
                }
                // Setează proprietatea din clasa de bază
                DetaliiCantitateAfisata = sbDetalii.ToString();
                // Sau dacă ai redenumit-o: TitluPrincipalAfisat = sbDetalii.ToString();
            }
            else
            {
                DetaliiCantitateAfisata = "Meniu (fără componente specificate)";
                // Sau TitluPrincipalAfisat = ...
            }

            if (string.IsNullOrWhiteSpace(DetaliiCantitateAfisata) && (_meniu.MeniuPreparate == null || !_meniu.MeniuPreparate.Any()))
            {
                // Dacă nu sunt componente, poate afișăm doar numele meniului în TitluPrincipalAfisat
                // și DetaliiCantitateAfisata rămâne gol sau "1 Porție".
                // Depinde ce vrei să arate proprietatea din clasa de bază.
                // Să presupunem că DetaliiCantitateAfisata este specific pentru conținut.
                // TitluPrincipalAfisat ar fi doar Denumire pentru meniuri dacă nu vrem stoc.
                // TitluPrincipalAfisat = Denumire; // Pentru meniuri, dacă nu vrem altceva
            }
            // Dacă string-ul rezultat e gol, poți seta un default
            if (string.IsNullOrWhiteSpace(DetaliiCantitateAfisata))
            {
                DetaliiCantitateAfisata = "Detalii meniu indisponibile";
            }

            decimal subtotalComponente = 0;
            bool toateComponenteleDisponibile = true;
            if (meniu.MeniuPreparate != null)
            {
                foreach (var componenta in meniu.MeniuPreparate)
                {
                    if (componenta.Preparat != null)
                    {
                        if (ParsingHelper.TryParseQuantityString(componenta.CantitateInMeniu, out decimal cantitateNumericaComponenta, out _))
                        {
                            subtotalComponente += componenta.Preparat.Pret * cantitateNumericaComponenta;
                        }
                        else
                        {
                            // Fallback dacă nu se poate parsa cantitatea, poate presupunem 1
                            subtotalComponente += componenta.Preparat.Pret;
                            System.Diagnostics.Debug.WriteLine($"AVERTISMENT: Nu s-a putut parsa CantitateInMeniu '{componenta.CantitateInMeniu}' pentru calcul preț componenta '{componenta.Preparat.Denumire}'");
                        }

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

            decimal stocCalculatPentruMeniu = decimal.MaxValue; // Pornim cu o valoare mare
            bool toateComponenteleAuStocSiSuntActive = true;

            if (_meniu.MeniuPreparate != null && _meniu.MeniuPreparate.Any())
            {
                foreach (var componenta in _meniu.MeniuPreparate)
                {
                    if (componenta.Preparat != null && componenta.Preparat.EsteActiv)
                    {
                        // Parsează cantitatea necesară din componentă pentru UN meniu
                        if (ParsingHelper.TryParseQuantityString(componenta.CantitateInMeniu, out decimal cantitateNecesaraComponenta, out string? unitateComponenta))
                        {
                            if (cantitateNecesaraComponenta <= 0) // Cantitate invalidă în definiția meniului
                            {
                                toateComponenteleAuStocSiSuntActive = false;
                                System.Diagnostics.Debug.WriteLine($"AVERTISMENT: Componenta '{componenta.Preparat.Denumire}' din meniul '{_meniu.Denumire}' are cantitate necesară invalidă: {componenta.CantitateInMeniu}");
                                break; // O componentă invalidă face tot meniul indisponibil din perspectiva stocului
                            }

                            // Presupunem că unitatea de măsură a componentei în meniu și unitatea de stoc a preparatului sunt compatibile
                            // sau că 'cantitateNecesaraComponenta' este deja în unitatea de stoc a preparatului.
                            // Aceasta este o simplificare. Într-un sistem real, ar trebui conversie de unități.
                            if (componenta.Preparat.CantitateTotalaStoc < cantitateNecesaraComponenta) // Stoc insuficient pentru o singură porție de meniu
                            {
                                toateComponenteleAuStocSiSuntActive = false;
                                System.Diagnostics.Debug.WriteLine($"AVERTISMENT: Stoc insuficient pentru componenta '{componenta.Preparat.Denumire}' (stoc: {componenta.Preparat.CantitateTotalaStoc}, necesar pt 1 meniu: {cantitateNecesaraComponenta}) din meniul '{_meniu.Denumire}'.");
                                break;
                            }

                            // Calculează câte meniuri complete se pot face pe baza acestei componente
                            decimal meniuriPosibileCuAceastaComponenta = Math.Floor(componenta.Preparat.CantitateTotalaStoc / cantitateNecesaraComponenta);
                            stocCalculatPentruMeniu = Math.Min(stocCalculatPentruMeniu, meniuriPosibileCuAceastaComponenta);
                        }
                        else
                        {
                            // Nu s-a putut parsa cantitatea componentei
                            toateComponenteleAuStocSiSuntActive = false;
                            System.Diagnostics.Debug.WriteLine($"AVERTISMENT: Nu s-a putut parsa CantitateInMeniu ('{componenta.CantitateInMeniu}') pentru componenta '{componenta.Preparat.Denumire}' din meniul '{_meniu.Denumire}'.");
                            break;
                        }
                    }
                    else // Preparatul componentei e null sau inactiv
                    {
                        toateComponenteleAuStocSiSuntActive = false;
                        System.Diagnostics.Debug.WriteLine($"AVERTISMENT: Componenta din meniul '{_meniu.Denumire}' este null sau inactivă.");
                        break;
                    }
                }
            }
            else // Meniu fără componente definite
            {
                toateComponenteleAuStocSiSuntActive = false;
                System.Diagnostics.Debug.WriteLine($"AVERTISMENT: Meniul '{_meniu.Denumire}' nu are componente definite.");
            }

            if (!toateComponenteleAuStocSiSuntActive || stocCalculatPentruMeniu == decimal.MaxValue)
            {
                StocDisponibilSnapshot = 0; // Dacă o componentă e indisponibilă sau meniul nu are componente, stocul e 0
            }
            else
            {
                StocDisponibilSnapshot = stocCalculatPentruMeniu;
            }

            EsteDisponibil = _meniu.EsteActiv && toateComponenteleAuStocSiSuntActive && StocDisponibilSnapshot > 0;
            System.Diagnostics.Debug.WriteLine($"Pentru meniul '{Denumire}', StocDisponibilSnapshot calculat: {StocDisponibilSnapshot}, EsteDisponibil: {EsteDisponibil}");
        }
    }
}