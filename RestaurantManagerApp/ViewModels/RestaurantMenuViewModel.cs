using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagerApp.DataAccess;
using RestaurantManagerApp.Models;
using RestaurantManagerApp.Utils; // Pentru ApplicationSettings
using RestaurantManagerApp.ViewModels.Display; // Pentru Display ViewModels
using RestaurantManagerApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel; // Pentru DesignerProperties și ICollectionView
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data; // Pentru CollectionViewSource
using System.Windows;

namespace RestaurantManagerApp.ViewModels
{
    public partial class RestaurantMenuViewModel : ObservableObject
    {
        private readonly ICategorieRepository? _categorieRepository; // Nullable pentru design time
        private readonly IPreparatRepository? _preparatRepository;   // Nullable pentru design time
        private readonly IMeniuRepository? _meniuRepository;         // Nullable pentru design time
        private readonly IAlergenRepository? _alergenRepository;     // Nullable pentru design time
        private readonly ApplicationSettings _appSettings;
        private readonly IAuthenticationService _authenticationService;
        private readonly IShoppingCartService _shoppingCartService;

        // Colecția sursă completă, nefiltrată (sau cât mai puțin filtrată inițial)
        private List<DisplayCategoryViewModel> _fullMenuList = new();

        // Colecția afișată în UI, rezultatul filtrării, acum ca ICollectionView
        [ObservableProperty]
        private ICollectionView? _displayedMenu; // Inițial null, va fi setat de ApplyFilters

