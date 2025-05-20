using RestaurantManagerApp.ViewModels;
using System.Windows;

namespace RestaurantManagerApp.Views
{
    public partial class MeniuManagementView : Window
    {
        public MeniuManagementView(MeniuManagementViewModel viewModel) // Tipul corect de ViewModel
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MeniuManagementViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}