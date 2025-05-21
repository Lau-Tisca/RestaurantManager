using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace RestaurantManagerApp.ViewModels.Display
{
    public partial class DisplayCategoryViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _nume = string.Empty;
        [ObservableProperty]
        private ObservableCollection<DisplayMenuItemViewModel> _elementeMeniu = new(); // Inițializează direct

        public DisplayCategoryViewModel(string nume)
        {
            _nume = nume;
        }
    }
}