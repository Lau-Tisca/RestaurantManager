// În ViewModels/MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using RestaurantManagerApp.Models; // Pentru Utilizator
using RestaurantManagerApp.Services; // Pentru IAuthenticationService
using System;
using System.Threading.Tasks; // Pentru Task
using System.Windows;     // Pentru MessageBox

namespace RestaurantManagerApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthenticationService _authenticationService; // Injectăm serviciul de autentificare

        [ObservableProperty]
        private ObservableObject? _currentViewModel;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsUserNotLoggedIn))] // Notifică și proprietatea inversă
        private bool _isUserLoggedIn = false; // Pentru a controla vizibilitatea elementelor UI (ex: buton Logout)

        public bool IsUserNotLoggedIn => !IsUserLoggedIn;


        // Comenzi de Navigare pentru Dashboard
        public IRelayCommand NavigateToCategoriesCommand { get; }
        public IRelayCommand NavigateToAllergensCommand { get; }
        public IRelayCommand NavigateToProductsCommand { get; }
        public IRelayCommand NavigateToMenusCommand { get; }
        public IRelayCommand LogoutCommand { get; }
        public IRelayCommand NavigateBackCommand { get; }


        public MainViewModel(IServiceProvider serviceProvider, IAuthenticationService authenticationService)
        {
            _serviceProvider = serviceProvider;
            _authenticationService = authenticationService; // Salvează serviciul injectat

            NavigateToCategoriesCommand = new RelayCommand(ExecuteNavigateToCategoryManagement);
            NavigateToAllergensCommand = new RelayCommand(ExecuteNavigateToAlergenManagement);
            NavigateToProductsCommand = new RelayCommand(ExecuteNavigateToPreparatManagement);
            NavigateToMenusCommand = new RelayCommand(ExecuteNavigateToMeniuManagement);
            LogoutCommand = new RelayCommand(ExecuteLogout, () => IsUserLoggedIn); // Comanda Logout e activă doar dacă e cineva logat
            NavigateBackCommand = new RelayCommand(ExecuteNavigateBack, CanExecuteNavigateBack);

            // Starea inițială a aplicației
            if (_authenticationService.CurrentUser == null)
            {
                NavigateToLogin();
            }
            else
            {
                // Dacă există deja un utilizator (ex: sesiune salvată - nu e cazul acum), navighează corespunzător
                HandleSuccessfulLogin();
            }
        }

        partial void OnCurrentViewModelChanged(ObservableObject? oldValue, ObservableObject? newValue)
        {
            // Simplu: dacă noua vedere nu este Login sau Register sau Dashboard, comanda Înapoi e activă.
            // Pentru o istorie reală, ai face push la oldValue în _navigationHistory aici.
            NavigateBackCommand.NotifyCanExecuteChanged();

            // Dacă noul ViewModel are nevoie de inițializare asincronă specifică la navigare
            // și are o metodă standard (ex: IAsyncInitializeViewModel)
            if (newValue is IAsyncInitializableVM initializableVM)
            {
                // Nu bloca firul UI, rulează asincron fără await aici,
                // sau fă metoda OnCurrentViewModelChanged async Task și folosește await.
                // Pentru simplitate, o lăsăm așa, presupunând că InitializeAsync e rapid sau gestionează corect.
                #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                initializableVM.InitializeAsync();
                #pragma warning restore CS4014
            }
            else if (newValue is CategoryManagementViewModel cmVM) // Exemplu de inițializare specifică
            {
                #pragma warning disable CS4014
                cmVM.InitializeAsync();
                #pragma warning restore CS4014
            }
            // Adaugă else if pentru celelalte ViewModels de management care au InitializeAsync
            else if (newValue is AlergenManagementViewModel amVM) { /* ... amVM.InitializeAsync(); ... */ }
            else if (newValue is PreparatManagementViewModel pmVM) { /* ... pmVM.InitializeAsync(); ... */ }
            else if (newValue is MeniuManagementViewModel mmVM) { /* ... mmVM.InitializeAsync(); ... */ }


        }

        private bool CanExecuteNavigateBack()
        {
            // Comanda "Înapoi" este activă dacă vederea curentă NU este:
            // - LoginView
            // - RegistrationView
            // - EmployeeDashboardView (sau ClientDashboardView) - adică ești într-o vedere "interioară"
            // Pentru o istorie: return _navigationHistory.Count > 0;
            return CurrentViewModel != null &&
                   !(CurrentViewModel is LoginViewModel) &&
                   !(CurrentViewModel is RegistrationViewModel) &&
                   !(CurrentViewModel is EmployeeDashboardViewModel); // Adaugă și ClientDashboardViewModel dacă îl ai
        }

        private void ExecuteNavigateBack()
        {
            // Pentru o istorie reală:
            // if (_navigationHistory.Count > 0)
            // {
            //     CurrentViewModel = _navigationHistory.Pop();
            // }
            // else
            // {
            //     // Fallback la dashboard-ul corespunzător dacă istoria e goală (nu ar trebui dacă CanExecute e corect)
            //     HandleSuccessfulLogin(); // Aceasta va naviga la dashboard-ul corect
            // }

            // Abordare Simplificată: "Înapoi" duce mereu la Dashboard-ul relevant
            Utilizator? currentUser = _authenticationService.CurrentUser;
            if (currentUser != null)
            {
                if (currentUser.TipUtilizator == "Angajat")
                {
                    NavigateToEmployeeDashboard();
                }
                else if (currentUser.TipUtilizator == "Client")
                {
                    // TODO: Navighează la Client Dashboard
                    // Momentan, putem să nu facem nimic sau să mergem la Login dacă nu e dashboard
                    NavigateToLogin(); // Sau o vedere "acasă" pentru client
                }
                else
                {
                    NavigateToLogin(); // Fallback
                }
            }
            else
            {
                NavigateToLogin(); // Dacă nu e niciun utilizator logat, du la Login
            }
        }

        private void UpdateLoginState()
        {
            IsUserLoggedIn = _authenticationService.CurrentUser != null;
            LogoutCommand.NotifyCanExecuteChanged(); // Notifică comanda Logout
        }

        private void NavigateToLogin()
        {
            var loginVM = _serviceProvider.GetService<LoginViewModel>();
            if (loginVM != null)
            {
                loginVM.OnLoginSuccessAsync = async () =>
                {
                    HandleSuccessfulLogin();
                    await Task.CompletedTask; // Func<Task> necesită un Task returnat
                };
                loginVM.OnNavigateToRegister = NavigateToRegister;
                CurrentViewModel = loginVM;
                UpdateLoginState();
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
                registerVM.OnNavigateToLogin = NavigateToLogin;
                CurrentViewModel = registerVM;
                UpdateLoginState(); // Deși nu e logat, e bine să actualizăm starea
            }
        }

        private void HandleSuccessfulLogin()
        {
            UpdateLoginState();
            Utilizator? currentUser = _authenticationService.CurrentUser;
            if (currentUser != null)
            {
                if (currentUser.TipUtilizator == "Angajat")
                {
                    NavigateToEmployeeDashboard();
                }
                else if (currentUser.TipUtilizator == "Client")
                {
                    // TODO: Navighează la Client Dashboard sau Meniu Client
                    MessageBox.Show("Client Dashboard/Menu nu este implementat încă.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Momentan, pentru test, putem naviga la EmployeeDashboard sau la un placeholder
                    // NavigateToEmployeeDashboard(); // Sau CurrentViewModel = new PlaceholderClientViewModel();
                    CurrentViewModel = null;
                }
                else
                {
                    MessageBox.Show("Tip utilizator necunoscut. Se navighează la Login.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigateToLogin(); // Sau un view de eroare
                }
            }
            else
            {
                // Acest caz nu ar trebui să apară dacă OnLoginSuccessAsync e apelat corect
                NavigateToLogin();
            }
        }

        private void NavigateToEmployeeDashboard()
        {
            var dashboardVM = _serviceProvider.GetService<EmployeeDashboardViewModel>();
            if (dashboardVM != null)
            {
                // Setăm acțiunile de navigare pentru butoanele din dashboard
                dashboardVM.NavigateToCategories = ExecuteNavigateToCategoryManagement;
                dashboardVM.NavigateToAllergens = ExecuteNavigateToAlergenManagement;
                dashboardVM.NavigateToProducts = ExecuteNavigateToPreparatManagement;
                dashboardVM.NavigateToMenus = ExecuteNavigateToMeniuManagement;
                // dashboardVM.NavigateToOrders = ...
                // dashboardVM.NavigateToReports = ...

                CurrentViewModel = dashboardVM;
            }
        }

        private void ExecuteLogout()
        {
            _authenticationService.Logout();
            NavigateToLogin(); // După logout, du-te la ecranul de login
        }

        // Metodele efective de schimbare a CurrentViewModel pentru modulele de management
        private void ExecuteNavigateToCategoryManagement()
        {
            var vm = _serviceProvider.GetService<CategoryManagementViewModel>();
            if (vm == null) return; // Verificăm dacă am obținut ViewModel-ul corect
            CurrentViewModel = vm;
        }
        private void ExecuteNavigateToAlergenManagement()
        {
            CurrentViewModel = _serviceProvider.GetService<AlergenManagementViewModel>();
        }
        private void ExecuteNavigateToPreparatManagement()
        {
            CurrentViewModel = _serviceProvider.GetService<PreparatManagementViewModel>();
        }
        private void ExecuteNavigateToMeniuManagement()
        {
            CurrentViewModel = _serviceProvider.GetService<MeniuManagementViewModel>();
        }
    }

    public interface IAsyncInitializableVM
    {
        Task InitializeAsync();
    }
}
