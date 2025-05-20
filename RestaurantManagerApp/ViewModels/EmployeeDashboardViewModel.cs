using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System; // Pentru Action

namespace RestaurantManagerApp.ViewModels
{
    public partial class EmployeeDashboardViewModel : ObservableObject
    {
        // Acțiuni care vor fi setate de MainViewModel pentru a declanșa navigarea
        public Action? NavigateToCategories { get; set; }
        public Action? NavigateToAllergens { get; set; }
        public Action? NavigateToProducts { get; set; }
        public Action? NavigateToMenus { get; set; }
        // public Action? NavigateToOrders { get; set; } // Pentru viitor
        // public Action? NavigateToReports { get; set; } // Pentru viitor

        public IRelayCommand GoToCategoriesCommand { get; }
        public IRelayCommand GoToAllergensCommand { get; }
        public IRelayCommand GoToProductsCommand { get; }
        public IRelayCommand GoToMenusCommand { get; }

        public EmployeeDashboardViewModel()
        {
            System.Diagnostics.Debug.WriteLine("EmployeeDashboardViewModel created.");
            GoToCategoriesCommand = new RelayCommand(() => NavigateToCategories?.Invoke());
            GoToAllergensCommand = new RelayCommand(() => NavigateToAllergens?.Invoke());
            GoToProductsCommand = new RelayCommand(() => NavigateToProducts?.Invoke());
            GoToMenusCommand = new RelayCommand(() => NavigateToMenus?.Invoke());
        }
    }
}