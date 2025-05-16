using RestaurantManagerApp.ViewModels; 
using System.Windows;

namespace RestaurantManagerApp.Views
{
    public partial class AlergenManagementView : Window
    {
        public AlergenManagementView(AlergenManagementViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AlergenManagementViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}