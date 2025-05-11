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
using System.Windows.Shapes;
using RestaurantManagerApp.ViewModels;

namespace RestaurantManagerApp.Views
{
    /// <summary>
    /// Interaction logic for CategoriesTestView.xaml
    /// </summary>
    public partial class CategoriesTestView : Window
    {
        private readonly CategoriesTestViewModel _viewModel;

        public CategoriesTestView()
        {
            InitializeComponent();
        }

        // Constructor care acceptă ViewModel-ul (preferat pentru MVVM și DI)
        public CategoriesTestView(CategoriesTestViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        // Eveniment apelat când fereastra s-a încărcat
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                await _viewModel.InitializeAsync();
            }
        }
    }
}
