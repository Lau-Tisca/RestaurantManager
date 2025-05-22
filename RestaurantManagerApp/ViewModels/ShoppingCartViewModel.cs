using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagerApp.Services; // Pentru IShoppingCartService și CartItemViewModel
using RestaurantManagerApp.Utils;
using RestaurantManagerApp.ViewModels.Display; // Pentru DisplayMenuItemViewModel
using System;             // Pentru ArgumentNullException
using System.Collections.ObjectModel;
using System.ComponentModel; // Pentru DesignerProperties
using System.Linq;
using System.Threading.Tasks; // Deși s-ar putea să nu avem operații async direct aici
using System.Windows;     // Pentru MessageBox (dacă e nevoie de feedback)

namespace RestaurantManagerApp.ViewModels
{
    public partial class ShoppingCartViewModel : ObservableObject, IAsyncInitializableVM // Implementează IAsyncInitializableVM dacă are nevoie de încărcare la navigare
    {
        private readonly IShoppingCartService _shoppingCartService;
        // Acțiune pentru a naviga la pasul de checkout
        public Action? OnProceedToCheckout { get; set; }


        // Expunem direct colecția și proprietățile din serviciu.
        // ViewModel-ul acționează ca un passthrough și poate adăuga logică UI specifică.
        public ObservableCollection<CartItemViewModel> CartItems => _shoppingCartService.CartItems;
        public decimal Subtotal => _shoppingCartService.Subtotal;
        public int TotalItems => _shoppingCartService.TotalItems;

        // Comenzi pentru interacțiunea cu coșul
        // Pentru UpdateQuantity, vom avea nevoie de un mod de a pasa noua cantitate.
        // O variantă este ca TextBox-ul din View să actualizeze direct Quantity pe CartItemViewModel
        // și apoi să avem un buton "Actualizează Coș" sau să se actualizeze la pierderea focusului.
        // Sau, o comandă care ia CartItemViewModel și o nouă cantitate (mai complex de legat din XAML).
        // Momentan, vom presupune că modificarea cantității se face direct pe CartItemViewModel.
        public IRelayCommand<CartItemViewModel> RemoveItemCommand { get; }
        public IRelayCommand ProceedToCheckoutCommand { get; }
        public IRelayCommand ClearCartCommand { get; }


        // Constructor pentru Design Time
        public ShoppingCartViewModel()
        {
            System.Diagnostics.Debug.WriteLine("ShoppingCartViewModel DesignTime Constructor Called");
            // Creează un mock ShoppingCartService pentru design time
            var mockCartService = new MockShoppingCartServiceForCartVM();
            var dummyPreparat = new DisplayPreparatViewModel(new Models.Preparat { PreparatID = 1, Denumire = "Produs Coș Design 1", Pret = 25.50m, CantitatePortie = "1 buc", EsteActiv = true, CantitateTotalaStoc = 10 });
            mockCartService.AddItemToCart(dummyPreparat, 2);
            var dummyMeniu = new DisplayMeniuViewModel(new Models.Meniu { MeniuID = 1, Denumire = "Meniu Coș Design", EsteActiv = true }, new ApplicationSettings { MenuDiscountPercentageX = 10 });
            mockCartService.AddItemToCart(dummyMeniu, 1);
            _shoppingCartService = mockCartService;

            // Trebuie să notificăm schimbările pentru proprietățile calculate dacă mock-ul nu o face
            OnPropertyChanged(nameof(CartItems));
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(TotalItems));

