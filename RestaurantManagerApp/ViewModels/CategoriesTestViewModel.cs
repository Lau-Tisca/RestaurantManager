using CommunityToolkit.Mvvm.ComponentModel; // Pentru ObservableObject
using CommunityToolkit.Mvvm.Input;         // Pentru RelayCommand
using RestaurantManagerApp.DataAccess;    // Pentru ICategorieRepository
using RestaurantManagerApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // Pentru MessageBox

namespace RestaurantManagerApp.ViewModels
{
    public partial class CategoriesTestViewModel : ObservableObject // Folosim partial pentru source generators
    {
        private readonly ICategorieRepository? _categorieRepository;

        // O colecție care notifică UI-ul când se schimbă (adăugare, ștergere elemente)
        [ObservableProperty] // Generează automat proprietatea Categorii și notificarea
        private ObservableCollection<Categorie> _categorii;

        [ObservableProperty] // Generează automat proprietatea NumeCategorieNoua și notificarea
        private string _numeCategorieNoua;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteCategoryCommand))] // Notifică comanda când se schimbă
        private Categorie? _selectedCategorie; // Categoria selectată în listă

        public IAsyncRelayCommand LoadCategoriesCommand { get; }
        public IAsyncRelayCommand AddCategoryCommand { get; }
        public IAsyncRelayCommand DeleteCategoryCommand { get; }

        // Constructor pentru Design Time (fără parametri)
        public CategoriesTestViewModel()
        {
            // Acest constructor va fi apelat de designer-ul XAML
            _categorii = new ObservableCollection<Categorie>();
            _numeCategorieNoua = "Nume Test"; // Valoare de test

            // Poți adăuga date de test în _categorii dacă vrei să le vezi în designer
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                _categorii.Add(new Categorie { Nume = "Categorie Test 1", EsteActiv = true });
                _categorii.Add(new Categorie { Nume = "Categorie Test 2", EsteActiv = true });
                _selectedCategorie = _categorii[0];
            }

            // Inițializează comenzile cu implementări goale sau care nu aruncă excepții în design time
            LoadCategoriesCommand = new AsyncRelayCommand(async () => await Task.CompletedTask);
            AddCategoryCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => !string.IsNullOrWhiteSpace(NumeCategorieNoua));
            DeleteCategoryCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => SelectedCategorie != null);
        }

        // Constructor pentru Runtime (cu dependențe injectate)
        public CategoriesTestViewModel(ICategorieRepository categorieRepository)
        {
            _categorieRepository = categorieRepository; // Repository-ul real

            _categorii = new ObservableCollection<Categorie>();
            _numeCategorieNoua = string.Empty;

            LoadCategoriesCommand = new AsyncRelayCommand(LoadCategoriesAsync);
            AddCategoryCommand = new AsyncRelayCommand(AddCategoryAsync, CanAddCategory);
            DeleteCategoryCommand = new AsyncRelayCommand(DeleteCategoryAsync, CanDeleteCategory);
        }

        // Metoda apelată la schimbarea proprietății NumeCategorieNoua (generată de [ObservableProperty])
        partial void OnNumeCategorieNouaChanged(string value)
        {
            AddCategoryCommand.NotifyCanExecuteChanged();
        }

        // Metoda apelată la schimbarea proprietății SelectedCategorie (generată de [ObservableProperty])
        // Am adăugat [NotifyCanExecuteChangedFor(nameof(DeleteCategoryCommand))] pe SelectedCategorie
        // deci nu mai e nevoie de partial void OnSelectedCategorieChanged.

        private async Task LoadCategoriesAsync()
        {
            if (_categorieRepository == null) return;
            var categoriesList = await _categorieRepository.GetAllActiveAsync();
            Categorii.Clear();
            foreach (var cat in categoriesList)
            {
                Categorii.Add(cat);
            }
        }

        private bool CanAddCategory()
        {
            return !string.IsNullOrWhiteSpace(NumeCategorieNoua);
        }

        private async Task AddCategoryAsync()
        {
            if (!CanAddCategory()) return;

            // Verificăm dacă numele există deja (opțional, dar bună practică)
            if (await _categorieRepository.NameExistsAsync(NumeCategorieNoua))
            {
                MessageBox.Show($"Categoria '{NumeCategorieNoua}' există deja.", "Eroare Adăugare", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newCategory = new Categorie { Nume = NumeCategorieNoua };
            await _categorieRepository.AddAsync(newCategory);
            NumeCategorieNoua = string.Empty; // Golește câmpul după adăugare
            await LoadCategoriesAsync();      // Reîncarcă lista
        }

        private bool CanDeleteCategory()
        {
            return SelectedCategorie != null;
        }

        private async Task DeleteCategoryAsync()
        {
            if (!CanDeleteCategory() || SelectedCategorie == null) return;

            var result = MessageBox.Show($"Sigur doriți să ștergeți (marcați ca inactivă) categoria '{SelectedCategorie.Nume}'?",
                                         "Confirmare Ștergere", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await _categorieRepository.DeleteAsync(SelectedCategorie.CategorieID);
                await LoadCategoriesAsync(); // Reîncarcă lista
            }
        }

        // Metodă apelată la inițializarea ViewModel-ului (de ex. din constructorul vederii sau la evenimentul Loaded)
        public async Task InitializeAsync()
        {
            await LoadCategoriesAsync();
        }
    }
}