        // --- Proprietăți pentru Filtrare/Căutare ---
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))] // SearchCommand poate depinde de existența textului
        private string? _searchText;

        [ObservableProperty]
        private ObservableCollection<Alergen> _availableAllergens = new();

        [ObservableProperty]
        private Alergen? _selectedAllergenFilter;

        public List<string> AllergenFilterTypes { get; } = new List<string> { "Toate", "Conține", "Nu Conține" };
        public bool IsClientLoggedIn => _authenticationService.CurrentUser?.TipUtilizator == "Client";

        [ObservableProperty]
        private string _selectedAllergenFilterType = "Toate";

        // --- Comenzi ---
        public IAsyncRelayCommand LoadMenuCommand { get; }
        public IRelayCommand SearchCommand { get; } // Redenumită din ApplyFiltersCommand pentru claritate
        public IRelayCommand ClearFiltersCommand { get; }
        public IRelayCommand<DisplayMenuItemViewModel> AddToCartCommand { get; }

        // --- Constructor pentru Design Time ---
        public RestaurantMenuViewModel()
        {
            System.Diagnostics.Debug.WriteLine("RestaurantMenuViewModel DesignTime Constructor Called");
            _authenticationService = new MockAuthenticationService();
            _shoppingCartService = new MockShoppingCartService();
            _appSettings = new ApplicationSettings { MenuDiscountPercentageX = 10 };
            _availableAllergens = new ObservableCollection<Alergen>
            {
                new Alergen { AlergenID = 0, Nume = "(Toți Alergenii)" }, // Opțiune placeholder
                new Alergen { AlergenID = 1, Nume = "Gluten Design" },
                new Alergen { AlergenID = 2, Nume = "Lactoză Design" }
            };
            _selectedAllergenFilter = _availableAllergens[0]; // Selectează placeholder-ul

            var designCategories = new ObservableCollection<DisplayCategoryViewModel>();
            var cat1 = new DisplayCategoryViewModel("Supe (Design)");
            var prep1 = new Preparat { PreparatID = 1, Denumire = "Supă de Pui Design", Pret = 15, CantitatePortie = "300ml", EsteActiv = true, CantitateTotalaStoc = 10, Alergeni = new List<Alergen>() };
            cat1.ElementeMeniu.Add(new DisplayPreparatViewModel(prep1));
            designCategories.Add(cat1);

            var cat2 = new DisplayCategoryViewModel("Fel Principal (Design)");
            var meniu1CompPrep = new Preparat { PreparatID = 2, Denumire = "Cartofi Design", Pret = 8, EsteActiv = true, CantitateTotalaStoc = 10, Alergeni = new List<Alergen>() };
            var meniu1 = new Meniu
            {
                MeniuID = 1,
                Denumire = "Meniu Pui Design",
                EsteActiv = true,
                MeniuPreparate = new List<MeniuPreparat> {
                                         new MeniuPreparat { Preparat = meniu1CompPrep, PreparatID = 2, CantitateInMeniu = "150g" }
                                     }
            };
            cat2.ElementeMeniu.Add(new DisplayMenuViewModel(meniu1, _appSettings));
            designCategories.Add(cat2);

            // Setează DisplayedMenu pentru design time
            DisplayedMenu = CollectionViewSource.GetDefaultView(designCategories);
            if (DisplayedMenu != null && DisplayedMenu.CanGroup)
            {
                DisplayedMenu.GroupDescriptions.Clear();
                DisplayedMenu.GroupDescriptions.Add(new PropertyGroupDescription("Nume"));
            }


            LoadMenuCommand = new AsyncRelayCommand(async () => await Task.CompletedTask);
            // CanExecute pentru SearchCommand ar putea verifica dacă există criterii de filtrare
            SearchCommand = new RelayCommand(ApplyFiltersInternal, CanApplyFiltersInternal);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFiltersInternal);
            AddToCartCommand = new RelayCommand<DisplayMenuItemViewModel>(
                item => { if (item != null) System.Diagnostics.Debug.WriteLine($"Design: Adaugă în coș {item.Denumire}"); },
                                                                    item => item != null && item.EsteDisponibil && IsClientLoggedIn);
        }

        // --- Constructor pentru Runtime ---
        public RestaurantMenuViewModel(
            ICategorieRepository categorieRepository,
            IPreparatRepository preparatRepository,
            IMeniuRepository meniuRepository,
            IAlergenRepository alergenRepository,
            ApplicationSettings appSettings,
            IAuthenticationService authenticationService,
            IShoppingCartService shoppingCartService)
        {
            System.Diagnostics.Debug.WriteLine("RestaurantMenuViewModel Runtime Constructor Called");
            _categorieRepository = categorieRepository ?? throw new ArgumentNullException(nameof(categorieRepository));
            _preparatRepository = preparatRepository ?? throw new ArgumentNullException(nameof(preparatRepository));
            _meniuRepository = meniuRepository ?? throw new ArgumentNullException(nameof(meniuRepository));
            _alergenRepository = alergenRepository ?? throw new ArgumentNullException(nameof(alergenRepository));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _shoppingCartService = shoppingCartService ?? throw new ArgumentNullException(nameof(shoppingCartService));

            LoadMenuCommand = new AsyncRelayCommand(LoadFullMenuAsync);
            SearchCommand = new RelayCommand(ApplyFiltersInternal, CanApplyFiltersInternal);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFiltersInternal);
            AddToCartCommand = new RelayCommand<DisplayMenuItemViewModel>(ExecuteAddToCart, CanExecuteAddToCart);

            OnPropertyChanged(nameof(IsClientLoggedIn));
        }

        public void UpdateLoginState()
        {
            OnPropertyChanged(nameof(IsClientLoggedIn));
            AddToCartCommand.NotifyCanExecuteChanged();
        }

        // --- Metode pentru încărcarea datelor ---
        private async Task LoadFullMenuAsync()
        {
            System.Diagnostics.Debug.WriteLine($"--- RestaurantMenuViewModel: LoadFullMenuAsync APELAT --- Timestamp: {DateTime.Now}");
            _fullMenuList.Clear(); // Asigură-te că o cureți la începutul încărcării complete
            AvailableAllergens.Clear(); // Și aceasta

            if (_categorieRepository == null || _preparatRepository == null || _meniuRepository == null || _alergenRepository == null)
            {
                System.Diagnostics.Debug.WriteLine("LoadFullMenuAsync: Unul sau mai multe repository-uri sunt null. Anulare.");
                return; // Protecție adițională, deși constructorul ar trebui să arunce excepție
            }

            // Încarcă alergenii disponibili pentru filtru
            var alergeniDb = await _alergenRepository.GetAllActiveAsync();
            AvailableAllergens.Clear();
            AvailableAllergens.Add(new Alergen { AlergenID = 0, Nume = "(Toți Alergenii)" }); // Placeholder
            foreach (var al in alergeniDb.OrderBy(a => a.Nume))
            {
                AvailableAllergens.Add(al);
            }
            SelectedAllergenFilter = AvailableAllergens.FirstOrDefault(); // Selectează placeholder-ul

            // Încarcă datele din repository-uri
            var categoriiDb = await _categorieRepository.GetAllActiveAsync();
            var preparateDb = await _preparatRepository.GetAllActiveWithDetailsAsync();
            var meniuriDb = await _meniuRepository.GetAllActiveWithDetailsAsync();

            _fullMenuList.Clear();

            foreach (var categorie in categoriiDb.OrderBy(c => c.Nume))
            {
                var displayCategory = new DisplayCategoryViewModel(categorie.Nume);

                try
                {
                    // Adaugă preparatele
                    var preparateInCategorie = preparateDb.Where(p => p.CategorieID == categorie.CategorieID);
                    foreach (var preparat in preparateInCategorie)
                    {
                        displayCategory.ElementeMeniu.Add(new DisplayPreparatViewModel(preparat));
                    }

                    // Adaugă meniurile
                    var meniuriInCategorie = meniuriDb.Where(m => m.CategorieID == categorie.CategorieID);
                    foreach (var meniu in meniuriInCategorie)
                    {
                        displayCategory.ElementeMeniu.Add(new DisplayMenuViewModel(meniu, _appSettings));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EROARE la crearea itemilor pentru categoria {categorie.Nume}: {ex.Message}");
                    // Poți decide să continui sau să te oprești
                }

                if (displayCategory.ElementeMeniu.Any())
                {
                    _fullMenuList.Add(displayCategory);
                }
            }

            System.Diagnostics.Debug.WriteLine($"--- RestaurantMenuViewModel: LoadFullMenuAsync - _fullMenuList.Count = {_fullMenuList.Count} ---");
            foreach (var cat in _fullMenuList)
            {
                System.Diagnostics.Debug.WriteLine($"  Categorie în _fullMenuList: {cat.Nume}, Elemente: {cat.ElementeMeniu.Count}");
            }
            Debug.WriteLine($"LoadFullMenuAsync: _fullMenuList.Count = {_fullMenuList.Count}");
            ApplyFiltersInternal(); // Aplică filtrele (inițial va afișa tot)
        }


        // --- Metode pentru Filtrare/Căutare ---
        // Metodele partial On...Changed sunt apelate automat de [ObservableProperty]
        // și vor declanșa ApplyFiltersInternal dacă e necesar sau vor notifica comanda SearchCommand
        partial void OnSearchTextChanged(string? value) => SearchCommand.NotifyCanExecuteChanged(); // Sau apelează direct ApplyFiltersInternal()
        partial void OnSelectedAllergenFilterChanged(Alergen? value) => SearchCommand.NotifyCanExecuteChanged();
        partial void OnSelectedAllergenFilterTypeChanged(string value) => SearchCommand.NotifyCanExecuteChanged();

        private bool CanApplyFiltersInternal()
        {
            // Comanda de căutare este activă dacă există text de căutare SAU un filtru de alergeni este activ (nu e "Toate" și un alergen e selectat)
            bool hasSearchText = !string.IsNullOrWhiteSpace(SearchText);
            bool hasAllergenFilter = SelectedAllergenFilter != null && SelectedAllergenFilter.AlergenID != 0 && SelectedAllergenFilterType != "Toate";
            return hasSearchText || hasAllergenFilter;
        }

        // Redenumită din ApplyFilters pentru a evita confuzia cu un posibil eveniment
        private void ApplyFiltersInternal()
        {
            System.Diagnostics.Debug.WriteLine($"--- RestaurantMenuViewModel: ApplyFiltersInternal APELAT --- Timestamp: {DateTime.Now}");
            System.Diagnostics.Debug.WriteLine($"ApplyFiltersInternal: _fullMenuList.Count = {_fullMenuList.Count}");
            // ... restul logicii de filtrare ...
            System.Diagnostics.Debug.WriteLine($"ApplyFiltersInternal: Text='{SearchText}', Alergen='{SelectedAllergenFilter?.Nume}', TipFiltru='{SelectedAllergenFilterType}'");

            IEnumerable<DisplayCategoryViewModel> categoriesToDisplaySource = _fullMenuList;

            // Aplică filtrul de text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string lowerSearchText = SearchText.ToLowerInvariant();
                var categoriesWithFilteredItemsByText = new List<DisplayCategoryViewModel>();
                foreach (var category in categoriesToDisplaySource)
                {
                    var matchingItems = category.ElementeMeniu
                        .Where(item => item.Denumire.ToLowerInvariant().Contains(lowerSearchText) ||
                                       (item.Descriere?.ToLowerInvariant().Contains(lowerSearchText) ?? false))
                        .ToList();

                    if (matchingItems.Any())
                    {
                        var newFilteredCategory = new DisplayCategoryViewModel(category.Nume);
                        foreach (var item in matchingItems) newFilteredCategory.ElementeMeniu.Add(item);
                        categoriesWithFilteredItemsByText.Add(newFilteredCategory);
                    }
                }
                categoriesToDisplaySource = categoriesWithFilteredItemsByText;
            }

            // Aplică filtrul de alergeni
            if (SelectedAllergenFilter != null && SelectedAllergenFilter.AlergenID != 0 && SelectedAllergenFilterType != "Toate")
            {
                var categoriesWithFilteredItemsByAllergen = new List<DisplayCategoryViewModel>();
                foreach (var category in categoriesToDisplaySource)
                {
                    var matchingItems = new List<DisplayMenuItemViewModel>();
                    foreach (var item in category.ElementeMeniu)
                    {
                        // Verificăm dacă AlergeniAfisati conține numele alergenului selectat.
                        // Este important ca SelectedAllergenFilter.Nume să nu fie null sau gol.
                        bool containsAllergen = item.AlergeniAfisati.ToLowerInvariant().Contains(SelectedAllergenFilter.Nume.ToLowerInvariant());

                        if (SelectedAllergenFilterType == "Conține" && containsAllergen)
                        {
                            matchingItems.Add(item);
                        }
                        else if (SelectedAllergenFilterType == "Nu Conține" && !containsAllergen)
                        {
                            matchingItems.Add(item);
                        }
                    }
                    if (matchingItems.Any())
                    {
                        var newFilteredCategory = new DisplayCategoryViewModel(category.Nume);
                        foreach (var item in matchingItems) newFilteredCategory.ElementeMeniu.Add(item);
                        categoriesWithFilteredItemsByAllergen.Add(newFilteredCategory);
                    }
                }
                categoriesToDisplaySource = categoriesWithFilteredItemsByAllergen;
            }

            // Creează și setează CollectionView-ul pentru afișare
            // Convertim IEnumerable în ObservableCollection pentru a putea fi folosit de CollectionViewSource în mod dinamic
            var finalFilteredObservableCollection = new ObservableCollection<DisplayCategoryViewModel>(categoriesToDisplaySource);
            DisplayedMenu = CollectionViewSource.GetDefaultView(finalFilteredObservableCollection);

            if (DisplayedMenu != null && DisplayedMenu.CanGroup)
            {
                DisplayedMenu.GroupDescriptions.Clear();
                DisplayedMenu.GroupDescriptions.Add(new PropertyGroupDescription("Nume")); // Grupează după proprietatea "Nume" a DisplayCategoryViewModel
            }
            // Forțează reîmprospătarea UI-ului pentru listă, dacă e necesar (deși schimbarea ICollectionView ar trebui să fie suficientă)
            // OnPropertyChanged(nameof(DisplayedMenu)); // Nu mai este necesar dacă DisplayedMenu este [ObservableProperty]
            System.Diagnostics.Debug.WriteLine($"DisplayedMenu count after filtering: {finalFilteredObservableCollection.Count}");
        }

        private void ExecuteClearFiltersInternal()
        {
            System.Diagnostics.Debug.WriteLine($"--- RestaurantMenuViewModel: ExecuteClearFiltersInternal APELAT --- Timestamp: {DateTime.Now}");
            SearchText = string.Empty; // Setează la string gol
            SelectedAllergenFilter = AvailableAllergens.FirstOrDefault(a => a.AlergenID == 0) ?? AvailableAllergens.FirstOrDefault(); // Resetează la "(Toți Alergenii)"
            SelectedAllergenFilterType = "Toate";
            // ApplyFiltersInternal(); // Va fi apelat de schimbările de proprietăți și notificarea comenzii SearchCommand
            //SearchCommand.NotifyCanExecuteChanged(); // Forțează reevaluarea CanExecute
            ApplyFiltersInternal(); // Aplică direct pentru a reafișa tot
        }

        private bool CanExecuteAddToCart(DisplayMenuItemViewModel? item)
        {
            return item != null && item.EsteDisponibil && IsClientLoggedIn;
        }

        private void ExecuteAddToCart(DisplayMenuItemViewModel? item)
        {
            System.Diagnostics.Debug.WriteLine("ExecuteAddToCart: APELAT");

            if (item == null)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteAddToCart: item este NULL! Comanda nu a primit parametrul corect.");
                MessageBox.Show("Eroare: Produsul nu a putut fi identificat pentru adăugare în coș.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _shoppingCartService.AddItemToCart(item, 1); // Adaugă 1 bucată implicit
                System.Diagnostics.Debug.WriteLine($"ExecuteAddToCart: '{item.Denumire}' a fost trimis către ShoppingCartService.");

                // Acum MessageBox-ul ar trebui să se afișeze
                //MessageBox.Show($"{item.Denumire} a fost adăugat în coș!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Debug.WriteLine($"ExecuteAddToCart: MessageBox afișat pentru '{item.Denumire}'.");
            }
            catch (Exception ex)
            {
                // Prinde orice excepție neașteptată din _shoppingCartService.AddItemToCart
                System.Diagnostics.Debug.WriteLine($"EROARE în ExecuteAddToCart la apelarea _shoppingCartService.AddItemToCart sau MessageBox: {ex.ToString()}");
                MessageBox.Show($"A apărut o eroare la adăugarea în coș: {ex.Message}", "Eroare Critică", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task InitializeAsync()
        {
            System.Diagnostics.Debug.WriteLine($"--- RestaurantMenuViewModel: InitializeAsync APELAT --- Timestamp: {DateTime.Now}");
            if (LoadMenuCommand.CanExecute(null))
            {
                System.Diagnostics.Debug.WriteLine("InitializeAsync: LoadMenuCommand.CanExecute este true. Se execută...");
                await LoadMenuCommand.ExecuteAsync(null);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("InitializeAsync: LoadMenuCommand.CanExecute este false! Nu se încarcă meniul.");
            }
            System.Diagnostics.Debug.WriteLine($"--- RestaurantMenuViewModel: InitializeAsync TERMINAT --- Timestamp: {DateTime.Now}");
        }

        private class MockAuthenticationService : IAuthenticationService { public Utilizator? CurrentUser => new Utilizator { TipUtilizator = "Client" }; /* implementează restul dacă e nevoie */ public Task<Utilizator?> LoginAsync(string e, string p) => Task.FromResult<Utilizator?>(null); public Task<bool> RegisterClientAsync(string n, string pn, string e, string p, string? nt, string? al) => Task.FromResult(false); public void Logout() { } }
        private class MockShoppingCartService : IShoppingCartService { public ObservableCollection<CartItemViewModel> CartItems => new(); public decimal Subtotal => 0; public int TotalItems => 0; public event PropertyChangedEventHandler? PropertyChanged; public void AddItemToCart(DisplayMenuItemViewModel i, int q = 1) { } public void ClearCart() { } public void RemoveItemFromCart(CartItemViewModel c) { } public void UpdateItemQuantity(CartItemViewModel c, int nq) { } }
    }
}
