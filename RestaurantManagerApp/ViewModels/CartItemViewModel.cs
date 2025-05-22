using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagerApp.Models; // Pentru Preparat (din DisplayPreparatViewModel)
using RestaurantManagerApp.Models;
using RestaurantManagerApp.ViewModels.Display; // Pentru DisplayMenuItemViewModel, DisplayPreparatViewModel, DisplayMeniuViewModel
using System;
using System.ComponentModel.DataAnnotations; // Pentru atribute de validare

namespace RestaurantManagerApp.ViewModels // Sau RestaurantManagerApp.Services
{
    public partial class CartItemViewModel : ObservableValidator // Moștenește ObservableValidator pentru validări
    {
        public DisplayMenuItemViewModel MenuItem { get; } // Produsul sau meniul din coș

        // Cantitatea - proprietate full pentru a putea adăuga logică la schimbare și validare
        [ObservableProperty] // Generează automat proprietatea publică "Quantity"
        [Required(ErrorMessage = "Cantitatea este obligatorie.")]
        [Range(1, 99, ErrorMessage = "Cantitatea trebuie să fie între 1 și 99.")]
        [NotifyDataErrorInfo] // Pentru a lega validarea la UI
        private int _quantity;

        public decimal UnitPrice { get; } // Prețul unitar al MenuItem la momentul adăugării

        // TotalPrice este o proprietate calculată
        public decimal TotalPrice => UnitPrice * Quantity;

        public decimal InitialStockAvailable { get; } // Stocul la momentul adăugării în coș

        public IRelayCommand IncrementQuantityCommand { get; }
        public IRelayCommand DecrementQuantityCommand { get; }

        public CartItemViewModel(DisplayMenuItemViewModel menuItem, int quantity)
        {
            MenuItem = menuItem ?? throw new ArgumentNullException(nameof(menuItem));
            Quantity = quantity;

            // Extrage UnitPrice
            if (menuItem is DisplayPreparatViewModel dpvm && dpvm.OriginalItem is Models.Preparat p)
            {
                UnitPrice = p.Pret;
            }
            else if (menuItem is DisplayMenuViewModel dmvm) // Numele corectat
            {
                UnitPrice = dmvm.CalculatedNumericPrice;
            }
            else { UnitPrice = 0m; }

            // Preia stocul snapshot direct din menuItem
            InitialStockAvailable = menuItem.StocDisponibilSnapshot;

            IncrementQuantityCommand = new RelayCommand(ExecuteIncrementQuantity, CanExecuteIncrementQuantity);
            DecrementQuantityCommand = new RelayCommand(ExecuteDecrementQuantity, CanExecuteDecrementQuantity);

            PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(Quantity))
                {
                    IncrementQuantityCommand.NotifyCanExecuteChanged();
                    DecrementQuantityCommand.NotifyCanExecuteChanged();
                }
            };
            ValidateAllProperties();
        }

        partial void OnQuantityChanged(int value)
        {
            // Notifică UI-ul că și TotalPrice s-a schimbat, deoarece depinde de Quantity.
            OnPropertyChanged(nameof(TotalPrice));

            // Aici este un loc bun DUPĂ ce Quantity s-a schimbat, pentru a notifica ShoppingCartService
            // dacă ShoppingCartService nu se abonează la PropertyChanged al fiecărui CartItemViewModel.
            // Dar în implementarea noastră curentă, ShoppingCartService SE ABONEAZĂ, deci nu e nevoie de un eveniment suplimentar aici.
        }

        private bool CanExecuteIncrementQuantity()
        {
            // Poți adăuga o cantitate dacă nu depășești stocul inițial disponibil
            // și un maxim per comandă (ex. 99)
            return Quantity < InitialStockAvailable && Quantity < 99; // Limită la 99 sau stoc
        }
        private void ExecuteIncrementQuantity()
        {
            if (CanExecuteIncrementQuantity()) Quantity++; // Verifică din nou CanExecute
        }

        private bool CanExecuteDecrementQuantity()
        {
            return Quantity > 1; // Poți scădea până la 1
        }
        private void ExecuteDecrementQuantity()
        {
            if (CanExecuteDecrementQuantity()) Quantity--; // Verifică din nou CanExecute
        }
    }
}