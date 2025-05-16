using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagerApp.DataAccess;
using RestaurantManagerApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel; // Pentru DesignerProperties
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RestaurantManagerApp.ViewModels
{
    public partial class AlergenManagementViewModel : ObservableValidator
    {
        private readonly IAlergenRepository _alergenRepository;
        private Alergen? _originalAlergen;

        [ObservableProperty]
        private ObservableCollection<Alergen> _alergeni;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteAlergenCommand))]
        private Alergen? _selectedAlergen;

        [ObservableProperty]
        [Required(ErrorMessage = "Numele alergenului este obligatoriu.")]
        [MaxLength(100, ErrorMessage = "Numele nu poate depăși 100 de caractere.")]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(AddNewAlergenCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private string _formNume = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
        private bool _formEsteActiv = true;

        [ObservableProperty]
        private bool _isEditMode = false;

        public bool IsAddMode => !IsEditMode;

        public IAsyncRelayCommand LoadAlergeniCommand { get; }
        public IRelayCommand PrepareNewAlergenCommand { get; }
        public IAsyncRelayCommand AddNewAlergenCommand { get; }
        public IAsyncRelayCommand SaveChangesCommand { get; }
        public IAsyncRelayCommand DeleteAlergenCommand { get; }
        public IRelayCommand CancelEditCommand { get; }

        // Constructor pentru Design Time
        public AlergenManagementViewModel()
        {
            System.Diagnostics.Debug.WriteLine("AlergenManagementViewModel DesignTime Constructor Called");
            _alergeni = new ObservableCollection<Alergen>();
            _formNume = "Design Nume Alergen";
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                _alergeni.Add(new Alergen { Nume = "Gluten (Design)" });
                _alergeni.Add(new Alergen { Nume = "Lactoză (Design)" });
            }
            LoadAlergeniCommand = new AsyncRelayCommand(async () => await Task.CompletedTask);
            PrepareNewAlergenCommand = new RelayCommand(() => { IsEditMode = false; FormNume = "Nou Alergen Design"; });
            AddNewAlergenCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => true);
            SaveChangesCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => true);
            DeleteAlergenCommand = new AsyncRelayCommand(async () => await Task.CompletedTask, () => true);
            CancelEditCommand = new RelayCommand(() => { IsEditMode = false; FormNume = "Anulat Alergen Design"; });
        }

        // Constructor pentru Runtime
        public AlergenManagementViewModel(IAlergenRepository alergenRepository)
        {
            System.Diagnostics.Debug.WriteLine("AlergenManagementViewModel Runtime Constructor Called");
            _alergenRepository = alergenRepository;
            _alergeni = new ObservableCollection<Alergen>();

            LoadAlergeniCommand = new AsyncRelayCommand(LoadAlergeniAsync);
            PrepareNewAlergenCommand = new RelayCommand(ExecutePrepareNewAlergen);
            AddNewAlergenCommand = new AsyncRelayCommand(ExecuteAddAlergenAsync, CanExecuteAddOrSave);
            SaveChangesCommand = new AsyncRelayCommand(ExecuteUpdateAlergenAsync, CanExecuteAddOrSave);
            DeleteAlergenCommand = new AsyncRelayCommand(ExecuteDeleteAlergenAsync, CanExecuteDelete);
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit);

            ExecutePrepareNewAlergen();
            ValidateAllProperties();
        }

        partial void OnSelectedAlergenChanged(Alergen? value)
        {
            if (value != null)
            {
                IsEditMode = true;
                _originalAlergen = new Alergen { AlergenID = value.AlergenID, Nume = value.Nume, EsteActiv = value.EsteActiv };
                FormNume = value.Nume;
                FormEsteActiv = value.EsteActiv;
                ClearErrors();
            }
            else
            {
                if (IsEditMode) ExecutePrepareNewAlergen();
            }
        }

        partial void OnIsEditModeChanged(bool value)
        {
            OnPropertyChanged(nameof(IsAddMode));
            AddNewAlergenCommand.NotifyCanExecuteChanged();
            SaveChangesCommand.NotifyCanExecuteChanged();
        }

        private async Task LoadAlergeniAsync()
        {
            if (_alergenRepository == null) { /* Protecție design time */ return; }
            var alergenList = await _alergenRepository.GetAllActiveAsync();
            Alergeni.Clear();
            foreach (var al in alergenList.OrderBy(a => a.Nume))
            {
                Alergeni.Add(al);
            }
            ExecutePrepareNewAlergen();
        }

        private void ExecutePrepareNewAlergen()
        {
            IsEditMode = false;
            SelectedAlergen = null;
            _originalAlergen = null;
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
                return SelectedAlergen != null && _originalAlergen != null &&
                       (FormNume != _originalAlergen.Nume || FormEsteActiv != _originalAlergen.EsteActiv);
            }
            else
            {
                return !string.IsNullOrWhiteSpace(FormNume);
            }
        }

        private async Task ExecuteAddAlergenAsync()
        {
            if (!CanExecuteAddOrSave() || _alergenRepository == null) return;
            if (await _alergenRepository.NameExistsAsync(FormNume))
            {
                MessageBox.Show($"Alergenul '{FormNume}' există deja.", "Nume Duplicat", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var newAlergen = new Alergen { Nume = FormNume, EsteActiv = FormEsteActiv };
            await _alergenRepository.AddAsync(newAlergen);
            await LoadAlergeniAsync();
        }

        private async Task ExecuteUpdateAlergenAsync()
        {
            if (!CanExecuteAddOrSave() || SelectedAlergen == null || _originalAlergen == null || _alergenRepository == null) return;
            if (FormNume != _originalAlergen.Nume && await _alergenRepository.NameExistsAsync(FormNume, SelectedAlergen.AlergenID))
            {
                MessageBox.Show($"Alergenul '{FormNume}' există deja pentru un alt ID.", "Nume Duplicat", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var updatedAlergen = new Alergen { AlergenID = SelectedAlergen.AlergenID, Nume = FormNume, EsteActiv = FormEsteActiv };
            await _alergenRepository.UpdateAsync(updatedAlergen);
            await LoadAlergeniAsync();
        }

        private bool CanExecuteDelete()
        {
            return SelectedAlergen != null && IsEditMode;
        }

        private async Task ExecuteDeleteAlergenAsync()
        {
            if (!CanExecuteDelete() || SelectedAlergen == null || _alergenRepository == null) return;
            var result = MessageBox.Show($"Sigur doriți să ștergeți (marcați ca inactiv) alergenul '{SelectedAlergen.Nume}'?",
                                         "Confirmare Ștergere", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await _alergenRepository.DeleteAsync(SelectedAlergen.AlergenID);
                await LoadAlergeniAsync();
            }
        }

        private void ExecuteCancelEdit()
        {
            ExecutePrepareNewAlergen();
        }

        public async Task InitializeAsync()
        {
            await LoadAlergeniAsync();
        }
    }
}