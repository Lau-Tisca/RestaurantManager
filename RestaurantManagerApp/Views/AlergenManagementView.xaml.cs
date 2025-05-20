using RestaurantManagerApp.ViewModels; 
using System.Windows;
using System.Windows.Controls;

namespace RestaurantManagerApp.Views
{
    public partial class AlergenManagementView : UserControl
    {
        public AlergenManagementView(AlergenManagementViewModel viewModel)
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AlergenManagementViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}