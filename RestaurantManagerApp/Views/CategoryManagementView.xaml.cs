using RestaurantManagerApp.ViewModels;
using System.Windows;
using System.Windows.Controls; // Necesar pentru UserControl

namespace RestaurantManagerApp.Views
{
    public partial class CategoryManagementView : UserControl // Schimbă aici
    {
        // Constructorul nu mai primește ViewModel-ul prin DI direct.
        // DataContext-ul va fi setat de ContentControl.
        public CategoryManagementView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e) // Sau Window_Loaded, dacă nu redenumești
        {
            // ViewModel-ul ar trebui să fie deja setat ca DataContext de către MainViewModel/ContentControl
            if (DataContext is CategoryManagementViewModel vm && vm.LoadCategoriesCommand.CanExecute(null)) // Verifică dacă se poate executa
            {
                // Dacă vm.InitializeAsync() încarcă datele, e OK.
                // Sau dacă vrei să încarci doar când se afișează explicit:
                // await vm.LoadCategoriesCommand.ExecuteAsync(null); 
                // Cel mai bine e ca MainViewModel să apeleze InitializeAsync pe CurrentViewModel când îl setează,
                // sau ViewModel-ul să aibă o logica de încărcare la prima activare.
                // Pentru moment, apelarea InitializeAsync aici la Loaded e o soluție simplă.
                await vm.InitializeAsync();
            }
        }
    }
}