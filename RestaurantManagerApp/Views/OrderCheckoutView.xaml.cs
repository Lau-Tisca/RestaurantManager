using RestaurantManagerApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantManagerApp.Views
{
    public partial class OrderCheckoutView : UserControl
    {
        public OrderCheckoutView() // Fără parametri pentru a putea fi folosit în DataTemplate
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrderCheckoutViewModel vm)
            {
                // Verifică dacă ViewModel-ul implementează IAsyncInitializableVM
                if (vm is IAsyncInitializableVM initializableVm)
                {
                    await initializableVm.InitializeAsync();
                }
            }
        }
    }
}