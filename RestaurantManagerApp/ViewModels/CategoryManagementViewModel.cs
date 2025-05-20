using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagerApp.DataAccess;
using RestaurantManagerApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel; // Necesar pentru DesignerProperties
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RestaurantManagerApp.ViewModels
{
    public partial class CategoryManagementViewModel : ObservableValidator, IAsyncInitializableVM
    {
        private readonly ICategorieRepository? _categorieRepository; // Nullable pentru design time
        private Categorie? _originalCategorie;


        [ObservableProperty]
        private ObservableCollection<Categorie> _categorii;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCategoryCommand))]
        private Categorie? _selectedCategorie;

        [ObservableProperty]
        [Required(ErrorMessage = "Numele categoriei este obligatoriu.")]
        [MaxLength(100, ErrorMessage = "Numele nu poate depăși 100 de caractere.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(AddNewCategoryCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private string _formNume = string.Empty;

        [ObservableProperty]
        // Am eliminat [NotifyDataErrorInfo] de aici dacă nu sunt validări specifice
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private bool _formEsteActiv = true;

        [ObservableProperty]
        private bool _isEditMode = false;

        public bool IsAddMode => !IsEditMode;

        public IAsyncRelayCommand LoadCategoriesCommand { get; }
        public IRelayCommand PrepareNewCategoryCommand { get; }
        public IAsyncRelayCommand AddNewCategoryCommand { get; }
        public IAsyncRelayCommand SaveChangesCommand { get; }
        public IAsyncRelayCommand DeleteCategoryCommand { get; }
        public IRelayCommand CancelEditCommand { get; }

        // Constructor pentru Design Time
        public CategoryManagementViewModel()
        {
            System.Diagnostics.Debug.WriteLine("CategoryManagementViewModel DesignTime Constructor Called");
            _categorii = new ObservableCollection<Categorie>();
            // Inițializări minime pentru câmpurile legate în UI, pentru a evita NullReference la binding
            _formNume = "Design Nume";
            _formEsteActiv = true;
            _isEditMode = false; // Sau true, pentru a testa ambele stări ale UI-ului

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                _categorii.Add(new Categorie { Nume = "Design Categorie 1" });
                _categorii.Add(new Categorie { Nume = "Design Categorie 2" });
            }

            // Inițializează comenzile cu implementări sigure (care nu fac nimic sau fac operații sigure)
            LoadCategoriesCommand = new AsyncRelayCommand(async () => await Task.CompletedTask);
            PrepareNewCategoryCommand = new RelayCommand(() => { IsEditMode = false; FormNume = "Nou Design"; });
            AddNewCategoryCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => true); // Presupunem că e valid în design
            SaveChangesCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => true); // Presupunem că e valid în design
            DeleteCategoryCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => true); // Presupunem că e valid în design
            CancelEditCommand = new RelayCommand(() => { IsEditMode = false; FormNume = "Anulat Design"; });

            // Nu apela ValidateAllProperties() aici decât dacă ești sigur că nu va genera erori
            // din cauza lipsei contextului de runtime. Adesea, pentru design time, e mai bine să eviți.
        }

        // Constructor pentru Runtime
        public CategoryManagementViewModel(ICategorieRepository categorieRepository)
        {
            System.Diagnostics.Debug.WriteLine("CategoryManagementViewModel Runtime Constructor Called");
            _categorieRepository = categorieRepository;
            _categorii = new ObservableCollection<Categorie>();
            // FormNume și FormEsteActiv sunt inițializate de câmpurile lor cu [ObservableProperty]

            LoadCategoriesCommand = new AsyncRelayCommand(LoadCategoriesAsync);
            PrepareNewCategoryCommand = new RelayCommand(ExecutePrepareNewCategory);
            AddNewCategoryCommand = new AsyncRelayCommand(ExecuteAddCategoryAsync, CanExecuteAddOrSave);
            SaveChangesCommand = new AsyncRelayCommand(ExecuteUpdateCategoryAsync, CanExecuteAddOrSave);
            DeleteCategoryCommand = new AsyncRelayCommand(ExecuteDeleteCategoryAsync, CanExecuteDelete);
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit);

            ExecutePrepareNewCategory();
            ValidateAllProperties(); // La runtime, validarea este importantă
        }

        partial void OnSelectedCategorieChanged(Categorie? value)
        {
            if (value != null)
            {
                IsEditMode = true;
                _originalCategorie = new Categorie { CategorieID = value.CategorieID, Nume = value.Nume, EsteActiv = value.EsteActiv };
                FormNume = value.Nume;
                FormEsteActiv = value.EsteActiv;
                ClearErrors();
            }
            else
            {
                if (IsEditMode) // Doar dacă eram în editare și s-a deselectat
                {
                    ExecutePrepareNewCategory();
                }
            }
        }

        partial void OnIsEditModeChanged(bool value)
        {
            OnPropertyChanged(nameof(IsAddMode));
            AddNewCategoryCommand.NotifyCanExecuteChanged();
            SaveChangesCommand.NotifyCanExecuteChanged();
        }

        private async Task LoadCategoriesAsync()
        {
            if (_categorieRepository == null) // Protecție pentru design time
            {
                if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                {
                    // E o problemă la runtime dacă repository-ul e null și nu suntem în design mode
                    MessageBox.Show("Eroare: Repository-ul de categorii nu este inițializat.", "Eroare Critică", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            var categoriesList = await _categorieRepository.GetAllActiveAsync();
            Categorii.Clear();
            foreach (var cat in categoriesList.OrderBy(c => c.Nume))
            {
                Categorii.Add(cat);
            }
            ExecutePrepareNewCategory();
        }

        private void ExecutePrepareNewCategory()
        {
            IsEditMode = false;
            // SelectedCategorie = null; // Setarea IsEditMode = false și logica din OnSelectedCategorieChanged ar trebui să gestioneze asta,
            // dar pentru a fi explicit, o putem face aici.
            // Totuși, dacă SelectedCategorie e legat la ListBox, setarea lui la null aici
            // poate cauza un ciclu dacă OnSelectedCategorieChanged apelează PrepareNewCategory.
            // Este mai sigur să fie gestionat prin IsEditMode și interacțiunea utilizatorului.
            // Lasă ca OnSelectedCategorieChanged(null) să facă treaba dacă e cazul.
            _originalCategorie = null;
            FormNume = string.Empty;
            FormEsteActiv = true;
            ClearErrors();
        }

        private bool CanExecuteAddOrSave()
        {
            ValidateAllProperties();
            if (HasErrors) return false;

            if (IsEditMode)
            {
                return SelectedCategorie != null && _originalCategorie != null &&
                       (FormNume != _originalCategorie.Nume || FormEsteActiv != _originalCategorie.EsteActiv);
            }
            else
            {
                return !string.IsNullOrWhiteSpace(FormNume);
            }
        }

        private async Task ExecuteAddCategoryAsync()
        {
            if (!CanExecuteAddOrSave() || _categorieRepository == null) return;
            if (await _categorieRepository.NameExistsAsync(FormNume))
            {
                MessageBox.Show($"Categoria '{FormNume}' există deja.", "Nume Duplicat", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var newCategory = new Categorie { Nume = FormNume, EsteActiv = FormEsteActiv };
            await _categorieRepository.AddAsync(newCategory);
            await LoadCategoriesAsync();
        }

        private async Task ExecuteUpdateCategoryAsync()
        {
            if (!CanExecuteAddOrSave() || SelectedCategorie == null || _originalCategorie == null || _categorieRepository == null) return;
            if (FormNume != _originalCategorie.Nume && await _categorieRepository.NameExistsAsync(FormNume, SelectedCategorie.CategorieID))
            {
                MessageBox.Show($"Categoria '{FormNume}' există deja pentru un alt ID.", "Nume Duplicat", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var updatedCategory = new Categorie { CategorieID = SelectedCategorie.CategorieID, Nume = FormNume, EsteActiv = FormEsteActiv };
            await _categorieRepository.UpdateAsync(updatedCategory);
            await LoadCategoriesAsync();
        }

        private bool CanExecuteDelete()
        {
            return SelectedCategorie != null && IsEditMode;
        }

        private async Task ExecuteDeleteCategoryAsync()
        {
            if (!CanExecuteDelete() || SelectedCategorie == null || _categorieRepository == null) return;
            var result = MessageBox.Show($"Sigur doriți să ștergeți (marcați ca inactivă) categoria '{SelectedCategorie.Nume}'?",
                                         "Confirmare Ștergere", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await _categorieRepository.DeleteAsync(SelectedCategorie.CategorieID);
                await LoadCategoriesAsync();
            }
        }

        private void ExecuteCancelEdit()
        {
            ExecutePrepareNewCategory();
        }

        public async Task InitializeAsync()
        {
            await LoadCategoriesAsync();
        }
    }
}