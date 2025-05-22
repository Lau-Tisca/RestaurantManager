using RestaurantManagerApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RestaurantManagerApp.Views
{
    /// <summary>
    /// Interaction logic for ShoppingCartView.xaml
    /// </summary>
    public partial class ShoppingCartView : UserControl
    {
        public ShoppingCartView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ShoppingCartViewModel vm)
            {
                // ShoppingCartViewModel se bazează pe starea ShoppingCartService,
                // care este un singleton. InitializeAsync poate forța o reîmprospătare a proprietăților.
                if (vm.InitializeAsync != null) // Verifică dacă metoda există
                {
                    await vm.InitializeAsync();
                }
            }
        }
    }
}
