using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagerApp.DataAccess;
using RestaurantManagerApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations; // Pentru atribute de validare
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RestaurantManagerApp.ViewModels
{
    // Folosim partial class pentru CommunityToolkit.Mvvm source generators
    public partial class CategoryManagementViewModel : ObservableValidator // ObservableValidator pentru validări
    {
        private readonly ICategorieRepository _categorieRepository;
        private Categorie? _originalCategorie; // Pentru a putea anula editarea

        [ObservableProperty]
        private ObservableCollection<Categorie> _categorii;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCategoryCommand))]
        private Categorie? _selectedCategorie;

        // Proprietăți pentru formularul de adăugare/editare
        // Am adăugat și validări folosind atribute
        private string _formNume = string.Empty;
        [Required(ErrorMessage = "Numele categoriei este obligatoriu.")]
        [MaxLength(100, ErrorMessage = "Numele nu poate depăși 100 de caractere.")]
        [NotifyDataErrorInfo] // Important pentru ObservableValidator
        public string FormNume
        {
            get => _formNume;
            set
            {
                SetProperty(ref _formNume, value, true); // Al treilea parametru true pentru a valida la schimbare
                AddNewCategoryCommand.NotifyCanExecuteChanged(); // Sau SaveChangesCommand dacă e în mod editare
                SaveChangesCommand.NotifyCanExecuteChanged();
            }
        }

        private bool _formEsteActiv = true;
        [NotifyDataErrorInfo]
        public bool FormEsteActiv
        {
            get => _formEsteActiv;
            set => SetProperty(ref _formEsteActiv, value, true);
        }


        [ObservableProperty]
        private bool _isEditMode = false;


        public IAsyncRelayCommand LoadCategoriesCommand { get; }
        public IRelayCommand PrepareNewCategoryCommand { get; } // Pregătește formularul pentru o nouă categorie
        public IAsyncRelayCommand AddNewCategoryCommand { get; } // Adaugă efectiv noua categorie
        public IAsyncRelayCommand SaveChangesCommand { get; }   // Salvează modificările la o categorie existentă
        public IAsyncRelayCommand DeleteCategoryCommand { get; }
        public IRelayCommand CancelEditCommand { get; }

        public CategoryManagementViewModel(ICategorieRepository categorieRepository)
        {
            _categorieRepository = categorieRepository;
            _categorii = new ObservableCollection<Categorie>();

            LoadCategoriesCommand = new AsyncRelayCommand(LoadCategoriesAsync);
            PrepareNewCategoryCommand = new RelayCommand(PrepareFormForNewCategory);
            AddNewCategoryCommand = new AsyncRelayCommand(ExecuteAddCategoryAsync, CanExecuteAddOrSave);
            SaveChangesCommand = new AsyncRelayCommand(ExecuteUpdateCategoryAsync, CanExecuteAddOrSave);
            DeleteCategoryCommand = new AsyncRelayCommand(ExecuteDeleteCategoryAsync, CanExecuteDelete);
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit);

            // Inițializăm ObservableValidator
            ValidateAllProperties();
        }

        // Acest "partial method" este apelat automat de [ObservableProperty] când SelectedCategorie se schimbă
        partial void OnSelectedCategorieChanged(Categorie? value)
        {
            if (value != null)
            {
                IsEditMode = true;
                _originalCategorie = new Categorie // Copiem valorile originale pentru anulare
                {
                    CategorieID = value.CategorieID,
                    Nume = value.Nume,
                    EsteActiv = value.EsteActiv
                };
                FormNume = value.Nume;
                FormEsteActiv = value.EsteActiv;
            }
            else
            {
                PrepareFormForNewCategory(); // Dacă deselectăm, pregătim pentru adăugare
            }
            // Notificăm comenzile care depind de SelectedCategorie
            DeleteCategoryCommand.NotifyCanExecuteChanged();
            SaveChangesCommand.NotifyCanExecuteChanged(); // CanExecuteAddOrSave depinde și de IsEditMode
            AddNewCategoryCommand.NotifyCanExecuteChanged();
        }


        private async Task LoadCategoriesAsync()
        {
            var categoriesList = await _categorieRepository.GetAllActiveAsync(); // Sau toate, dacă vrem să le activăm/dezactivăm
            Categorii.Clear();
            foreach (var cat in categoriesList.OrderBy(c => c.Nume))
            {
                Categorii.Add(cat);
            }
            PrepareFormForNewCategory(); // Resetează formularul după încărcare
        }

        private void PrepareFormForNewCategory()
        {
            IsEditMode = false;
            SelectedCategorie = null; // Deselectează orice categorie din listă
            _originalCategorie = null;
            FormNume = string.Empty;
            FormEsteActiv = true;
            ClearErrors(); // Șterge erorile de validare de pe formular
            AddNewCategoryCommand.NotifyCanExecuteChanged();
            SaveChangesCommand.NotifyCanExecuteChanged();
        }

        private bool CanExecuteAddOrSave()
        {
            // Verifică dacă formularul este valid
            ValidateAllProperties(); // Forțează validarea
            if (HasErrors) return false;

            if (IsEditMode)
            {
                return SelectedCategorie != null && // Trebuie să avem o categorie selectată
                       !string.IsNullOrWhiteSpace(FormNume) && // Numele nu e gol
                       (_originalCategorie != null && // Avem originalul pentru comparație
                        (FormNume != _originalCategorie.Nume || FormEsteActiv != _originalCategorie.EsteActiv)); // Și ceva s-a schimbat
            }
            else // Mod Adăugare
            {
                return !string.IsNullOrWhiteSpace(FormNume);
            }
        }

        private async Task ExecuteAddCategoryAsync()
        {
            if (!CanExecuteAddOrSave()) return;

            if (await _categorieRepository.NameExistsAsync(FormNume))
            {
                MessageBox.Show($"Categoria '{FormNume}' există deja.", "Nume Duplicat", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newCategory = new Categorie
            {
                Nume = FormNume,
                EsteActiv = FormEsteActiv // Deși în repository e default true, o setăm explicit
            };
            await _categorieRepository.AddAsync(newCategory);
            await LoadCategoriesAsync(); // Reîncarcă și resetează formularul
        }

        private async Task ExecuteUpdateCategoryAsync()
        {
            if (!CanExecuteAddOrSave() || SelectedCategorie == null || _originalCategorie == null) return;

            // Verifică dacă numele nou (dacă s-a schimbat) există deja pentru alt ID
            if (FormNume != _originalCategorie.Nume && await _categorieRepository.NameExistsAsync(FormNume, SelectedCategorie.CategorieID))
            {
                MessageBox.Show($"Categoria '{FormNume}' există deja.", "Nume Duplicat", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Actualizăm direct obiectul SelectedCategorie cu noile valori din formular
            // Acest lucru e OK dacă SelectedCategorie este chiar obiectul din colecția _categorii.
            // Dar pentru o separare mai bună și pentru a folosi _originalCategorie, creăm un obiect actualizat.
            var updatedCategory = new Categorie
            {
                CategorieID = SelectedCategorie.CategorieID,
                Nume = FormNume,
                EsteActiv = FormEsteActiv
            };

            await _categorieRepository.UpdateAsync(updatedCategory);
            await LoadCategoriesAsync(); // Reîncarcă și resetează formularul
        }


        private bool CanExecuteDelete()
        {
            return SelectedCategorie != null;
        }

        private async Task ExecuteDeleteCategoryAsync()
        {
            if (!CanExecuteDelete() || SelectedCategorie == null) return;

            var result = MessageBox.Show($"Sigur doriți să ștergeți (marcați ca inactivă) categoria '{SelectedCategorie.Nume}'?",
                                         "Confirmare Ștergere", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await _categorieRepository.DeleteAsync(SelectedCategorie.CategorieID);
                await LoadCategoriesAsync(); // Reîncarcă și resetează formularul
            }
        }

        private void ExecuteCancelEdit()
        {
            // Resetează formularul la starea de adăugare nouă
            // Sau, dacă eram în mod editare și avem _originalCategorie, putem reveni la acele valori
            // if (IsEditMode && _originalCategorie != null)
            // {
            //     FormNume = _originalCategorie.Nume;
            //     FormEsteActiv = _originalCategorie.EsteActiv;
            //     // Poate deselectăm SelectedCategorie sau lăsăm așa pentru a putea re-edita
            // }
            // else
            // {
            //     PrepareFormForNewCategory();
            // }
            // Pentru simplitate, acum doar resetăm la "new"
            PrepareFormForNewCategory();
        }

        public async Task InitializeAsync()
        {
            await LoadCategoriesAsync();
        }
    }
}