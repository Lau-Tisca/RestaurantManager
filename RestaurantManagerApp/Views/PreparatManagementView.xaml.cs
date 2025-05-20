using RestaurantManagerApp.ViewModels; // Asigură-te că acest using este prezent
using System.Windows;
using System.Windows.Controls;

namespace RestaurantManagerApp.Views
{
    public partial class PreparatManagementView : UserControl
    {
        public PreparatManagementView(PreparatManagementViewModel viewModel) // Tipul corect de ViewModel
        {
            InitializeComponent();
        }

        public PreparatManagementView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PreparatManagementViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}