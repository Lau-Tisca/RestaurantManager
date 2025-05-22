using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagerApp.DataAccess; // Pentru IOrderRepository și IPreparatRepository
using RestaurantManagerApp.Models;
using RestaurantManagerApp.Services;
using RestaurantManagerApp.Utils;
using RestaurantManagerApp.ViewModels.Display;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; // Pentru CartItems
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace RestaurantManagerApp.ViewModels
{
    public partial class OrderCheckoutViewModel : ObservableValidator, IAsyncInitializableVM
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IOrderRepository _orderRepository;
        private readonly IPreparatRepository _preparatRepository;
        private readonly ApplicationSettings _appSettings;

        public Action? OnOrderPlacedSuccessfully { get; set; } // Pentru a naviga după plasarea comenzii
        public Action? OnCancelCheckout { get; set; } // Pentru a naviga înapoi la coș/meniu

        // Datele coșului (pot fi legate direct sau prin proprietăți)
        public ObservableCollection<CartItemViewModel> CartItems => _shoppingCartService.CartItems;

        [ObservableProperty]
        private decimal _cartSubtotal;

        // --- Formular Checkout ---
        [ObservableProperty]
        [Required(ErrorMessage = "Adresa de livrare este obligatorie.")]
        [MaxLength(500, ErrorMessage = "Adresa nu poate depăși 500 de caractere.")]
        [NotifyDataErrorInfo]
        private string _deliveryAddress = string.Empty;

        [ObservableProperty]
        // [Phone(ErrorMessage = "Număr de telefon invalid.")] // Poți adăuga validare specifică
        [MaxLength(20, ErrorMessage = "Numărul de telefon nu poate depăși 20 de caractere.")]
        [NotifyDataErrorInfo]
        private string? _phoneNumber;

        [ObservableProperty]
        [MaxLength(1000, ErrorMessage = "Observațiile nu pot depăși 1000 de caractere.")]
        [NotifyDataErrorInfo]
        private string? _orderNotes;

        // --- Sumar Comandă (Calculat) ---
        [ObservableProperty]
        private decimal _calculatedDiscount;

        [ObservableProperty]
        private decimal _calculatedShippingCost;

        [ObservableProperty]
        private decimal _calculatedTotalAmount;

        [ObservableProperty]
        private bool _isLoading = false;


        public IAsyncRelayCommand PlaceOrderCommand { get; }
        public IRelayCommand CancelCommand { get; }

        // Constructor pentru Design Time
        public OrderCheckoutViewModel()
        {
            _appSettings = new ApplicationSettings { ShippingMinOrderValueA = 50, ShippingCostB = 15, MenuDiscountPercentageX = 10, OrderDiscountThresholdY = 100, OrderDiscountPercentageW = 5 };
            // Mock ShoppingCartService
            var mockCart = new MockShoppingCartServiceForCheckout();
            var dummyPrep = new DisplayPreparatViewModel(new Preparat { Denumire = "Pizza Design", Pret = 30 });
            mockCart.AddItemToCart(dummyPrep, 2);
            _shoppingCartService = mockCart;

            _cartSubtotal = _shoppingCartService.Subtotal;
            _deliveryAddress = "Str. Exemplu 123, Oraș";
            _phoneNumber = "0722000000";
            CalculateTotals(); // Calculează pe baza datelor mock

            PlaceOrderCommand = new AsyncRelayCommand(async () => { MessageBox.Show("Design: Comandă Plasată!"); await Task.CompletedTask; }, CanPlaceOrder);
            CancelCommand = new RelayCommand(() => { MessageBox.Show("Design: Checkout Anulat"); });
        }
        // Mock pentru IShoppingCartService
        private class MockShoppingCartServiceForCheckout : IShoppingCartService { public ObservableCollection<CartItemViewModel> CartItems { get; } = new(); public decimal Subtotal => CartItems.Sum(i => i.TotalPrice); public int TotalItems => CartItems.Sum(i => i.Quantity); public event PropertyChangedEventHandler? PropertyChanged; public void AddItemToCart(DisplayMenuItemViewModel i, int q = 1) => CartItems.Add(new CartItemViewModel(i, q)); public void ClearCart() => CartItems.Clear(); public void RemoveItemFromCart(CartItemViewModel c) => CartItems.Remove(c); public void UpdateItemQuantity(CartItemViewModel c, int nq) { if (nq > 0) c.Quantity = nq; else CartItems.Remove(c); } }


        // Constructor pentru Runtime
        public OrderCheckoutViewModel(
            IShoppingCartService shoppingCartService,
            IAuthenticationService authenticationService,
            IOrderRepository orderRepository,
            IPreparatRepository preparatRepository,
            ApplicationSettings appSettings)
        {
            _shoppingCartService = shoppingCartService ?? throw new ArgumentNullException(nameof(shoppingCartService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _preparatRepository = preparatRepository ?? throw new ArgumentNullException(nameof(preparatRepository));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

            PlaceOrderCommand = new AsyncRelayCommand(ExecutePlaceOrderAsync, CanPlaceOrder);
            CancelCommand = new RelayCommand(ExecuteCancelCheckout);

            // Ne abonăm la schimbările coșului pentru a recalcula totalurile dacă coșul s-ar modifica în background (puțin probabil aici)
            _shoppingCartService.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(IShoppingCartService.Subtotal)) InitializeDataAndTotals(); };
        }

        public Task InitializeAsync() // Implementare IAsyncInitializableVM
        {
            System.Diagnostics.Debug.WriteLine("OrderCheckoutViewModel: InitializeAsync called.");
            InitializeDataAndTotals();
            return Task.CompletedTask;
        }

        private void InitializeDataAndTotals()
        {
            // Pre-populează datele din contul utilizatorului
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser != null)
            {
                DeliveryAddress = currentUser.AdresaLivrare ?? string.Empty;
                PhoneNumber = currentUser.NumarTelefon;
            }
            else
            {
                // Utilizatorul nu e logat, nu ar trebui să ajungă aici teoretic
                // Sau, dacă permiți checkout ca oaspete (nu e cazul acum), ai cere datele.
                DeliveryAddress = string.Empty;
                PhoneNumber = string.Empty;
            }
            OrderNotes = string.Empty;
            CartSubtotal = _shoppingCartService.Subtotal; // Ia subtotalul din serviciul de coș
            CalculateTotals(); // Calculează discount, transport, total general
            ValidateAllProperties(); // Validează formularul
            PlaceOrderCommand.NotifyCanExecuteChanged(); // Reevaluază starea butonului de comandă
        }

        private void CalculateTotals()
        {
            decimal subtotalAfterItemDiscounts = CartSubtotal; // Momentan, CartSubtotal este deja după discounturi de meniu (dacă sunt)

            // 1. Discount de valoare pe comandă (prag y, procent w)
            CalculatedDiscount = 0;
            if (subtotalAfterItemDiscounts > (decimal)_appSettings.OrderDiscountThresholdY) // Asigură cast la decimal
            {
                CalculatedDiscount = subtotalAfterItemDiscounts * ((decimal)_appSettings.OrderDiscountPercentageW / 100m);
            }
            // TODO: Adaugă logica pentru discount de loialitate (z comenzi în t timp) dacă o implementezi

            decimal amountAfterOrderDiscounts = subtotalAfterItemDiscounts - CalculatedDiscount;

            // 2. Taxa de transport (prag a, cost b)
            CalculatedShippingCost = 0;
            if (amountAfterOrderDiscounts < (decimal)_appSettings.ShippingMinOrderValueA)
            {
                CalculatedShippingCost = (decimal)_appSettings.ShippingCostB;
            }

            CalculatedTotalAmount = amountAfterOrderDiscounts + CalculatedShippingCost;
            OnPropertyChanged(nameof(CalculatedTotalAmount)); // Asigură notificarea
        }

        // Listeneri pentru proprietățile care afectează totalurile (dacă ai avea inputuri care le schimbă)
        // De ex., dacă ai avea un câmp pentru cod de discount care modifică CalculatedDiscount
        // partial void OnCalculatedDiscountChanged(decimal value) => CalculateTotals();

        private bool CanPlaceOrder()
        {
            ValidateAllProperties(); // Validează adresa, etc.
            return !HasErrors && CartItems.Any() && !IsLoading;
        }

        private async Task ExecutePlaceOrderAsync()
        {
            if (!CanPlaceOrder()) return;

            IsLoading = true; // Setează IsLoading la true la începutul operațiunii
            Comanda? comandaSalvata = null; // Pentru a avea acces la comandă în blocul finally dacă e nevoie
            List<string> stockIssues = new List<string>(); // Pentru a colecta problemele de stoc

            try
            {
                var currentUser = _authenticationService.CurrentUser;
                if (currentUser == null)
                {
                    MessageBox.Show("Eroare: Nu sunteți autentificat. Vă rugăm să vă autentificați.", "Eroare Comandă", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Nu este nevoie de IsLoading = false aici, se va face în finally
                    return;
                }

                // --- VERIFICARE STOC ÎNAINTE DE SALVARE COMANDĂ ---
                System.Diagnostics.Debug.WriteLine("ExecutePlaceOrderAsync: Început verificare stoc.");
                bool allItemsAvailableInRequestedQuantity = true;
                Dictionary<int, decimal> requiredStock = new Dictionary<int, decimal>(); // PreparatID -> Cantitate totală necesară

                // Calculează necesarul total pentru fiecare preparat din comandă
                foreach (var cartItem in CartItems)
                {
                    if (cartItem.MenuItem is DisplayPreparatViewModel dpvm && dpvm.OriginalItem is Models.Preparat pOrig)
                    {
                        if (ParsingHelper.TryParseQuantityString(pOrig.CantitatePortie, out decimal valPortie, out _))
                        {
                            decimal totalNeeded = cartItem.Quantity * valPortie;
                            if (requiredStock.ContainsKey(pOrig.PreparatID))
                                requiredStock[pOrig.PreparatID] += totalNeeded;
                            else
                                requiredStock[pOrig.PreparatID] = totalNeeded;
                        }
                        else { stockIssues.Add($"Format cantitate porție invalid pentru '{pOrig.Denumire}'."); allItemsAvailableInRequestedQuantity = false; }
                    }
                    else if (cartItem.MenuItem is DisplayMenuViewModel dmvm && dmvm.OriginalItem is Meniu mOrig)
                    {
                        if (mOrig.MeniuPreparate != null)
                        {
                            foreach (var comp in mOrig.MeniuPreparate)
                            {
                                if (comp.Preparat != null)
                                {
                                    if (ParsingHelper.TryParseQuantityString(comp.CantitateInMeniu, out decimal valComp, out _))
                                    {
                                        decimal totalNeeded = cartItem.Quantity * valComp;
                                        if (requiredStock.ContainsKey(comp.PreparatID))
                                            requiredStock[comp.PreparatID] += totalNeeded;
                                        else
                                            requiredStock[comp.PreparatID] = totalNeeded;
                                    }
                                    else { stockIssues.Add($"Format cantitate invalid pentru componenta '{comp.Preparat.Denumire}' din meniul '{mOrig.Denumire}'."); allItemsAvailableInRequestedQuantity = false; }
                                }
                            }
                        }
                    }
                }

                if (!allItemsAvailableInRequestedQuantity) // Dacă au fost probleme la parsarea cantităților
                {
                    MessageBox.Show("Au apărut probleme la procesarea cantităților din comandă:\n" + string.Join("\n", stockIssues), "Eroare Cantități", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // finally se va ocupa de IsLoading
                }

                // Verifică stocul real pentru fiecare preparat necesar
                foreach (var itemRequired in requiredStock)
                {
                    var preparatId = itemRequired.Key;
                    var cantitateNecesara = itemRequired.Value;
                    var stocDisponibil = await _preparatRepository.GetAvailableStockByIdAsync(preparatId);

                    if (stocDisponibil == null) // Preparatul nu a fost găsit (problemă gravă)
                    {
                        stockIssues.Add($"Preparatul cu ID {preparatId} nu a fost găsit în baza de date.");
                        allItemsAvailableInRequestedQuantity = false;
                    }
                    else if (stocDisponibil < cantitateNecesara)
                    {
                        // Găsește numele preparatului pentru un mesaj mai prietenos
                        var preparatInfo = _preparatRepository.GetByIdAsync(preparatId).Result; // Apel sincron pentru nume, sau încarcă toate preparatele la început
                        string numePreparat = preparatInfo?.Denumire ?? $"ID {preparatId}";
                        stockIssues.Add($"Stoc insuficient pentru '{numePreparat}'. Necesar: {cantitateNecesara}, Disponibil: {stocDisponibil}.");
                        allItemsAvailableInRequestedQuantity = false;
                    }
                }

                if (!allItemsAvailableInRequestedQuantity)
                {
                    MessageBox.Show("Nu se poate plasa comanda din cauza problemelor de stoc:\n" + string.Join("\n", stockIssues) + "\nVă rugăm ajustați coșul.", "Stoc Insuficient", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; // finally se va ocupa de IsLoading
                }
                System.Diagnostics.Debug.WriteLine("ExecutePlaceOrderAsync: Verificare stoc trecută cu succes.");
                // --- SFÂRȘIT VERIFICARE STOC ---

                // 1. Creează obiectul Comanda
                var comanda = new Comanda
                {
                    UtilizatorID = currentUser.UtilizatorID,
                    AdresaLivrareComanda = DeliveryAddress,
                    NumarTelefonComanda = PhoneNumber,
                    Observatii = OrderNotes,
                    Subtotal = CartSubtotal,
                    DiscountAplicat = CalculatedDiscount,
                    CostTransport = CalculatedShippingCost,
                    TotalGeneral = CalculatedTotalAmount,
                    // DataComanda, CodUnic, StareComanda - vor fi setate în repository sau au valori default
                };

                // 2. Creează lista de ElementeComanda
                var elementeComanda = new List<ElementComanda>();
                foreach (var cartItem in CartItems)
                {
                    elementeComanda.Add(new ElementComanda
                    {
                        PreparatID = !cartItem.MenuItem.EsteMeniuCompus ? (int?)cartItem.MenuItem.OriginalId : null,
                        MeniuID = cartItem.MenuItem.EsteMeniuCompus ? (int?)cartItem.MenuItem.OriginalId : null,
                        Cantitate = cartItem.Quantity,
                        PretUnitarLaMomentulComenzii = cartItem.UnitPrice,
                        SubtotalElement = cartItem.TotalPrice
                    });
                }

                // 3. Salvează comanda
                // Este important ca AddOrderAsync să seteze comanda.ComandaID și comanda.CodUnic
                // dacă sunt generate în repository și vrem să le folosim mai jos.
                // Să presupunem că AddOrderAsync returnează ID-ul, dar actualizează și obiectul 'comanda' pasat.
                await _orderRepository.AddOrderAsync(comanda, elementeComanda);
                comandaSalvata = comanda; // Păstrăm referința la comanda cu ID și CodUnic (dacă sunt setate de repo)
                System.Diagnostics.Debug.WriteLine($"Comanda cu ID {comandaSalvata.ComandaID} (Cod: {comandaSalvata.CodUnic}) a fost procesată pentru salvare.");


                // 4. Actualizează stocul
                System.Diagnostics.Debug.WriteLine("Început actualizare stocuri...");
                try // Un try-catch intern pentru actualizarea stocului
                {
                    foreach (var cartItem in CartItems)
                    {
                        // ... (logica de actualizare stoc pe care am discutat-o, cu ParsingHelper) ...
                        // Exemplu simplificat pentru un preparat:
                        if (cartItem.MenuItem is DisplayPreparatViewModel dpvm && dpvm.OriginalItem is Models.Preparat preparatOriginal)
                        {
                            if (ParsingHelper.TryParseQuantityString(preparatOriginal.CantitatePortie, out decimal valoarePortieNumerica, out _))
                            {
                                await _preparatRepository.UpdateStockAsync(preparatOriginal.PreparatID, cartItem.Quantity * valoarePortieNumerica);
                            }
                        }
                        else if (cartItem.MenuItem is DisplayMenuViewModel dmvm && dmvm.OriginalItem is Meniu meniuOriginal)
                        {
                            if (meniuOriginal.MeniuPreparate != null)
                            {
                                foreach (var componentaMeniu in meniuOriginal.MeniuPreparate)
                                {
                                    if (componentaMeniu.Preparat != null &&
                                        ParsingHelper.TryParseQuantityString(componentaMeniu.CantitateInMeniu, out decimal cantitateComponentaNumerica, out _))
                                    {
                                        await _preparatRepository.UpdateStockAsync(componentaMeniu.PreparatID, cartItem.Quantity * cantitateComponentaNumerica);
                                    }
                                }
                            }
                        }
                    }
                    System.Diagnostics.Debug.WriteLine("Stocuri actualizate (sau s-a încercat actualizarea).");
                }
                catch (Exception stockEx)
                {
                    System.Diagnostics.Debug.WriteLine($"EROARE LA ACTUALIZAREA STOCULUI după plasarea comenzii {comandaSalvata?.CodUnic}: {stockEx.ToString()}");
                    MessageBox.Show($"Comanda (Cod: {comandaSalvata?.CodUnic}) a fost plasată, dar a apărut o eroare la actualizarea stocului: {stockEx.Message}. Vă rugăm contactați administratorul pentru verificarea stocurilor.", "Avertisment Stoc Critic", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Comanda rămâne plasată.
                }

                // 5. Golește coșul și notifică utilizatorul
                _shoppingCartService.ClearCart();
                MessageBox.Show($"Comanda dvs. (Cod: {comandaSalvata?.CodUnic}) a fost plasată cu succes! Total de plată: {comandaSalvata?.TotalGeneral:C}", "Comandă Plasată", MessageBoxButton.OK, MessageBoxImage.Information);
                OnOrderPlacedSuccessfully?.Invoke();
            }
            catch (Exception ex) // Prinde erorile de la salvarea comenzii principale sau alte erori neașteptate
            {
                System.Diagnostics.Debug.WriteLine($"EROARE GENERALĂ la plasarea comenzii: {ex.ToString()}");
                MessageBox.Show($"A apărut o eroare la plasarea comenzii: {ex.Message}", "Eroare Comandă", MessageBoxButton.OK, MessageBoxImage.Error);
                // OnOrderPlacedSuccessfully NU este invocat aici
            }
            finally
            {
                IsLoading = false; // ASIGURĂ-TE CĂ IsLoading este setat la false indiferent de rezultat
            }
        }

        private void ExecuteCancelCheckout()
        {
            OnCancelCheckout?.Invoke(); // Navighează înapoi (probabil la coș sau meniu)
        }
    }
}