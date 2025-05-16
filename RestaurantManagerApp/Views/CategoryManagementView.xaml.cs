using RestaurantManagerApp.ViewModels;
using System.Windows;

namespace RestaurantManagerApp.Views
{
    public partial class CategoryManagementView : Window
    {
        private readonly CategoryManagementViewModel _viewModel;

        // Constructor care acceptă ViewModel-ul (preferat pentru MVVM și DI)
        public CategoryManagementView(CategoryManagementViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel; // Setează DataContext-ul vederii la ViewModel
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Verificăm dacă DataContext-ul (ViewModel-ul) este setat și are metoda InitializeAsync
            if (DataContext is CategoryManagementViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}