            RemoveItemCommand = new RelayCommand<CartItemViewModel>(item =>
            {
                if (item != null)
                {
                    // Simulează ștergerea din mock
                    (CartItems as ObservableCollection<CartItemViewModel>)?.Remove(item);
                    OnPropertyChanged(nameof(Subtotal));
                    OnPropertyChanged(nameof(TotalItems));
                }
            }, item => item != null);
            ProceedToCheckoutCommand = new RelayCommand(() => { MessageBox.Show("Design: Mergi la Checkout"); }, () => CartItems.Any());
            ClearCartCommand = new RelayCommand(() =>
            {
                CartItems.Clear();
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(TotalItems));
            }, () => CartItems.Any());
        }
        // Mock simplu pentru IShoppingCartService pentru constructorul de design time
        private class MockShoppingCartServiceForCartVM : IShoppingCartService
        {
            public ObservableCollection<CartItemViewModel> CartItems { get; } = new();
            public decimal Subtotal => CartItems.Sum(i => i.TotalPrice);
            public int TotalItems => CartItems.Sum(i => i.Quantity);
            public event PropertyChangedEventHandler? PropertyChanged; // Nu-l implementăm complet aici
            public void AddItemToCart(DisplayMenuItemViewModel item, int quantity = 1) => CartItems.Add(new CartItemViewModel(item, quantity));
            public void ClearCart() => CartItems.Clear();
            public void RemoveItemFromCart(CartItemViewModel cartItem) => CartItems.Remove(cartItem);
            public void UpdateItemQuantity(CartItemViewModel cartItem, int newQuantity) { if (newQuantity > 0) cartItem.Quantity = newQuantity; else CartItems.Remove(cartItem); }
            protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        // Constructor pentru Runtime
        public ShoppingCartViewModel(IShoppingCartService shoppingCartService)
        {
            System.Diagnostics.Debug.WriteLine("ShoppingCartViewModel Runtime Constructor Called");
            _shoppingCartService = shoppingCartService ?? throw new ArgumentNullException(nameof(shoppingCartService));

            // Ne abonăm la PropertyChanged al serviciului pentru a reflecta schimbările
            _shoppingCartService.PropertyChanged += ShoppingCartService_PropertyChanged;

            RemoveItemCommand = new RelayCommand<CartItemViewModel>(ExecuteRemoveItem, CanExecuteRemoveItem);
            ProceedToCheckoutCommand = new RelayCommand(ExecuteProceedToCheckout, CanExecuteProceedToCheckout);
            ClearCartCommand = new RelayCommand(ExecuteClearCart, CanExecuteClearCart);
        }

        private void ShoppingCartService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Când o proprietate din serviciu se schimbă (Subtotal, TotalItems),
            // notificăm UI-ul că proprietățile corespunzătoare din acest ViewModel s-au schimbat.
            // Dacă CartItems din serviciu este ObservableCollection și ViewModel-ul expune direct referința,
            // UI-ul va vedea schimbările la iteme. Dar pentru Subtotal și TotalItems, e nevoie de notificare.
            if (e.PropertyName == nameof(IShoppingCartService.Subtotal))
            {
                OnPropertyChanged(nameof(Subtotal));
                ProceedToCheckoutCommand.NotifyCanExecuteChanged(); // Starea coșului poate afecta checkout-ul
                ClearCartCommand.NotifyCanExecuteChanged();
            }
            else if (e.PropertyName == nameof(IShoppingCartService.TotalItems))
            {
                OnPropertyChanged(nameof(TotalItems));
            }

            ProceedToCheckoutCommand.NotifyCanExecuteChanged();
            ClearCartCommand.NotifyCanExecuteChanged();
            // Dacă `CartItems` în sine (referința la colecție) s-ar schimba în serviciu, am notifica și asta.
            // Dar de obicei doar conținutul ei se schimbă.
        }

        // --- Implementări Comenzi ---
        private bool CanExecuteRemoveItem(CartItemViewModel? item) => item != null && CartItems.Contains(item);
        private void ExecuteRemoveItem(CartItemViewModel? item)
        {
            if (item != null) _shoppingCartService.RemoveItemFromCart(item);
        }

        private bool CanExecuteProceedToCheckout() => CartItems.Any();
        private void ExecuteProceedToCheckout()
        {
            System.Diagnostics.Debug.WriteLine("ShoppingCartViewModel: Se navighează la Checkout.");
            OnProceedToCheckout?.Invoke();
        }

        private bool CanExecuteClearCart() => CartItems.Any();
        private void ExecuteClearCart()
        {
            var result = MessageBox.Show("Sigur doriți să goliți coșul de cumpărături?", "Confirmare Golire Coș",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _shoppingCartService.ClearCart();
            }
        }

        // Implementare IAsyncInitializableVM (dacă este necesar)
        // De obicei, ShoppingCartViewModel nu are nevoie de inițializare asincronă proprie,
        // deoarece se bazează pe starea ShoppingCartService care este un singleton.
        // Totuși, pentru consistență cu alte ViewModel-uri, o putem adăuga.
        public Task InitializeAsync()
        {
            System.Diagnostics.Debug.WriteLine("ShoppingCartViewModel: InitializeAsync called.");
            // Forțează o reîmprospătare a proprietăților la afișare, dacă e nevoie
            OnPropertyChanged(nameof(CartItems)); // Deși CartItems e direct din serviciu
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(TotalItems));
            ProceedToCheckoutCommand.NotifyCanExecuteChanged();
            ClearCartCommand.NotifyCanExecuteChanged();
            return Task.CompletedTask;
        }

        // Este o bună practică să ne dezabonăm de la evenimente când ViewModel-ul nu mai este folosit,
        // pentru a evita memory leaks, mai ales dacă serviciul e singleton și ViewModel-ul e transient.
        // Acest lucru se poate face într-o metodă OnUnloaded/OnDetached, sau prin IDisposable.
        // Momentan, pentru simplitate, omitem acest aspect, dar e important în aplicații complexe.
        // public void Dispose()
        // {
        //     _shoppingCartService.PropertyChanged -= ShoppingCartService_PropertyChanged;
        //     GC.SuppressFinalize(this);
        // }
    }
}