// În ViewModels/MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using RestaurantManagerApp.Models;
using RestaurantManagerApp.Services;
using RestaurantManagerApp.Utils;
using System;
using System.Collections.Generic; // Pentru Stack (dacă implementezi istorie navigație)
using System.Threading.Tasks;
using System.Windows;

namespace RestaurantManagerApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthenticationService _authenticationService;
        private readonly IShoppingCartService _shoppingCartService;

        [ObservableProperty]
        private ObservableObject? _currentViewModel;

        // Proprietatea principală pentru starea de login
        [ObservableProperty]
        // [NotifyPropertyChangedFor(nameof(IsUserNotLoggedIn))] // Nu e nevoie aici dacă o facem în OnIsUserLoggedInChanged
        private bool _isUserLoggedIn = false;

        // Proprietatea calculată inversă
        public bool IsUserNotLoggedIn => !IsUserLoggedIn;

        // Proprietate pentru numele utilizatorului logat
        public string NumeUtilizatorLogat
        {
            get
            {
                return _authenticationService.CurrentUser?.NumeComplet() ?? string.Empty;
            }
        }


        // Comenzi de Navigare pentru Dashboard Angajat
        public IRelayCommand NavigateToCategoriesCommand { get; }
        public IRelayCommand NavigateToAllergensCommand { get; }
        public IRelayCommand NavigateToProductsCommand { get; }
        public IRelayCommand NavigateToMenusCommand { get; }

        // Comenzi Globale
        public IRelayCommand LogoutCommand { get; }
        public IRelayCommand NavigateToLoginCommand { get; } // DECLARĂ AICI
        public IRelayCommand NavigateBackCommand { get; }
        public IRelayCommand NavigateToShoppingCartCommand { get; }

        // Pentru o istorie de navigație simplă (opțional)
        // private Stack<ObservableObject> _navigationHistory = new Stack<ObservableObject>();

        public MainViewModel(IServiceProvider serviceProvider, IAuthenticationService authenticationService, IShoppingCartService shoppingCartService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _shoppingCartService = shoppingCartService;

            // Inițializare comenzi
            NavigateToCategoriesCommand = new RelayCommand(ExecuteNavigateToCategoryManagement);
            NavigateToAllergensCommand = new RelayCommand(ExecuteNavigateToAlergenManagement);
            NavigateToProductsCommand = new RelayCommand(ExecuteNavigateToPreparatManagement);
            NavigateToMenusCommand = new RelayCommand(ExecuteNavigateToMeniuManagement);
            NavigateToShoppingCartCommand = new RelayCommand(ExecuteNavigateToShoppingCart, CanExecuteNavigateToShoppingCart);

            LogoutCommand = new RelayCommand(ExecuteLogout, () => IsUserLoggedIn);
            NavigateToLoginCommand = new RelayCommand(ExecuteNavigateToLoginInternal, () => !IsUserLoggedIn); // INIȚIALIZEAZĂ AICI
            NavigateBackCommand = new RelayCommand(ExecuteNavigateBack, CanExecuteNavigateBack);

            _shoppingCartService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IShoppingCartService.TotalItems))
                {
                    NavigateToShoppingCartCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(CartItemCountDisplay)); // Notifică și textul afișat
                }
            };

            // Starea inițială a aplicației: afișează meniul restaurantului
            NavigateToRestaurantMenu();
            UpdateLoginState(); // Actualizează starea vizibilității butoanelor Login/Logout
        }

        // Metodă apelată automat când _isUserLoggedIn (și deci IsUserLoggedIn) se schimbă
        partial void OnIsUserLoggedInChanged(bool value)
        {
            OnPropertyChanged(nameof(IsUserNotLoggedIn)); // Notifică proprietatea inversă
            OnPropertyChanged(nameof(NumeUtilizatorLogat)); // Notifică numele utilizatorului
            OnPropertyChanged(nameof(CartItemCountDisplay));
            LogoutCommand.NotifyCanExecuteChanged();       // Notifică comenzile care depind de starea de login
            NavigateToLoginCommand.NotifyCanExecuteChanged();
            NavigateBackCommand.NotifyCanExecuteChanged(); // Starea "Înapoi" poate depinde și de login
            NavigateToShoppingCartCommand.NotifyCanExecuteChanged();
        }

        // Metodă apelată automat când _currentViewModel (și deci CurrentViewModel) se schimbă
        partial void OnCurrentViewModelChanged(ObservableObject? oldValue, ObservableObject? newValue)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: CurrentViewModel changed. Old: {oldValue?.GetType().Name}, New: {newValue?.GetType().Name}");
            NavigateBackCommand.NotifyCanExecuteChanged();

            if (newValue is RestaurantMenuViewModel restaurantMenuVM)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: Noul CurrentViewModel este RestaurantMenuViewModel. Se apelează InitializeAsync...");
                // Forțează inițializarea aici, pe lângă cea din UserControl_Loaded
