using RestaurantManagerApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantManagerApp.Views
{
    public partial class MeniuManagementView : UserControl
    {
        public MeniuManagementView(MeniuManagementViewModel viewModel) // Tipul corect de ViewModel
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MeniuManagementViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}