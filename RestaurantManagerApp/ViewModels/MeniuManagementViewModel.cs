using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RestaurantManagerApp.DataAccess;
using RestaurantManagerApp.Models;
using RestaurantManagerApp.Utils; // Pentru ConfigurationHelper și ApplicationSettings
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RestaurantManagerApp.ViewModels
{
    // Wrapper pentru un preparat selectabil pentru a fi adăugat într-un meniu
    // și pentru a afișa/edita preparatele deja în meniul curent.
    public partial class MeniuComponentaViewModel : ObservableObject
    {
        [ObservableProperty]
        private Preparat _preparatOriginal; // Referința la obiectul Preparat original

        [ObservableProperty]
        private string _cantitateInMeniu = "1"; // Default la 1

        [ObservableProperty]
        private string _unitateMasura = "buc"; // Default

        public int PreparatID => PreparatOriginal.PreparatID;
        public string DenumirePreparat => PreparatOriginal.Denumire;
        public decimal PretPreparat => PreparatOriginal.Pret;

        // Constructor pentru când adăugăm un preparat NOU în meniul curent
        public MeniuComponentaViewModel(Preparat preparatOriginal)
        {
            _preparatOriginal = preparatOriginal;
            // Preia unitatea de măsură default a preparatului dacă e relevant, altfel lasă "buc"
            // _unitateMasura = preparatOriginal.UnitateMasuraStoc; // Exemplu
        }

        // Constructor pentru când încărcăm un MeniuPreparat EXISTENT
        public MeniuComponentaViewModel(MeniuPreparat meniuPreparatExistent)
        {
            _preparatOriginal = meniuPreparatExistent.Preparat; // Asigură-te că Preparat e încărcat
            _cantitateInMeniu = meniuPreparatExistent.CantitateInMeniu;
            _unitateMasura = meniuPreparatExistent.UnitateMasuraCantitateInMeniu;
        }
    }


    public partial class MeniuManagementViewModel : ObservableValidator
    {
        private readonly IMeniuRepository _meniuRepository;
        private readonly ICategorieRepository _categorieRepository;
        private readonly IPreparatRepository _preparatRepository; // Pentru a încărca lista de preparate disponibile
        private readonly ApplicationSettings _appSettings;


        private Meniu? _originalMeniu;

        // --- Liste pentru UI ---
        [ObservableProperty]
        private ObservableCollection<Meniu> _meniuri; // Lista de meniuri principale afișate

        [ObservableProperty]
        private ObservableCollection<Categorie> _listaCategoriiDisponibile;

        [ObservableProperty]
        private ObservableCollection<Preparat> _listaPreparateDisponibile; // Toate preparatele active

        // Lista de componente pentru meniul curent din formular
        [ObservableProperty]
        private ObservableCollection<MeniuComponentaViewModel> _formComponenteMeniu;


        // --- Proprietăți pentru formularul de adăugare/editare Meniu ---
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteMeniuCommand))]
        private Meniu? _selectedMeniu;

        [ObservableProperty]
        [Required(ErrorMessage = "Denumirea meniului este obligatorie.")]
        [MaxLength(200, ErrorMessage = "Denumirea nu poate depăși 200 de caractere.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(AddNewMeniuCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private string _formDenumire = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewMeniuCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private Categorie? _formSelectedCategorie;

        [Required(ErrorMessage = "Categoria este obligatorie.")]
        public int? FormSelectedCategorieId => _formSelectedCategorie?.CategorieID;

        [ObservableProperty]
        [MaxLength(255, ErrorMessage = "Calea imaginii nu poate depăși 255 de caractere.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private string? _formCaleImagine;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private string? _formDescriere;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private bool _formEsteActiv = true;

        // Proprietate pentru ComboBox-ul de selecție a preparatelor de adăugat
        [ObservableProperty]
        private Preparat? _formSelectedPreparatPentruAdaugare;

        // Proprietate pentru prețul calculat al meniului (cu discount)
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private decimal _formPretCalculatMeniu;


        [ObservableProperty]
        private bool _isEditMode = false;
        public bool IsAddMode => !IsEditMode;

        // --- Comenzi ---
        public IAsyncRelayCommand LoadInitialDataCommand { get; }
        public IRelayCommand PrepareNewMeniuCommand { get; }
        public IAsyncRelayCommand AddNewMeniuCommand { get; }
        public IAsyncRelayCommand SaveChangesCommand { get; }
        public IAsyncRelayCommand DeleteMeniuCommand { get; }
        public IRelayCommand CancelEditCommand { get; }
        public IRelayCommand AddComponentaCommand { get; }
        public IRelayCommand<MeniuComponentaViewModel> RemoveComponentaCommand { get; }
        public IRelayCommand RecalculatePriceCommand { get; }
        public IRelayCommand SelectMeniuImageCommand { get; }


        // --- Constructor pentru Design Time ---
        public MeniuManagementViewModel()
        {
            System.Diagnostics.Debug.WriteLine("MeniuManagementViewModel DesignTime Constructor Called");
            _appSettings = new ApplicationSettings { MenuDiscountPercentageX = 10 }; // Valoare mock
            _meniuri = new ObservableCollection<Meniu>();
            _listaCategoriiDisponibile = new ObservableCollection<Categorie> { new Categorie { CategorieID = 0, Nume = "(Alegeți)" }, new Categorie { Nume = "Design Cat Meniu" } };
            _listaPreparateDisponibile = new ObservableCollection<Preparat> { new Preparat { Denumire = "Design Preparat Pt Meniu", Pret = 10 } };
            _formComponenteMeniu = new ObservableCollection<MeniuComponentaViewModel> { new MeniuComponentaViewModel(_listaPreparateDisponibile[0]) { CantitateInMeniu = "2" } };
            _formDenumire = "Meniu Design";
            _formSelectedCategorie = _listaCategoriiDisponibile[0];
            RecalculateAndSetMenuPrice();

            LoadInitialDataCommand = new AsyncRelayCommand(async () => await Task.CompletedTask);
            PrepareNewMeniuCommand = new RelayCommand(() => { });
            AddNewMeniuCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => true);
            SaveChangesCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => true);
            DeleteMeniuCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => true);
            CancelEditCommand = new RelayCommand(() => { });
            AddComponentaCommand = new RelayCommand(() => { }, () => FormSelectedPreparatPentruAdaugare != null);
            RemoveComponentaCommand = new RelayCommand<MeniuComponentaViewModel>((comp) => { if (comp != null) FormComponenteMeniu.Remove(comp); }, (comp) => comp != null);
            RecalculatePriceCommand = new RelayCommand(RecalculateAndSetMenuPrice);
            SelectMeniuImageCommand = new RelayCommand(() => { });
        }

        // --- Constructor pentru Runtime ---
        public MeniuManagementViewModel(IMeniuRepository meniuRepository,
                                      ICategorieRepository categorieRepository,
                                      IPreparatRepository preparatRepository)
        {
            System.Diagnostics.Debug.WriteLine("MeniuManagementViewModel Runtime Constructor Called");
            _meniuRepository = meniuRepository;
            _categorieRepository = categorieRepository;
            _preparatRepository = preparatRepository;
            _appSettings = ConfigurationHelper.GetApplicationSettings() ?? new ApplicationSettings(); // Ia din config

            _meniuri = new ObservableCollection<Meniu>();
            _listaCategoriiDisponibile = new ObservableCollection<Categorie>();
            _listaPreparateDisponibile = new ObservableCollection<Preparat>();
            _formComponenteMeniu = new ObservableCollection<MeniuComponentaViewModel>();
            _formComponenteMeniu.CollectionChanged += (s, e) => { RecalculateAndSetMenuPrice(); SaveChangesCommand.NotifyCanExecuteChanged(); AddNewMeniuCommand.NotifyCanExecuteChanged(); };


            LoadInitialDataCommand = new AsyncRelayCommand(LoadInitialDataAsync);
            PrepareNewMeniuCommand = new RelayCommand(ExecutePrepareNewMeniu);
            AddNewMeniuCommand = new AsyncRelayCommand(ExecuteAddMeniuAsync, CanExecuteAddOrSave);
            SaveChangesCommand = new AsyncRelayCommand(ExecuteUpdateMeniuAsync, CanExecuteAddOrSave);
            DeleteMeniuCommand = new AsyncRelayCommand(ExecuteDeleteMeniuAsync, CanExecuteDelete);
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit);
            AddComponentaCommand = new RelayCommand(ExecuteAddComponenta, CanExecuteAddComponenta);
            RemoveComponentaCommand = new RelayCommand<MeniuComponentaViewModel>(ExecuteRemoveComponenta, (comp) => comp != null);
            RecalculatePriceCommand = new RelayCommand(RecalculateAndSetMenuPrice);
            SelectMeniuImageCommand = new RelayCommand(ExecuteSelectMeniuImage);

            ExecutePrepareNewMeniu();
            ValidateAllProperties();
        }

        // --- Listeneri pentru proprietăți ---
        partial void OnIsEditModeChanged(bool value)
        {
            OnPropertyChanged(nameof(IsAddMode));
            AddNewMeniuCommand.NotifyCanExecuteChanged();
            SaveChangesCommand.NotifyCanExecuteChanged();
        }

        partial void OnFormSelectedCategorieChanged(Categorie? value)
        {
            OnPropertyChanged(nameof(FormSelectedCategorieId));
            ValidateProperty(FormSelectedCategorieId, nameof(FormSelectedCategorieId));
            AddNewMeniuCommand.NotifyCanExecuteChanged();
            SaveChangesCommand.NotifyCanExecuteChanged();
        }

        partial void OnFormSelectedPreparatPentruAdaugareChanged(Preparat? value)
        {
            AddComponentaCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedMeniuChanged(Meniu? value)
        {
            if (value != null) // Meniul selectat din lista principală
            {
                IsEditMode = true;
                // Asigură-te că MeniuPreparate și Preparat sunt încărcate (ar trebui să fie din GetByIdWithDetailsAsync)
                _originalMeniu = new Meniu
                {
                    MeniuID = value.MeniuID,
                    Denumire = value.Denumire,
                    CategorieID = value.CategorieID,
                    Descriere = value.Descriere,
                    CaleImagine = value.CaleImagine,
                    EsteActiv = value.EsteActiv,
                    MeniuPreparate = value.MeniuPreparate?
                        .Select(mp => new MeniuPreparat
                        {
                            PreparatID = mp.PreparatID,
                            CantitateInMeniu = mp.CantitateInMeniu,
                            UnitateMasuraCantitateInMeniu = mp.UnitateMasuraCantitateInMeniu,
                            Preparat = mp.Preparat // Copiază și referința la preparat
                        }).ToList() ?? new List<MeniuPreparat>()
                };

                FormDenumire = value.Denumire;
                FormSelectedCategorie = ListaCategoriiDisponibile.FirstOrDefault(c => c.CategorieID == value.CategorieID);
                FormDescriere = value.Descriere;
                FormCaleImagine = value.CaleImagine;
                FormEsteActiv = value.EsteActiv;

                FormComponenteMeniu.Clear();
                if (value.MeniuPreparate != null)
                {
                    foreach (var mp in value.MeniuPreparate)
                    {
                        // Important: mp.Preparat trebuie să fie încărcat de EF Core
                        if (mp.Preparat != null) FormComponenteMeniu.Add(new MeniuComponentaViewModel(mp));
                    }
                }
                RecalculateAndSetMenuPrice();
                ClearErrors();
            }
            else
            {
                if (IsEditMode) ExecutePrepareNewMeniu();
            }
        }

        // --- Metode de încărcare ---
        private async Task LoadInitialDataAsync()
        {
            if (_categorieRepository == null || _preparatRepository == null) return;

            var categorii = await _categorieRepository.GetAllActiveAsync();
            ListaCategoriiDisponibile.Clear();
            ListaCategoriiDisponibile.Add(new Categorie { CategorieID = 0, Nume = "(Alegeți Categoria)" });
            foreach (var cat in categorii.OrderBy(c => c.Nume)) { ListaCategoriiDisponibile.Add(cat); }

            var preparate = await _preparatRepository.GetAllActiveWithDetailsAsync(); // Avem nevoie de prețurile preparatelor
            ListaPreparateDisponibile.Clear();
            foreach (var p in preparate.OrderBy(pr => pr.Denumire)) { ListaPreparateDisponibile.Add(p); }

            await LoadMeniuriAsync();
        }

        private async Task LoadMeniuriAsync()
        {
            if (_meniuRepository == null) return;
            var meniuriList = await _meniuRepository.GetAllActiveWithDetailsAsync();
            Meniuri.Clear();
            foreach (var m in meniuriList.OrderBy(me => me.Denumire)) { Meniuri.Add(m); }
            ExecutePrepareNewMeniu();
        }

        // --- Logica formularului ---
        private void ExecutePrepareNewMeniu()
        {
            IsEditMode = false; SelectedMeniu = null; _originalMeniu = null;
            FormDenumire = string.Empty;
            FormSelectedCategorie = ListaCategoriiDisponibile.FirstOrDefault(c => c.CategorieID == 0) ?? ListaCategoriiDisponibile.FirstOrDefault();
            FormDescriere = string.Empty; FormCaleImagine = string.Empty; FormEsteActiv = true;
            FormComponenteMeniu.Clear(); // Golește lista de componente
            FormSelectedPreparatPentruAdaugare = null;
            RecalculateAndSetMenuPrice();
            ClearErrors();
        }

        private void RecalculateAndSetMenuPrice()
        {
            decimal subtotalComponente = 0;
            foreach (var comp in FormComponenteMeniu)
            {
                // Aici trebuie să convertim CantitateInMeniu la un număr, dacă e cazul
                // Presupunem că este deja un număr sau '1 buc' implică prețul întreg al preparatului
                // Pentru o implementare reală, parsarea lui CantitateInMeniu e necesară
                if (decimal.TryParse(comp.CantitateInMeniu.Replace("buc", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantitateDenumirerica))
                {
                    // Dacă unitatea de măsură din meniu e diferită de cea a preparatului, e nevoie de conversie
                    // Momentan, presupunem că e vorba de "bucăți" de preparat standard
                    subtotalComponente += comp.PreparatOriginal.Pret * cantitateDenumirerica;
                }
                else //Dacă nu se poate parsa, presupunem 1 * pretul preparatului
                {
                    subtotalComponente += comp.PreparatOriginal.Pret;
                }
            }

            decimal discount = _appSettings.MenuDiscountPercentageX; // ex: 10 pentru 10%
            FormPretCalculatMeniu = subtotalComponente * (1 - (discount / 100m));
        }


        // --- Logica pentru componentele meniului ---
        private bool CanExecuteAddComponenta() => FormSelectedPreparatPentruAdaugare != null;

        private void ExecuteAddComponenta()
        {
            if (!CanExecuteAddComponenta() || FormSelectedPreparatPentruAdaugare == null) return;

            // Verifică dacă preparatul nu este deja în listă
            if (!FormComponenteMeniu.Any(c => c.PreparatID == FormSelectedPreparatPentruAdaugare.PreparatID))
            {
                FormComponenteMeniu.Add(new MeniuComponentaViewModel(FormSelectedPreparatPentruAdaugare));
                // FormSelectedPreparatPentruAdaugare = null; // Opcional, resetează selecția
                // Nu mai notificăm SaveChangesCommand aici, se face prin CollectionChanged
            }
            else
            {
                MessageBox.Show("Acest preparat este deja adăugat în meniu.", "Preparat Duplicat", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecuteRemoveComponenta(MeniuComponentaViewModel? componenta)
        {
            if (componenta != null)
            {
                FormComponenteMeniu.Remove(componenta);
                // Nu mai notificăm SaveChangesCommand aici, se face prin CollectionChanged
            }
        }


        // --- Logica de Salvare/Adăugare/Ștergere Meniu ---
        private bool CanExecuteAddOrSave()
        {
            OnPropertyChanged(nameof(FormSelectedCategorieId));
            ValidateAllProperties();
            if (HasErrors) return false;
            if (!FormComponenteMeniu.Any()) // Un meniu trebuie să aibă cel puțin o componentă
            {
                // Poți seta o eroare specifică aici dacă vrei
                System.Diagnostics.Debug.WriteLine("CanExecuteAddOrSave: Meniul nu are componente. Returnează false.");
                return false;
            }


            if (IsEditMode)
            {
                if (SelectedMeniu == null || _originalMeniu == null) return false;
                bool formChanged = FormDenumire != _originalMeniu.Denumire ||
                                   FormSelectedCategorie?.CategorieID != _originalMeniu.CategorieID ||
                                   FormDescriere != _originalMeniu.Descriere ||
                                   FormCaleImagine != _originalMeniu.CaleImagine ||
                                   FormEsteActiv != _originalMeniu.EsteActiv;

                var originalComponente = _originalMeniu.MeniuPreparate?.ToList() ?? new List<MeniuPreparat>();
                var originalComponenteDetails = originalComponente
                    .Select(mp => new { mp.PreparatID, mp.CantitateInMeniu })
                    .OrderBy(x => x.PreparatID).ThenBy(x => x.CantitateInMeniu)
                    .ToList();

                var currentComponenteDetails = FormComponenteMeniu
                    .Select(vm => new { vm.PreparatID, vm.CantitateInMeniu })
                    .OrderBy(x => x.PreparatID).ThenBy(x => x.CantitateInMeniu)
                    .ToList();

                bool componenteChanged = !originalComponenteDetails.SequenceEqual(currentComponenteDetails);

                return formChanged || componenteChanged;
            }
            // Mod Adăugare
            return !string.IsNullOrWhiteSpace(FormDenumire) && FormSelectedCategorie != null && FormSelectedCategorie.CategorieID != 0 && FormComponenteMeniu.Any();
        }

        private async Task ExecuteAddMeniuAsync()
        {
            if (!CanExecuteAddOrSave() || _meniuRepository == null || FormSelectedCategorie == null || FormSelectedCategorie.CategorieID == 0) return;
            if (await _meniuRepository.NameExistsAsync(FormDenumire))
            {
                MessageBox.Show($"Meniul '{FormDenumire}' există deja.", "Denumire Duplicat", MessageBoxButton.OK, MessageBoxImage.Error); return;
            }

            var newMeniu = new Meniu
            {
                Denumire = FormDenumire,
                CategorieID = FormSelectedCategorie.CategorieID,
                Descriere = FormDescriere,
                CaleImagine = FormCaleImagine,
                EsteActiv = FormEsteActiv
            };
            var componenteDeSalvat = FormComponenteMeniu.Select(vm => new MeniuPreparat
            {
                PreparatID = vm.PreparatID,
                CantitateInMeniu = vm.CantitateInMeniu,
                UnitateMasuraCantitateInMeniu = vm.UnitateMasura
            }).ToList();

            await _meniuRepository.AddAsync(newMeniu, componenteDeSalvat);
            await LoadInitialDataAsync();
        }

        private async Task ExecuteUpdateMeniuAsync()
        {
            if (!CanExecuteAddOrSave() || SelectedMeniu == null || _originalMeniu == null || _meniuRepository == null || FormSelectedCategorie == null || FormSelectedCategorie.CategorieID == 0) return;
            if (FormDenumire != _originalMeniu.Denumire && await _meniuRepository.NameExistsAsync(FormDenumire, SelectedMeniu.MeniuID))
            {
                MessageBox.Show($"Meniul '{FormDenumire}' există deja pentru un alt ID.", "Denumire Duplicat", MessageBoxButton.OK, MessageBoxImage.Error); return;
            }

            var updatedMeniu = new Meniu
            {
                MeniuID = SelectedMeniu.MeniuID,
                Denumire = FormDenumire,
                CategorieID = FormSelectedCategorie.CategorieID,
                Descriere = FormDescriere,
                CaleImagine = FormCaleImagine,
                EsteActiv = FormEsteActiv
            };
            var componenteDeSalvat = FormComponenteMeniu.Select(vm => new MeniuPreparat
            {
                // MeniuID va fi setat în repository sau este deja pe SelectedMeniu.MeniuID
                PreparatID = vm.PreparatID,
                CantitateInMeniu = vm.CantitateInMeniu,
                UnitateMasuraCantitateInMeniu = vm.UnitateMasura
            }).ToList();

            await _meniuRepository.UpdateAsync(updatedMeniu, componenteDeSalvat);
            await LoadInitialDataAsync();
        }

        private bool CanExecuteDelete() => SelectedMeniu != null && IsEditMode;

        private async Task ExecuteDeleteMeniuAsync()
        {
            if (!CanExecuteDelete() || SelectedMeniu == null || _meniuRepository == null) return;
            var result = MessageBox.Show($"Sigur doriți să ștergeți (marcați ca inactiv) meniul '{SelectedMeniu.Denumire}'?", "Confirmare Ștergere", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await _meniuRepository.DeleteAsync(SelectedMeniu.MeniuID);
                await LoadInitialDataAsync();
            }
        }

        private void ExecuteSelectMeniuImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Selectează o Imagine pentru Meniu",
                Filter = "Fișiere Imagine (*.jpg; *.jpeg; *.png; *.gif; *.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Toate Fișierele (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };

            // Directorul țintă pentru imaginile meniurilor
            string targetImageFolderName = "MenuImages"; 

            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                // Încearcă să găsească folderul la rădăcina proiectului (ajustând calea din bin/Debug)
                string projectImageFolderPath = Path.Combine(baseDirectory, "..", "..", "..", targetImageFolderName);
                projectImageFolderPath = Path.GetFullPath(projectImageFolderPath);

                if (Directory.Exists(projectImageFolderPath))
                {
                    openFileDialog.InitialDirectory = projectImageFolderPath;
                }
                else
                {
                    // Dacă nu găsește folderul specific proiectului, folosește "My Pictures"
                    string defaultUserPictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    if (Directory.Exists(defaultUserPictures))
                    {
                        openFileDialog.InitialDirectory = defaultUserPictures;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Eroare la setarea InitialDirectory pentru imagini meniu: {ex.Message}");
            }

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                try
                {
                    // Directorul unde vor fi copiate imaginile în folderul de output al aplicației
                    string appOutputImagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, targetImageFolderName);
                    if (!Directory.Exists(appOutputImagesDir))
                    {
                        Directory.CreateDirectory(appOutputImagesDir);
                    }

                    string fileName = Path.GetFileName(selectedFilePath);
                    string destinationPath = Path.Combine(appOutputImagesDir, fileName);

                    // Suprascrie dacă există (logica simplificată)
                    File.Copy(selectedFilePath, destinationPath, true); // true = overwrite

                    // Stochează calea relativă la folderul de imagini din output-ul aplicației
                    FormCaleImagine = Path.Combine(targetImageFolderName, fileName); // Ex: "MenuImages/imagine_meniu.jpg"
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Eroare la copierea imaginii pentru meniu: {ex.Message}", "Eroare Imagine", MessageBoxButton.OK, MessageBoxImage.Error);
                    FormCaleImagine = selectedFilePath; // Revine la calea completă dacă copierea eșuează
                }
            }
        }

        private void ExecuteCancelEdit() => ExecutePrepareNewMeniu();
        public async Task InitializeAsync() => await LoadInitialDataAsync();
    }
}