#pragma warning disable CS4014
                restaurantMenuVM.InitializeAsync();
#pragma warning restore CS4014
            }
            else if (newValue is IAsyncInitializableVM initializableVM) // Generalizează pentru alte VM-uri
            {
                System.Diagnostics.Debug.WriteLine($"MainViewModel: Noul CurrentViewModel ({newValue?.GetType().Name}) implementează IAsyncInitializableVM. Se apelează InitializeAsync...");
#pragma warning disable CS4014
                initializableVM.InitializeAsync();
#pragma warning restore CS4014
            }
            // ... else if pentru alte ViewModels specifice care nu implementează interfața ...
        }

        private bool CanExecuteNavigateBack()
        {
            // O logică mai bună pentru "Înapoi" ar fi necesară dacă ai mai multe niveluri de navigație
            // Momentan, e activ dacă nu suntem pe Login, Register sau un Dashboard principal
            return CurrentViewModel != null &&
                   !(CurrentViewModel is LoginViewModel) &&
                   !(CurrentViewModel is RegistrationViewModel) &&
                   !(CurrentViewModel is EmployeeDashboardViewModel) &&
                   !(CurrentViewModel is RestaurantMenuViewModel && !IsUserLoggedIn); // Nu te duci înapoi de la meniul oaspetelui dacă nu ești logat
        }

        private void ExecuteNavigateBack()
        {
            // Navigație simplificată "Înapoi":
            // Dacă suntem într-o vedere de management (deschisă din EmployeeDashboard), ne întoarcem la EmployeeDashboard.
            // Altfel, dacă suntem logați ca și client și nu suntem pe RestaurantMenu, ne întoarcem la RestaurantMenu.
            // Altfel, la RestaurantMenu (landing page).
            if (_authenticationService.CurrentUser?.TipUtilizator == "Angajat" && !(CurrentViewModel is EmployeeDashboardViewModel))
            {
                NavigateToEmployeeDashboard();
            }
            else if (_authenticationService.CurrentUser?.TipUtilizator == "Client" && !(CurrentViewModel is RestaurantMenuViewModel)) // Presupunând că RestaurantMenu e "acasă" pentru client
            {
                NavigateToRestaurantMenu();
            }
            else
            {
                NavigateToRestaurantMenu(); // Pagina default
            }
        }


        private void UpdateLoginState()
        {
            // Forțează notificarea pentru IsUserLoggedIn pentru a declanșa OnIsUserLoggedInChanged
            bool newState = _authenticationService.CurrentUser != null;
            if (IsUserLoggedIn != newState) // Setează doar dacă s-a schimbat efectiv pentru a evita bucle
            {
                IsUserLoggedIn = newState;
            }
            else // Dacă starea nu s-a schimbat, dar vrem să forțăm notificarea proprietăților dependente
            {
                OnPropertyChanged(nameof(IsUserLoggedIn)); // Va apela OnIsUserLoggedInChanged
                OnPropertyChanged(nameof(IsUserNotLoggedIn));
                OnPropertyChanged(nameof(NumeUtilizatorLogat));
                LogoutCommand.NotifyCanExecuteChanged();
                NavigateToLoginCommand.NotifyCanExecuteChanged();
            }
        }

        // Metoda internă apelată de comanda NavigateToLoginCommand
        private void ExecuteNavigateToLoginInternal()
        {
            NavigateToLogin(true); // true pentru a indica că e o acțiune a utilizatorului
        }

        // Metoda de navigare la Login, poate fi apelată și intern
        private void NavigateToLogin(bool fromUserActionOrSpecificContext = false)
        {
            // if (!fromUserActionOrSpecificContext && IsUserLoggedIn) return; // O optimizare posibilă

            var loginVM = _serviceProvider.GetService<LoginViewModel>();
            if (loginVM != null)
            {
                loginVM.OnLoginSuccessAsync = async () =>
                {
                    HandleSuccessfulLogin();
                    await Task.CompletedTask;
                };
                loginVM.OnNavigateToRegister = NavigateToRegister; // Aceasta ar trebui să fie OK dacă NavigateToRegister nu are parametri
                CurrentViewModel = loginVM;
            }
        }

        private void NavigateToRegister()
        {
            var registerVM = _serviceProvider.GetService<RegistrationViewModel>();
            if (registerVM != null)
            {
                registerVM.OnRegistrationSuccessAsync = async () =>
                {
                    NavigateToLogin();
                    await Task.CompletedTask;
                };
                registerVM.OnNavigateToLogin = ExecuteNavigateToLoginInternal;
                CurrentViewModel = registerVM;
            }
        }

        private void HandleSuccessfulLogin()
        {
            UpdateLoginState(); // Foarte important să fie apelat aici pentru a actualiza UI-ul
            Utilizator? currentUser = _authenticationService.CurrentUser;
            if (currentUser != null)
            {
                if (currentUser.TipUtilizator == "Angajat")
                {
                    NavigateToEmployeeDashboard();
                }
                else if (currentUser.TipUtilizator == "Client")
                {
                    NavigateToRestaurantMenu();
                }
                else
                {
                    MessageBox.Show("Tip utilizator necunoscut. Se navighează la Login.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigateToLogin();
                }
            }
            else
            {
                NavigateToLogin();
            }
        }

        private void NavigateToEmployeeDashboard()
        {
            var dashboardVM = _serviceProvider.GetService<EmployeeDashboardViewModel>();
            if (dashboardVM != null)
            {
                dashboardVM.NavigateToCategories = ExecuteNavigateToCategoryManagement;
                dashboardVM.NavigateToAllergens = ExecuteNavigateToAlergenManagement;
                dashboardVM.NavigateToProducts = ExecuteNavigateToPreparatManagement;
                dashboardVM.NavigateToMenus = ExecuteNavigateToMeniuManagement;
                CurrentViewModel = dashboardVM;
            }
        }

        private void NavigateToRestaurantMenu()
        {
            CurrentViewModel = _serviceProvider.GetService<RestaurantMenuViewModel>();
        }

        private void ExecuteLogout()
        {
            _authenticationService.Logout();
            // UpdateLoginState(); // Este apelat de OnIsUserLoggedInChanged
            IsUserLoggedIn = false; // Setează direct și lasă OnIsUserLoggedInChanged să facă restul
            NavigateToRestaurantMenu(); // După logout, du-te la meniul restaurantului (landing page)
        }

        private void ExecuteNavigateToCategoryManagement() => CurrentViewModel = _serviceProvider.GetService<CategoryManagementViewModel>();
        private void ExecuteNavigateToAlergenManagement() => CurrentViewModel = _serviceProvider.GetService<AlergenManagementViewModel>();
        private void ExecuteNavigateToPreparatManagement() => CurrentViewModel = _serviceProvider.GetService<PreparatManagementViewModel>();
        private void ExecuteNavigateToMeniuManagement() => CurrentViewModel = _serviceProvider.GetService<MeniuManagementViewModel>();
        private void ExecuteNavigateToShoppingCart()
        {
            CurrentViewModel = _serviceProvider.GetService<ShoppingCartViewModel>();
        }
        public string CartItemCountDisplay => _shoppingCartService.TotalItems > 0 ? $"Coș ({_shoppingCartService.TotalItems})" : "Coș";
        private bool CanExecuteNavigateToShoppingCart() => _shoppingCartService.TotalItems > 0; // Sau mereu activ
    }

    // Interfață pentru ViewModels care necesită inițializare asincronă la navigare
    public interface IAsyncInitializableVM
    {
        Task InitializeAsync();
        // Poți adăuga un bool IsDataLoaded { get; } pentru a evita reîncărcarea inutilă
    }
}