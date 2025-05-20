using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagerApp.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic; // Necesar pentru List<string> în SetErrors

namespace RestaurantManagerApp.ViewModels
{
    public partial class RegistrationViewModel : ObservableValidator
    {
        private readonly IAuthenticationService _authenticationService;
        public Func<Task>? OnRegistrationSuccessAsync { get; set; }
        public Action? OnNavigateToLogin { get; set; }

        [ObservableProperty]
        [Required(ErrorMessage = "Numele este obligatoriu.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _nume = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Prenumele este obligatoriu.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _prenume = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Adresa de email este obligatorie.")]
        [EmailAddress(ErrorMessage = "Adresa de email nu este validă.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _email = string.Empty;

        // Pentru Parola, o lăsăm cu [ObservableProperty] deoarece [Compare] se va aplica pe ConfirmaParola
        [ObservableProperty]
        [Required(ErrorMessage = "Parola este obligatorie.")]
        [MinLength(6, ErrorMessage = "Parola trebuie să aibă minim 6 caractere.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _parola = string.Empty;

        // Pentru ConfirmaParola, o definim manual pentru a aplica [Compare]
        private string _confirmaParolaManual = string.Empty; // Câmp privat de stocare

        [Required(ErrorMessage = "Confirmarea parolei este obligatorie.")]
        [Compare(nameof(Parola), ErrorMessage = "Parolele nu se potrivesc.")] // Acum se aplică pe proprietatea publică
        //[NotifyDataErrorInfo] // Spune ObservableValidator să includă această proprietate în validare
        public string ConfirmaParola
        {
            get => _confirmaParolaManual;
            set
            {
                // Setăm valoarea și declanșăm validarea pentru ACEASTĂ proprietate
                SetProperty(ref _confirmaParolaManual, value, true); // true pentru a valida imediat
                // Deoarece [Compare] depinde de Parola, și Parola ar trebui să declanșeze revalidarea lui ConfirmaParola
                // Am adăugat [NotifyCanExecuteChangedFor(nameof(ConfirmaParola))] pe _parola
                // De asemenea, notificăm comanda Register că starea ei s-ar putea schimba
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }


        [ObservableProperty]
        private string? _numarTelefon;

        [ObservableProperty]
        private string? _adresaLivrare;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isLoading = false;

        public IAsyncRelayCommand RegisterCommand { get; }
        public IRelayCommand NavigateToLoginCommand { get; }

        // Constructor Design Time
        public RegistrationViewModel()
        {
            System.Diagnostics.Debug.WriteLine("RegistrationViewModel DesignTime Constructor Called");
            RegisterCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => CanRegister);
            NavigateToLoginCommand = new RelayCommand(() => { });
        }

        // Constructor Runtime
        public RegistrationViewModel(IAuthenticationService authenticationService)
        {
            System.Diagnostics.Debug.WriteLine("RegistrationViewModel Runtime Constructor Called");
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            RegisterCommand = new AsyncRelayCommand(ExecuteRegisterAsync, CanExecuteRegister);
            NavigateToLoginCommand = new RelayCommand(ExecuteNavigateToLogin);
            ValidateAllProperties(); // Inițiază validarea
        }

        // Am eliminat partial void OnParolaChanged și OnConfirmaParolaChanged
        // deoarece notificările CanExecute sunt acum pe atribute sau în setter-ul lui ConfirmaParola.
        // Validarea pentru [Compare] se va declanșa când se schimbă ConfirmaParola (datorită validate: true în SetProperty)
        // și când se schimbă Parola (datorită [NotifyCanExecuteChangedFor(nameof(ConfirmaParola))] pe _parola, care ar trebui să fie [NotifyDataErrorInfo] pe ConfirmaParola).
        // Mai corect: când _parola se schimbă, trebuie să revalidăm ConfirmaParola.
        partial void OnParolaChanged(string value)
        {
            // Forțează revalidarea proprietății ConfirmaParola atunci când Parola se schimbă
            ValidateProperty(ConfirmaParola, nameof(ConfirmaParola));
            RegisterCommand.NotifyCanExecuteChanged();
        }


        public bool CanRegister => !HasErrors && !IsLoading &&
                                 !string.IsNullOrWhiteSpace(Nume) &&
                                 !string.IsNullOrWhiteSpace(Prenume) &&
                                 !string.IsNullOrWhiteSpace(Email) &&
                                 !string.IsNullOrWhiteSpace(Parola) &&
                                 !string.IsNullOrWhiteSpace(ConfirmaParola);
        // Verificarea Parola == ConfirmaParola este acum făcută de atributul [Compare]
        // și va fi reflectată în HasErrors.

        private bool CanExecuteRegister()
        {
            // Forțează validarea tuturor proprietăților înainte de a verifica HasErrors
            // ValidateAllProperties(); // Acest lucru poate fi costisitor și se face deja la schimbarea proprietăților
            // Este suficient să verificăm HasErrors dacă validarea e la zi.

            return !HasErrors && !IsLoading &&
                   !string.IsNullOrWhiteSpace(Nume) &&
                   !string.IsNullOrWhiteSpace(Prenume) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Parola) &&
                   !string.IsNullOrWhiteSpace(ConfirmaParola);
            // Verificarea Parola == ConfirmaParola este acum făcută de atributul [Compare]
            // și va fi reflectată în HasErrors.
        }

        private async Task ExecuteRegisterAsync()
        {
            ValidateAllProperties(); // Asigură-te că toate validările rulează
            if (HasErrors)
            {
                ErrorMessage = "Vă rugăm corectați erorile de validare.";
                // Loghează erorile specifice pentru debug, dacă e necesar
                var errors = GetErrors(null).Select(e => $"{string.Join(",", e.MemberNames)}: {e.ErrorMessage}");
                System.Diagnostics.Debug.WriteLine("Erori validare la Register: " + string.Join("; ", errors));
                return;
            }
            // Verificarea suplimentară Parola != ConfirmaParola nu mai e strict necesară aici
            // dacă atributul [Compare] funcționează și setează HasErrors.

            IsLoading = true;
            ErrorMessage = null;

            bool success = await _authenticationService.RegisterClientAsync(Nume, Prenume, Email, Parola, NumarTelefon, AdresaLivrare);
            IsLoading = false;

            if (success)
            {
                MessageBox.Show("Înregistrare reușită! Vă puteți autentifica acum.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                OnRegistrationSuccessAsync?.Invoke(); // Am eliminat await dacă OnRegistrationSuccessAsync e Func<Task>?
            }
            else
            {
                ErrorMessage = "Înregistrarea a eșuat. Emailul ar putea fi deja folosit sau a apărut o problemă.";
                // Poți încerca să adaugi o eroare specifică pe câmpul Email dacă știi că asta e problema
                // SetErrors(nameof(Email), new List<string> { "Acest email este deja înregistrat." });
            }
        }

        private void ExecuteNavigateToLogin()
        {
            OnNavigateToLogin?.Invoke();
        }

        public void UpdateParola(string newPassword) => Parola = newPassword;
        public void UpdateConfirmaParola(string newConfirmPassword) => ConfirmaParola = newConfirmPassword;
    }
}