using CommunityToolkit.Mvvm.ComponentModel;
using RestaurantManagerApp.Models;
using RestaurantManagerApp.ViewModels.Display;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagerApp.Services
{
    // Un ViewModel simplu pentru un element din coș
    public partial class CartItemViewModel : ObservableObject
    {
        public DisplayMenuItemViewModel MenuItem { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPrice))]
        private int _quantity;
        [Required(ErrorMessage = "Cantitatea e obligatorie")]
        [Range(1, 99, ErrorMessage = "Cantitate între 1 și 99")] // Ajustează range-ul
                                                                 // [NotifyDataErrorInfo] // Dacă CartItemViewModel moștenește ObservableValidator

        public decimal UnitPrice { get; } // Prețul unitar al MenuItem la momentul adăugării
        public decimal TotalPrice => UnitPrice * Quantity;

        public CartItemViewModel(DisplayMenuItemViewModel menuItem, int quantity)
        {
            MenuItem = menuItem;
            _quantity = quantity;
            // Extrage prețul numeric din PretAfisat (necesită parsare robustă)
            // Momentan, vom presupune că MenuItem are o proprietate de preț numeric
            // Sau, mai bine, DisplayMenuItemViewModel ar trebui să expună prețul numeric.
            // Să adăugăm o proprietate numerică în DisplayMenuItemViewModel
            if (menuItem is DisplayPreparatViewModel dpvm)
            {
                UnitPrice = ((Preparat)dpvm.OriginalItem).Pret;
            }
            else if (menuItem is DisplayMeniuViewModel dmvm)
            {
                // PretCalculat din DisplayMeniuViewModel ar trebui să fie decimal, nu string
                // Să presupunem că avem acces la prețul numeric.
                // Aceasta este o simplificare, calculul prețului meniului e deja în DisplayMeniuViewModel.
                UnitPrice = dmvm.CalculatedNumericPrice; // Vom adăuga această proprietate
            }
            else
            {
                UnitPrice = 0; // Fallback
                System.Diagnostics.Debug.WriteLine("AVERTISMENT: Nu s-a putut determina prețul unitar pentru un item din coș.");
            }
        }

        partial void OnQuantityChanged(int value)
        {
            OnPropertyChanged(nameof(TotalPrice)); // Notifică UI-ul că TotalPrice s-a schimbat

            // AICI, ideal, ar trebui să notifici ShoppingCartService că o cantitate s-a schimbat,
            // pentru ca serviciul să-și actualizeze Subtotalul și TotalItems general.
            // Acest lucru se poate face printr-un eveniment ridicat de CartItemViewModel
            // la care ShoppingCartService se abonează.
            // Pentru simplitate acum, vom lăsa ca ShoppingCartService să fie notificat
            // prin metodele sale UpdateItemQuantity, RemoveItem, etc.
            // Dar dacă TextBox-ul din UI modifică direct Quantity, atunci e nevoie de acest mecanism.
            QuantityChanged?.Invoke(this, EventArgs.Empty); // Ridică un eveniment
        }

        public event EventHandler? QuantityChanged;
    }

    public interface IShoppingCartService : INotifyPropertyChanged
    {
        ObservableCollection<CartItemViewModel> CartItems { get; }
        decimal Subtotal { get; }
        int TotalItems { get; }
        void AddItemToCart(DisplayMenuItemViewModel item, int quantity = 1);
        void RemoveItemFromCart(CartItemViewModel cartItem);
        void UpdateItemQuantity(CartItemViewModel cartItem, int newQuantity);
        void ClearCart();
    }
}