using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // Pentru IRelayCommand dacă e nevoie de comenzi aici
using Microsoft.Extensions.DependencyInjection; // Pentru IServiceProvider
using System; // Pentru Action, Func
using System.Windows;

namespace RestaurantManagerApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider; // Pentru a crea alte ViewModels

        [ObservableProperty]
        private ObservableObject? _currentViewModel; // Vederea curentă afișată

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            // La pornire, afișăm LoginView
            NavigateToLogin();
        }

        private void NavigateToLogin()
        {
            var loginVM = _serviceProvider.GetService<LoginViewModel>();
            if (loginVM != null)
            {
                loginVM.OnLoginSuccessAsync = async () => // Ce se întâmplă după login reușit
                {
                    // Aici vom naviga la un Dashboard sau la o altă vedere principală
                    // Momentan, putem doar schimba CurrentViewModel la ceva simplu
                    // sau să închidem fereastra de login și să deschidem alta (mai puțin MVVM-ish direct)
                    MessageBox.Show("Login Reușit! Navigarea spre Dashboard nu e implementată încă în MainViewModel.", "Navigare", MessageBoxButton.OK, MessageBoxImage.Information);
                    // CurrentViewModel = _serviceProvider.GetService<NumeDashboardViewModel>(); // Exemplu
                    // Pentru a testa acum, putem naviga la CategoryManagement
                    NavigateToCategoryManagement();
                };
                loginVM.OnNavigateToRegister = NavigateToRegister; // Setează acțiunea de navigare
                CurrentViewModel = loginVM;
            }
        }

        private void NavigateToRegister()
        {
            var registerVM = _serviceProvider.GetService<RegistrationViewModel>();
            if (registerVM != null)
            {
                registerVM.OnRegistrationSuccessAsync = async () => // Ce se întâmplă după înregistrare reușită
                {
                    NavigateToLogin(); // Întoarce la Login după înregistrare
                    await Task.CompletedTask;
                };
                registerVM.OnNavigateToLogin = NavigateToLogin; // Setează acțiunea de navigare
                CurrentViewModel = registerVM;
            }
        }

        // Metode de navigare pentru modulele de management (exemple)
        // Acestea ar putea fi apelate de pe un "Dashboard" View
        public void NavigateToCategoryManagement()
        {
            CurrentViewModel = _serviceProvider.GetService<CategoryManagementViewModel>();
        }
        public void NavigateToAlergenManagement()
        {
            CurrentViewModel = _serviceProvider.GetService<AlergenManagementViewModel>();
        }
        public void NavigateToPreparatManagement()
        {
            CurrentViewModel = _serviceProvider.GetService<PreparatManagementViewModel>();
        }
        public void NavigateToMeniuManagement()
        {
            CurrentViewModel = _serviceProvider.GetService<MeniuManagementViewModel>();
        }
        // Adaugă și o metodă de Logout care va apela AuthenticationService.Logout() și va naviga la Login
    }
}