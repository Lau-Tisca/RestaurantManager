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
    /// Interaction logic for RestaurantMenuView.xaml
    /// </summary>
    public partial class RestaurantMenuView : UserControl
    {
        public RestaurantMenuView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is RestaurantMenuViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine($"RestaurantMenuView: UserControl_Loaded - ViewModel este OK. Timestamp: {DateTime.Now}");
                bool menuNeedsLoading = vm.DisplayedMenu == null || !vm.DisplayedMenu.Cast<object>().Any();
                System.Diagnostics.Debug.WriteLine($"  menuNeedsLoading = {menuNeedsLoading}");
                System.Diagnostics.Debug.WriteLine($"  vm.LoadMenuCommand.IsRunning = {vm.LoadMenuCommand.IsRunning}");

                if (!vm.LoadMenuCommand.IsRunning && menuNeedsLoading)
                {
                    System.Diagnostics.Debug.WriteLine("  UserControl_Loaded: Se apelează vm.InitializeAsync().");
                    await vm.InitializeAsync();
                    System.Diagnostics.Debug.WriteLine("  UserControl_Loaded: vm.InitializeAsync() a terminat.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("RestaurantMenuView: LoadMenuCommand este deja în execuție.");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("RestaurantMenuView: UserControl_Loaded - DataContext NU este RestaurantMenuViewModel sau este null.");
            }
        }
    }
}
