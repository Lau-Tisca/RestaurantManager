using RestaurantManagerApp.ViewModels; // Asigură-te că acest using este prezent
using System.Windows;

namespace RestaurantManagerApp.Views
{
    public partial class PreparatManagementView : Window
    {
        public PreparatManagementView(PreparatManagementViewModel viewModel) // Tipul corect de ViewModel
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PreparatManagementViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}