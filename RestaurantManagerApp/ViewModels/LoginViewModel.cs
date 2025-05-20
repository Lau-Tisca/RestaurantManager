using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagerApp.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows;
using System; // Pentru Func și Action
using RestaurantManagerApp.Utils;

namespace RestaurantManagerApp.ViewModels
{
    public partial class LoginViewModel : ObservableValidator
    {
        private readonly IAuthenticationService? _authenticationService; // Nullable pentru DesignTime
        public Func<Task>? OnLoginSuccessAsync { get; set; }
        public Action? OnNavigateToRegister { get; set; }

        [ObservableProperty]
        [Required(ErrorMessage = "Adresa de email este obligatorie.")]
        [EmailAddress(ErrorMessage = "Adresa de email nu este validă.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string _email = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Parola este obligatorie.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string _parola = string.Empty;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))] // Notifică și comanda Login
        private bool _isLoading = false;

        public IAsyncRelayCommand LoginCommand { get; }
        public IRelayCommand NavigateToRegisterCommand { get; }

        // Constructor pentru Design Time
        public LoginViewModel()
        {
            System.Diagnostics.Debug.WriteLine("LoginViewModel DesignTime Constructor Called");
            _email = "test@example.com";
            _parola = "password"; // Adaugă o valoare pentru ca CanExecuteLogin să poată fi true
            LoginCommand = new AsyncRelayCommand(async () => { ErrorMessage = "Login de test (design time)"; await Task.CompletedTask; }, CanExecuteLogin); // Folosește metoda
            NavigateToRegisterCommand = new RelayCommand(() => { System.Diagnostics.Debug.WriteLine("Navigare spre Inregistrare (design time)"); });
        }

        // Constructor pentru Runtime
        public LoginViewModel(IAuthenticationService authenticationService)
        {
            System.Diagnostics.Debug.WriteLine("LoginViewModel Runtime Constructor Called");
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin); // Folosește metoda
            NavigateToRegisterCommand = new RelayCommand(ExecuteNavigateToRegister);
            ValidateAllProperties();
        }

        // Metoda CanExecute pentru LoginCommand
        private bool CanExecuteLogin()
        {
            // Forțează validarea proprietăților Email și Parola dacă nu s-a făcut deja
            // ValidateProperty(Email, nameof(Email)); // Poate fi redundant dacă se face prin atribute
            // ValidateProperty(Parola, nameof(Parola));

            return !HasErrors && !IsLoading &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Parola);
        }

        private async Task ExecuteLoginAsync()
        {
            ValidateAllProperties(); // Revalidează totul înainte de acțiune
            if (!CanExecuteLogin()) // Verifică din nou starea CanExecute
            {
                // Acest if s-ar putea să nu fie strict necesar dacă butonul e corect dezactivat,
                // dar e o măsură de siguranță.
                ErrorMessage = "Vă rugăm corectați erorile sau completați câmpurile.";
                return;
            }

            IsLoading = true;
            ErrorMessage = null;

            var utilizator = await _authenticationService!.LoginAsync(Email, Parola); // Folosim ! deoarece _authenticationService nu e null aici

            IsLoading = false;

            if (utilizator != null)
            {
                MessageBox.Show($"Autentificare reușită ca {utilizator.NumeComplet()} ({utilizator.TipUtilizator})!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                if (OnLoginSuccessAsync != null)
                {
                    await OnLoginSuccessAsync.Invoke();
                }
            }
            else
            {
                ErrorMessage = "Email sau parolă incorectă, sau contul este inactiv.";
            }
        }

        private void ExecuteNavigateToRegister()
        {
            OnNavigateToRegister?.Invoke();
        }

        public void UpdatePassword(string newPassword)
        {
            Parola = newPassword; // Atributul [NotifyCanExecuteChangedFor(nameof(LoginCommand))] de pe _parola va notifica comanda
        }
    }
}
