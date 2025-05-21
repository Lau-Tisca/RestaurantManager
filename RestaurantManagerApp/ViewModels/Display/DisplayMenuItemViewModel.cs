using CommunityToolkit.Mvvm.ComponentModel;
using RestaurantManagerApp.Models; // Pentru a putea stoca obiectul original
using System.Collections.Generic;

namespace RestaurantManagerApp.ViewModels.Display
{
    public abstract partial class DisplayMenuItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _denumire = string.Empty;

        [ObservableProperty]
        private string _pretAfisat = string.Empty; // Formatat ca "XX.YY RON"

        [ObservableProperty]
        private string? _descriere;

        [ObservableProperty]
        private string? _caleImagine;

        [ObservableProperty]
        private string _cantitatePortie = string.Empty; // Ex: "300g", "1 buc"

        [ObservableProperty]
        private string _alergeniAfisati = string.Empty; // Ex: "Gluten, Lactoză" sau "Fără alergeni cunoscuți"

        [ObservableProperty]
        private bool _esteDisponibil = true;

        public abstract bool EsteMeniuCompus { get; }
        public abstract int OriginalId { get; } // Pentru identificare
        public abstract object OriginalItem { get; } // Pentru referința la modelul original
    }
}