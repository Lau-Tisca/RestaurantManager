// ViewModels/Display/DisplayMenuItemViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace RestaurantManagerApp.ViewModels.Display
{
    public abstract partial class DisplayMenuItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _denumire = string.Empty;

        [ObservableProperty]
        private string _pretAfisat = string.Empty;

        [ObservableProperty]
        private string? _descriere;

        [ObservableProperty]
        private string? _caleImagine;

        // Vom folosi această proprietate pentru afișare în XAML
        [ObservableProperty]
        private string _detaliiCantitateAfisata = string.Empty;

        [ObservableProperty]
        private string _alergeniAfisati = string.Empty;

        [ObservableProperty]
        private bool _esteDisponibil = true;

        [ObservableProperty]
        private decimal _stocDisponibilSnapshot;

        public abstract bool EsteMeniuCompus { get; }
        public abstract int OriginalId { get; }
        public abstract object OriginalItem { get; }
    }
}