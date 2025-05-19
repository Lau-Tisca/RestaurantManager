using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagerApp.DataAccess;
using RestaurantManagerApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel; // Pentru DesignerProperties
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // Pentru MessageBox
using System.Collections.Generic; // Necesar pentru List<T>
using Microsoft.Win32; // Pentru OpenFileDialog
using System.IO;       // Pentru Path
using System;          // Pentru AppDomain

namespace RestaurantManagerApp.ViewModels
{
    public partial class PreparatManagementViewModel : ObservableValidator
    {
        private readonly IPreparatRepository _preparatRepository;
        private readonly ICategorieRepository _categorieRepository; // Necesar pentru a încărca lista de categorii
        private readonly IAlergenRepository _alergenRepository;     // Necesar pentru a încărca lista de alergeni

        private Preparat? _originalPreparat;

        // --- Liste pentru selecții în UI ---
        [ObservableProperty]
        private ObservableCollection<Categorie> _listaCategoriiDisponibile;

        [ObservableProperty]
        private ObservableCollection<AlergenWrapper> _listaAlergeniDisponibili; // Vom folosi un wrapper pentru CheckBox

        // --- Proprietăți pentru formularul de adăugare/editare Preparat ---
        [ObservableProperty]
        private ObservableCollection<Preparat> _preparate; // Lista de preparate afișate

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeletePreparatCommand))]
        private Preparat? _selectedPreparat;

        // Câmpurile din formular
        [ObservableProperty]
        [Required(ErrorMessage = "Denumirea preparatului este obligatorie.")]
        [MaxLength(200, ErrorMessage = "Denumirea nu poate depăși 200 de caractere.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(AddNewPreparatCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private string _formDenumire = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Prețul este obligatoriu.")]
        [Range(0.01, 10000, ErrorMessage = "Prețul trebuie să fie între 0.01 și 10000.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(AddNewPreparatCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private decimal _formPret; // Sau string și convertești/validezi

        [ObservableProperty]
        [Required(ErrorMessage = "Cantitatea porției este obligatorie.")]
        [MaxLength(50, ErrorMessage = "Cantitatea porției nu poate depăși 50 de caractere.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(AddNewPreparatCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private string _formCantitatePortie = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Stocul total este obligatoriu.")]
        [Range(0, 100000, ErrorMessage = "Stocul trebuie să fie între 0 și 100000.")] // Ajustează limitele
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(AddNewPreparatCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private decimal _formCantitateTotalaStoc;

        [ObservableProperty]
        [Required(ErrorMessage = "Unitatea de măsură pentru stoc este obligatorie.")]
        [MaxLength(20, ErrorMessage = "Unitatea de măsură nu poate depăși 20 caractere.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(AddNewPreparatCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private string _formUnitateMasuraStoc = "g";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewPreparatCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private Categorie? _formSelectedCategorie; // Categoria selectată în ComboBox

        // Valoare ascunsă sau doar pentru validare, dacă _formSelectedCategorie poate fi null
        private int? _formSelectedCategorieId;
        [Required(ErrorMessage = "Categoria este obligatorie.")]
        public int? FormSelectedCategorieId
        {
            get => _formSelectedCategorie?.CategorieID; // Obține ID-ul din obiectul selectat
            // Nu avem nevoie de setter dacă nu vrem să-l setăm direct prin ID
        }


        [ObservableProperty]
        [MaxLength(255, ErrorMessage = "Calea imaginii nu poate depăși 255 de caractere.")]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private string? _formCaleImagine;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private string? _formDescriere;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private bool _formEsteActiv = true;

        [ObservableProperty]
        private bool _isEditMode = false;
        public bool IsAddMode => !IsEditMode;

        // --- Comenzi ---
        public IAsyncRelayCommand LoadInitialDataCommand { get; } // Încarcă preparate, categorii, alergeni
        public IRelayCommand PrepareNewPreparatCommand { get; }
        public IAsyncRelayCommand AddNewPreparatCommand { get; }
        public IAsyncRelayCommand SaveChangesCommand { get; }
        public IAsyncRelayCommand DeletePreparatCommand { get; }
        public IRelayCommand CancelEditCommand { get; }
        public IRelayCommand SelectImageCommand { get; }


        // --- Constructor pentru Design Time ---
        public PreparatManagementViewModel()
        {
            System.Diagnostics.Debug.WriteLine("PreparatManagementViewModel DesignTime Constructor Called");
            _listaCategoriiDisponibile = new ObservableCollection<Categorie>
            {
                new Categorie { CategorieID = 1, Nume = "Categorie Design 1" },
                new Categorie { CategorieID = 2, Nume = "Categorie Design 2" }
            };
            _listaAlergeniDisponibili = new ObservableCollection<AlergenWrapper>
            {
                new AlergenWrapper(new Alergen { AlergenID = 1, Nume = "Alergen Design 1" }) { IsSelected = true },
                new AlergenWrapper(new Alergen { AlergenID = 2, Nume = "Alergen Design 2" }) { IsSelected = false }
            };
            _preparate = new ObservableCollection<Preparat>
            {
                new Preparat { Denumire = "Preparat Design 1", Pret = 10.5m, Categorie = _listaCategoriiDisponibile[0] }
            };
            _formDenumire = "Denumire Test";
            _formPret = 12.34m;
            _formSelectedCategorie = _listaCategoriiDisponibile[0];

            // Inițializează comenzile cu implementări sigure
            LoadInitialDataCommand = new AsyncRelayCommand(async () => await Task.CompletedTask);
            PrepareNewPreparatCommand = new RelayCommand(() => { });
            AddNewPreparatCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => true);
            SaveChangesCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => true);
            DeletePreparatCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => true);
            CancelEditCommand = new RelayCommand(() => { });
            SelectImageCommand = new RelayCommand(() => { });
        }

        // --- Constructor pentru Runtime ---
        public PreparatManagementViewModel(IPreparatRepository preparatRepository,
                                         ICategorieRepository categorieRepository,
                                         IAlergenRepository alergenRepository)
        {
            System.Diagnostics.Debug.WriteLine("PreparatManagementViewModel Runtime Constructor Called");
            _preparatRepository = preparatRepository;
            _categorieRepository = categorieRepository;
            _alergenRepository = alergenRepository;

            _preparate = new ObservableCollection<Preparat>();
            _listaCategoriiDisponibile = new ObservableCollection<Categorie>();
            _listaAlergeniDisponibili = new ObservableCollection<AlergenWrapper>();
            _listaAlergeniDisponibili.CollectionChanged += ListaAlergeniDisponibili_CollectionChanged;

            LoadInitialDataCommand = new AsyncRelayCommand(LoadInitialDataAsync);
            PrepareNewPreparatCommand = new RelayCommand(ExecutePrepareNewPreparat);
            AddNewPreparatCommand = new AsyncRelayCommand(ExecuteAddPreparatAsync, CanExecuteAddOrSave);
            SaveChangesCommand = new AsyncRelayCommand(ExecuteUpdatePreparatAsync, CanExecuteAddOrSave);
            DeletePreparatCommand = new AsyncRelayCommand(ExecuteDeletePreparatAsync, CanExecuteDelete);
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit);
            SelectImageCommand = new RelayCommand(ExecuteSelectImage);

            ExecutePrepareNewPreparat(); // Setează starea inițială a formularului
            ValidateAllProperties();
        }

        // --- Metode pentru Comenzi și Logică ---

        private void ListaAlergeniDisponibili_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (object? item in e.NewItems) // Folosește object? și verifică tipul
                {
                    if (item is AlergenWrapper newItem)
                    {
                        newItem.PropertyChanged += AlergenWrapper_PropertyChanged;
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (object? item in e.OldItems) // Folosește object? și verifică tipul
                {
                    if (item is AlergenWrapper oldItem)
                    {
                        oldItem.PropertyChanged -= AlergenWrapper_PropertyChanged;
                    }
                }
            }
            // Este o idee bună să notificăm comanda și aici,
            // deși schimbarea IsSelected va face asta prin AlergenWrapper_PropertyChanged
            SaveChangesCommand.NotifyCanExecuteChanged();
        }

        private void AlergenWrapper_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AlergenWrapper.IsSelected))
            {
                //System.Diagnostics.Debug.WriteLine($"AlergenWrapper IsSelected changed. Notifying SaveChangesCommand.");
                SaveChangesCommand.NotifyCanExecuteChanged();
                // Poți notifica și AddNewPreparatCommand dacă este relevant
                // AddNewPreparatCommand.NotifyCanExecuteChanged(); 
            }
        }

        partial void OnIsEditModeChanged(bool value)
        {
            OnPropertyChanged(nameof(IsAddMode));
            AddNewPreparatCommand.NotifyCanExecuteChanged();
            SaveChangesCommand.NotifyCanExecuteChanged();
        }

        // Listener pentru _formSelectedCategorie pentru a actualiza FormSelectedCategorieId și a valida
        partial void OnFormSelectedCategorieChanged(Categorie? value)
        {
            // Forțează revalidarea proprietății FormSelectedCategorieId
            OnPropertyChanged(nameof(FormSelectedCategorieId));
            ValidateProperty(FormSelectedCategorieId, nameof(FormSelectedCategorieId));
            AddNewPreparatCommand.NotifyCanExecuteChanged();
            SaveChangesCommand.NotifyCanExecuteChanged();
        }


        partial void OnSelectedPreparatChanged(Preparat? value)
        {
            if (value != null)
            {
                IsEditMode = true;
                _originalPreparat = new Preparat // Copiem valorile originale
                {
                    PreparatID = value.PreparatID,
                    Denumire = value.Denumire,
                    Pret = value.Pret,
                    CantitatePortie = value.CantitatePortie,
                    CantitateTotalaStoc = value.CantitateTotalaStoc,
                    UnitateMasuraStoc = value.UnitateMasuraStoc,
                    CategorieID = value.CategorieID,
                    Descriere = value.Descriere,
                    CaleImagine = value.CaleImagine,
                    EsteActiv = value.EsteActiv,
                    // Copiem și alergenii originali pentru comparație/resetare
                    Alergeni = value.Alergeni.Select(a => new Alergen { AlergenID = a.AlergenID, Nume = a.Nume }).ToList() ?? new List<Alergen>()
                };

                // Populăm formularul
                FormDenumire = value.Denumire;
                FormPret = value.Pret;
                FormCantitatePortie = value.CantitatePortie;
                FormCantitateTotalaStoc = value.CantitateTotalaStoc;
                FormUnitateMasuraStoc = value.UnitateMasuraStoc;
                FormSelectedCategorie = ListaCategoriiDisponibile.FirstOrDefault(c => c.CategorieID == value.CategorieID);
                FormDescriere = value.Descriere;
                FormCaleImagine = value.CaleImagine;
                FormEsteActiv = value.EsteActiv;

                ClearErrors();
                ValidateAllProperties();
                //System.Diagnostics.Debug.WriteLine($"După populare și validare în OnSelectedPreparatChanged, HasErrors: {HasErrors}");
                if (HasErrors)
                {
                    // Loghează care proprietăți au erori
                    var errors = GetErrors(); // Sau GetErrors(null) pentru toate proprietățile
                    foreach (var err in errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Eroare pe '{err.MemberNames.FirstOrDefault()}': {string.Join(", ", err.ErrorMessage)}");
                    }
                }

                // Selectăm alergenii în lista _listaAlergeniDisponibili
                foreach (var alergenWrapper in _listaAlergeniDisponibili)
                {
                    alergenWrapper.IsSelected = value.Alergeni.Any(a => a.AlergenID == alergenWrapper.Alergen.AlergenID);
                }

                ClearErrors();
                ValidateAllProperties();

                //System.Diagnostics.Debug.WriteLine($"După populare și validare în OnSelectedPreparatChanged, HasErrors: {HasErrors}");
                if (HasErrors)
                {
                    var errors = GetErrors(null);
                    foreach (var err in errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"  EROARE (OnSelectedPreparatChanged) pe '{err.MemberNames.FirstOrDefault()}': {err.ErrorMessage}");
                    }
                }
            }

            else
            {
                if (IsEditMode) ExecutePrepareNewPreparat();
            }
        }


        private async Task LoadInitialDataAsync()
        {
            if (_categorieRepository == null || _alergenRepository == null) return;

            // Încarcă categoriile disponibile
            var categorii = await _categorieRepository.GetAllActiveAsync();

            ListaCategoriiDisponibile.Clear();
            foreach (var cat in categorii.OrderBy(c => c.Nume))
            {
                ListaCategoriiDisponibile.Add(cat);
            }

            var alergeni = await _alergenRepository.GetAllActiveAsync();
            // Dezabonează-te de la evenimentele vechilor iteme înainte de Clear
            foreach (var wrapper in ListaAlergeniDisponibili)
            {
                wrapper.PropertyChanged -= AlergenWrapper_PropertyChanged;
            }
            ListaAlergeniDisponibili.Clear();

            foreach (var al in alergeni.OrderBy(a => a.Nume))
            {
                var wrapper = new AlergenWrapper(al);
                wrapper.PropertyChanged += AlergenWrapper_PropertyChanged; // Abonează-te la noul item
                ListaAlergeniDisponibili.Add(wrapper);
            }

            // Încarcă lista de preparate
            await LoadPreparateAsync();
        }

        private async Task LoadPreparateAsync()
        {
            if (_preparatRepository == null) return;    
            var preparateList = await _preparatRepository.GetAllActiveWithDetailsAsync(); // Cu detalii pentru afișare
            Preparate.Clear();
            foreach (var p in preparateList.OrderBy(p => p.Denumire))
            {
                Preparate.Add(p);
            }
            ExecutePrepareNewPreparat(); // Resetează formularul
        }

        private void ExecutePrepareNewPreparat()
        {
            IsEditMode = false;
            SelectedPreparat = null;
            _originalPreparat = null;

            FormDenumire = string.Empty;
            FormPret = 0;
            FormCantitatePortie = string.Empty;
            FormCantitateTotalaStoc = 0;
            FormUnitateMasuraStoc = "g";
            FormSelectedCategorie = null; // Sau ListaCategoriiDisponibile.FirstOrDefault();
            FormDescriere = string.Empty;
            FormCaleImagine = string.Empty;
            FormEsteActiv = true;

            // Deselectează toți alergenii
            foreach (var wrapper in ListaAlergeniDisponibili)
            {
                wrapper.IsSelected = false;
            }
            ClearErrors();
        }

        private List<int> GetSelectedAlergenIds()
        {
            return ListaAlergeniDisponibili.Where(aw => aw.IsSelected).Select(aw => aw.Alergen.AlergenID).ToList();
        }

        private bool CanExecuteAddOrSave()
        {
            // Setează valoarea pentru proprietatea de validare a categoriei
            OnPropertyChanged(nameof(FormSelectedCategorieId));
            ValidateAllProperties(); // Include și validarea pentru FormSelectedCategorieId
            if (HasErrors) return false;

            if (IsEditMode)
            {
                if (SelectedPreparat == null || _originalPreparat == null) return false;
                // Verifică dacă s-a schimbat ceva
                bool
                formChanged = FormDenumire != _originalPreparat.Denumire ||
                                  FormPret != _originalPreparat.Pret ||
                                  FormCantitatePortie != _originalPreparat.CantitatePortie ||
                                  FormCantitateTotalaStoc != _originalPreparat.CantitateTotalaStoc ||
                                  FormUnitateMasuraStoc != _originalPreparat.UnitateMasuraStoc ||
                                  FormSelectedCategorie?.CategorieID != _originalPreparat.CategorieID ||
                                  FormDescriere != _originalPreparat.Descriere ||
                                  FormCaleImagine != _originalPreparat.CaleImagine ||
                                  FormEsteActiv != _originalPreparat.EsteActiv;

                var selectedOriginalAlergenIds = _originalPreparat.Alergeni.Select(a => a.AlergenID).OrderBy(id => id).ToList();
                var currentSelectedAlergenIds = GetSelectedAlergenIds().OrderBy(id => id).ToList();
                bool alergeniChanged = !selectedOriginalAlergenIds.SequenceEqual(currentSelectedAlergenIds);

                return formChanged || alergeniChanged;
            }
            else // Mod Adăugare
            {
                return !string.IsNullOrWhiteSpace(FormDenumire) && FormSelectedCategorie != null; // Și alte câmpuri obligatorii
            }
        }

        //CanExecuteAddOrSave cu debug code
        //private bool CanExecuteAddOrSave()
        //{
        //    // La începutul metodei, forțăm validarea pentru a avea starea HasErrors actualizată
        //    OnPropertyChanged(nameof(FormSelectedCategorieId)); // Asigură că validarea pt categorie rulează
        //    ValidateAllProperties();
        //    if (HasErrors)
        //    {
        //        System.Diagnostics.Debug.WriteLine("CanExecuteAddOrSave: HasErrors este true. Returnează false.");
        //        var errors = GetErrors(null); // Obține toate erorile de validare din ViewModel
        //        foreach (var error in errors)
        //        {
        //            foreach (var memberName in error.MemberNames)
        //            {
        //                System.Diagnostics.Debug.WriteLine($"  EROARE DE VALIDARE pe '{memberName}': {error.ErrorMessage}");
        //            }
        //        }
        //        return false;
        //    }

        //    if (IsEditMode)
        //    {
        //        System.Diagnostics.Debug.WriteLine("CanExecuteAddOrSave: IsEditMode este true.");
        //        if (SelectedPreparat == null || _originalPreparat == null)
        //        {
        //            System.Diagnostics.Debug.WriteLine("CanExecuteAddOrSave: SelectedPreparat sau _originalPreparat este null. Returnează false.");
        //            return false;
        //        }

        //        // Verificăm fiecare câmp individual și logăm starea lui
        //        bool denumireChanged = FormDenumire != _originalPreparat.Denumire;
        //        bool pretChanged = FormPret != _originalPreparat.Pret;
        //        bool cantitatePortieChanged = FormCantitatePortie != _originalPreparat.CantitatePortie;
        //        bool stocChanged = FormCantitateTotalaStoc != _originalPreparat.CantitateTotalaStoc;
        //        bool unitateMasuraChanged = FormUnitateMasuraStoc != _originalPreparat.UnitateMasuraStoc;
        //        bool categorieChanged = FormSelectedCategorie?.CategorieID != _originalPreparat.CategorieID; // Atenție la null aici
        //        bool descriereChanged = FormDescriere != _originalPreparat.Descriere;
        //        bool caleImagineChanged = FormCaleImagine != _originalPreparat.CaleImagine;
        //        bool esteActivChanged = FormEsteActiv != _originalPreparat.EsteActiv;

        //        System.Diagnostics.Debug.WriteLine($"  Denumire: '{FormDenumire}' vs '{_originalPreparat.Denumire}' -> Changed: {denumireChanged}");
        //        System.Diagnostics.Debug.WriteLine($"  Pret: {FormPret} vs {_originalPreparat.Pret} -> Changed: {pretChanged}");
        //        System.Diagnostics.Debug.WriteLine($"  CantitatePortie: '{FormCantitatePortie}' vs '{_originalPreparat.CantitatePortie}' -> Changed: {cantitatePortieChanged}");
        //        System.Diagnostics.Debug.WriteLine($"  Stoc: {FormCantitateTotalaStoc} vs {_originalPreparat.CantitateTotalaStoc} -> Changed: {stocChanged}");
        //        System.Diagnostics.Debug.WriteLine($"  Unitate: '{FormUnitateMasuraStoc}' vs '{_originalPreparat.UnitateMasuraStoc}' -> Changed: {unitateMasuraChanged}");
        //        System.Diagnostics.Debug.WriteLine($"  CategorieID: {FormSelectedCategorie?.CategorieID} vs {_originalPreparat.CategorieID} -> Changed: {categorieChanged}");
        //        System.Diagnostics.Debug.WriteLine($"  Descriere: '{FormDescriere}' vs '{_originalPreparat.Descriere}' -> Changed: {descriereChanged}"); // Atenție la null-uri aici
        //        System.Diagnostics.Debug.WriteLine($"  Imagine: '{FormCaleImagine}' vs '{_originalPreparat.CaleImagine}' -> Changed: {caleImagineChanged}"); // Atenție la null-uri
        //        System.Diagnostics.Debug.WriteLine($"  Activ: {FormEsteActiv} vs {_originalPreparat.EsteActiv} -> Changed: {esteActivChanged}");


        //        bool formChanged = denumireChanged || pretChanged || cantitatePortieChanged || stocChanged ||
        //                           unitateMasuraChanged || categorieChanged || descriereChanged ||
        //                           caleImagineChanged || esteActivChanged;

        //        // Logica pentru alergeni (presupunând că _originalPreparat.Alergeni este populat corect)
        //        var originalAlergenIds = _originalPreparat.Alergeni?.Select(a => a.AlergenID).OrderBy(id => id).ToList() ?? new List<int>();
        //        var currentAlergenIds = GetSelectedAlergenIds().OrderBy(id => id).ToList();
        //        bool alergeniChanged = !originalAlergenIds.SequenceEqual(currentAlergenIds);

        //        System.Diagnostics.Debug.WriteLine($"  FormChanged: {formChanged}");
        //        System.Diagnostics.Debug.WriteLine($"  AlergeniOriginali: [{string.Join(", ", originalAlergenIds)}]");
        //        System.Diagnostics.Debug.WriteLine($"  AlergeniCurenti: [{string.Join(", ", currentAlergenIds)}]");
        //        System.Diagnostics.Debug.WriteLine($"  AlergeniChanged: {alergeniChanged}");
        //        System.Diagnostics.Debug.WriteLine($"  Returnează: {formChanged || alergeniChanged}");

        //        System.Diagnostics.Debug.WriteLine($"CanExecuteAddOrSave (EditMode, No Global Errors): formChanged={formChanged}, alergeniChanged={alergeniChanged}. Returnează: {formChanged || alergeniChanged}");

        //        return formChanged || alergeniChanged;
        //    }
        //    else // Mod Adăugare
        //    {
        //        System.Diagnostics.Debug.WriteLine("CanExecuteAddOrSave: IsEditMode este false (Mod Adăugare).");
        //        // Asigură-te că FormSelectedCategorie.CategorieID != 0 verifică că nu e placeholder-ul
        //        bool canAdd = !string.IsNullOrWhiteSpace(FormDenumire) && FormSelectedCategorie != null && FormSelectedCategorie.CategorieID != 0;
        //        System.Diagnostics.Debug.WriteLine($"CanExecuteAddOrSave (AddMode, No Global Errors): canAdd={canAdd}. Returnează: {canAdd}");
        //        return canAdd;
        //    }
        //}

        private async Task ExecuteAddPreparatAsync()
        {
            if (!CanExecuteAddOrSave() || _preparatRepository == null || FormSelectedCategorie == null) return;
            if (await _preparatRepository.NameExistsAsync(FormDenumire))
            {
                MessageBox.Show($"Preparatul '{FormDenumire}' există deja.", "Nume Duplicat", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newPreparat = new Preparat
            {
                Denumire = FormDenumire,
                Pret = FormPret,
                CantitatePortie = FormCantitatePortie,
                CantitateTotalaStoc = FormCantitateTotalaStoc,
                UnitateMasuraStoc = FormUnitateMasuraStoc,
                CategorieID = FormSelectedCategorie.CategorieID, // Folosim ID-ul categoriei selectate
                Descriere = FormDescriere,
                CaleImagine = FormCaleImagine,
                EsteActiv = FormEsteActiv
            };
            await _preparatRepository.AddAsync(newPreparat, GetSelectedAlergenIds());
            await LoadInitialDataAsync(); // Reîncarcă tot, inclusiv preparatele
        }

        private async Task ExecuteUpdatePreparatAsync()
        {
            if (!CanExecuteAddOrSave() || SelectedPreparat == null || _originalPreparat == null || _preparatRepository == null || FormSelectedCategorie == null) return;
            if (FormDenumire != _originalPreparat.Denumire && await _preparatRepository.NameExistsAsync(FormDenumire, SelectedPreparat.PreparatID))
            {
                MessageBox.Show($"Preparatul '{FormDenumire}' există deja pentru un alt ID.", "Nume Duplicat", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var updatedPreparat = new Preparat
            {
                PreparatID = SelectedPreparat.PreparatID,
                Denumire = FormDenumire,
                Pret = FormPret,
                CantitatePortie = FormCantitatePortie,
                CantitateTotalaStoc = FormCantitateTotalaStoc,
                UnitateMasuraStoc = FormUnitateMasuraStoc,
                CategorieID = FormSelectedCategorie.CategorieID,
                Descriere = FormDescriere,
                CaleImagine = FormCaleImagine,
                EsteActiv = FormEsteActiv
                // Alergenii vor fi gestionați prin lista de ID-uri
            };
            await _preparatRepository.UpdateAsync(updatedPreparat, GetSelectedAlergenIds());
            await LoadInitialDataAsync();
        }


        private bool CanExecuteDelete()
        {
            return SelectedPreparat != null && IsEditMode;
        }

        private async Task ExecuteDeletePreparatAsync()
        {
            if (!CanExecuteDelete() || SelectedPreparat == null || _preparatRepository == null) return;
            var result = MessageBox.Show($"Sigur doriți să ștergeți (marcați ca inactiv) preparatul '{SelectedPreparat.Denumire}'?",
                                         "Confirmare Ștergere", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await _preparatRepository.DeleteAsync(SelectedPreparat.PreparatID);
                await LoadInitialDataAsync();
            }
        }

        private void ExecuteCancelEdit()
        {
            ExecutePrepareNewPreparat();
        }

        private void ExecuteSelectImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Selectează o Imagine pentru Preparat",
                Filter = "Fișiere Imagine (*.jpg; *.jpeg; *.png; *.gif; *.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Toate Fișierele (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };

            // Încercăm să setăm directorul inițial la un subfolder din proiect
            try
            {
                // Calea către directorul unde rulează executabilul
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                // Navigăm în sus pentru a ajunge la directorul soluției/proiectului (poate necesita ajustare)
                // Acest lucru este fragil și depinde de structura de directoare la build.
                // Pentru Debug, baseDirectory este bin\Debug\net8.0-windows.
                // Presupunem că ProductImages este la același nivel cu .csproj sau un nivel mai sus.

                // O abordare mai robustă pentru a găsi directorul proiectului la runtime poate fi complicată.
                // Pentru simplitate, vom încerca să creăm o cale relativă la executabil,
                // presupunând că folderul ProductImages va fi copiat în directorul de output
                // sau vom folosi o cale mai directă dacă știm unde vrem să fie.

                string projectImageFolderPath = Path.Combine(baseDirectory, "..", "..", "..", "ProductImages"); // Navighează 3 nivele în sus din bin/Debug/netX.Y
                projectImageFolderPath = Path.GetFullPath(projectImageFolderPath); // Normalizează calea

                if (Directory.Exists(projectImageFolderPath))
                {
                    openFileDialog.InitialDirectory = projectImageFolderPath;
                }
                else
                {
                    // Folderul nu există la calea așteptată, creăm o cale implicită sau lăsăm sistemul să decidă
                    string defaultUserPictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    if (Directory.Exists(defaultUserPictures))
                    {
                        openFileDialog.InitialDirectory = defaultUserPictures;
                    }
                    // Poți crea folderul ProductImages dacă nu există aici, dar e mai bine să fie parte din structura proiectului
                    // Directory.CreateDirectory(projectImageFolderPath);
                }
            }
            catch (Exception ex)
            {
                // Loghează eroarea dacă e cazul, nu bloca dialogul
                System.Diagnostics.Debug.WriteLine($"Eroare la setarea InitialDirectory: {ex.Message}");
                // Continuă cu directorul default al sistemului
            }


            if (openFileDialog.ShowDialog() == true)
            {
                // Utilizatorul a selectat un fișier
                string selectedFilePath = openFileDialog.FileName;

                // --- Opțional: Copierea imaginii în folderul ProductImages și stocarea căii relative ---
                // Acest pas adaugă complexitate: trebuie să te asiguri că folderul ProductImages
                // este inclus în build și deploy, și că ai permisiuni de scriere.
                try
                {
                    string productImagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductImages");
                    if (!Directory.Exists(productImagesDir))
                    {
                        Directory.CreateDirectory(productImagesDir);
                    }

                    string fileName = Path.GetFileName(selectedFilePath);
                    string destinationPath = Path.Combine(productImagesDir, fileName);

                    // Verifică dacă fișierul există deja pentru a nu suprascrie fără avertisment (sau generează nume unic)
                    if (File.Exists(destinationPath) && selectedFilePath != destinationPath)
                    {
                        // Poți întreba utilizatorul dacă vrea să suprascrie sau să generezi un nume nou
                        // Pentru simplitate, vom suprascrie sau vom adăuga un timestamp/guid la nume
                        string extension = Path.GetExtension(fileName);
                        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                        destinationPath = Path.Combine(productImagesDir, $"{nameWithoutExtension}_{Guid.NewGuid().ToString().Substring(0, 4)}{extension}");
                    }

                    if (selectedFilePath != destinationPath) // Copiază doar dacă nu e deja în folderul destinație
                    {
                        File.Copy(selectedFilePath, destinationPath, true); // true pentru a suprascrie dacă există cu același nume (după generarea numelui unic)
                    }


                    // Stochează o cale relativă în ViewModel, dacă folderul ProductImages
                    // este relativ la executabilul aplicației.
                    // Sau stochează doar numele fișierului dacă toate imaginile sunt într-un singur folder cunoscut.
                    FormCaleImagine = Path.Combine("ProductImages", Path.GetFileName(destinationPath)); // Ex: "ProductImages/nume_imagine.jpg"
                                                                                                        // Sau doar Path.GetFileName(destinationPath)
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Eroare la copierea imaginii: {ex.Message}", "Eroare Imagine", MessageBoxButton.OK, MessageBoxImage.Error);
                    FormCaleImagine = selectedFilePath; // Revine la calea completă dacă copierea eșuează
                }
                // --- Sfârșit secțiune opțională de copiere ---

                // Dacă nu faci copierea, pur și simplu setezi calea completă:
                // FormCaleImagine = selectedFilePath;
            }
        }

        // Metoda de inițializare principală
        public async Task InitializeAsync()
        {
            await LoadInitialDataAsync();
        }
    }

    // Clasa Wrapper pentru Alergeni, pentru a permite binding la IsSelected într-un ListBox cu CheckBox-uri
    public partial class AlergenWrapper : ObservableObject
    {
        [ObservableProperty]
        private Alergen _alergen;

        [ObservableProperty]
        private bool _isSelected;

        public AlergenWrapper(Alergen alergen)
        {
            _alergen = alergen ?? throw new ArgumentNullException(nameof(alergen));
            _isSelected = false;
        }
    }
}