using CommunityToolkit.Mvvm.ComponentModel;
using RestaurantManagerApp.Models;
using RestaurantManagerApp.ViewModels.Display;
using RestaurantManagerApp.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagerApp.Services 
{
    public interface IShoppingCartService : INotifyPropertyChanged
    {
        ObservableCollection<CartItemViewModel> CartItems { get; }
        decimal Subtotal { get; }
        int TotalItems { get; }
        void AddItemToCart(DisplayMenuItemViewModel item, int quantity = 1);
        void RemoveItemFromCart(CartItemViewModel cartItem);
        void UpdateItemQuantity(CartItemViewModel cartItem, int newQuantity);
        void ClearCart();
    }
}