using CommunityToolkit.Mvvm.ComponentModel; // Pentru ObservableObject
using CommunityToolkit.Mvvm.Input;         // Pentru RelayCommand
using RestaurantManagerApp.DataAccess;    // Pentru ICategorieRepository
using RestaurantManagerApp.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // Pentru MessageBox

namespace RestaurantManagerApp.ViewModels
{
    public partial class CategoriesTestViewModel : ObservableObject // Folosim partial pentru source generators
    {
        private readonly ICategorieRepository _categorieRepository;

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

        public CategoriesTestViewModel(ICategorieRepository categorieRepository)
        {
            _categorieRepository = categorieRepository;

            _categorii = new ObservableCollection<Categorie>();
            _numeCategorieNoua = string.Empty;

            LoadCategoriesCommand = new AsyncRelayCommand(LoadCategoriesAsync);
            AddCategoryCommand = new AsyncRelayCommand(AddCategoryAsync, CanAddCategory);
            DeleteCategoryCommand = new AsyncRelayCommand(DeleteCategoryAsync, CanDeleteCategory);

            // Ascultă schimbările la NumeCategorieNoua pentru a re-evalua CanAddCategory
            // (Modul CommunityToolkit.Mvvm de a face asta e prin atribute sau manual PropertyChanged)
            // Vom re-evalua CanExecute manual la schimbarea textului pentru simplitate aici
            // sau folosim [NotifyCanExecuteChangedFor(nameof(AddCategoryCommand))] pe NumeCategorieNoua
            // dacă am face NumeCategorieNoua o proprietate generată cu [ObservableProperty]
            // și comanda ar depinde de ea. Pentru moment, CanAddCategory se bazează pe NumeCategorieNoua.
            // Am adăugat [ObservableProperty] la _numeCategorieNoua, deci putem face:
            // PropertyChanged += (s, e) => { if (e.PropertyName == nameof(NumeCategorieNoua)) AddCategoryCommand.NotifyCanExecuteChanged(); };
            // Sau, mai simplu, folosim atributul direct pe comandă (necesită ca și comanda să fie proprietate generată sau să aibă atribut)
            // Deoarece comenzile sunt definite ca proprietăți simple, vom re-evalua CanExecute manual la schimbarea textului în XAML (prin UpdateSourceTrigger=PropertyChanged)
            // și vom apela NotifyCanExecuteChanged() dacă e necesar din setter-ul proprietății _numeCategorieNoua.
            // Cu [ObservableProperty] pe _numeCategorieNoua, sursa generată va apela OnPropertyChanged.
            // Vom adăuga manual NotifyCanExecuteChanged în setter-ul generat (nu e ideal, dar e o opțiune).
            // Alternativ, mai curat, este ca metoda CanExecute să fie reevaluată de UI la fiecare interacțiune.
            // Pentru simplificare acum, vom lăsa așa și ne bazăm pe reevaluarea automată la focus/interacțiune.
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