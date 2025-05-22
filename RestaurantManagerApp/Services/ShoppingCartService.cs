using CommunityToolkit.Mvvm.ComponentModel; // Pentru ObservableObject
using RestaurantManagerApp.ViewModels.Display; // Pentru DisplayMenuItemViewModel
using RestaurantManagerApp.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized; // Pentru NotifyCollectionChangedEventArgs
using System.ComponentModel; // Pentru INotifyPropertyChanged și PropertyChangedEventArgs
using System.Linq;
using System.Windows;

namespace RestaurantManagerApp.Services
{
    public class ShoppingCartService : ObservableObject, IShoppingCartService
    {
        private ObservableCollection<CartItemViewModel> _cartItems;
        public ObservableCollection<CartItemViewModel> CartItems => _cartItems; // Expunem direct pentru binding

        // Constructor
        public ShoppingCartService()
        {
            _cartItems = new ObservableCollection<CartItemViewModel>();
            // Abonează-te la schimbările colecției pentru a gestiona abonarea/dezabonarea la iteme
            _cartItems.CollectionChanged += CartItems_CollectionChanged;
            System.Diagnostics.Debug.WriteLine("ShoppingCartService instanțiat.");
        }

        // Proprietăți calculate
        public decimal Subtotal => _cartItems.Sum(item => item.TotalPrice);
        public int TotalItems => _cartItems.Sum(item => item.Quantity);

        // Metodă publică pentru adăugarea unui item în coș
        public void AddItemToCart(DisplayMenuItemViewModel newItemVm, int quantityToAdd = 1)
        {
            System.Diagnostics.Debug.WriteLine($"ShoppingCartService.AddItemToCart: Încercare adăugare '{newItemVm?.Denumire}' cu cantitatea {quantityToAdd}");

            if (newItemVm == null || quantityToAdd <= 0 || !newItemVm.EsteDisponibil)
            {
                System.Diagnostics.Debug.WriteLine("  AddItemToCart: Condiții pre-adăugare neîndeplinite. Return.");
                return;
            }

            var existingCartItem = _cartItems.FirstOrDefault(ci =>
                ci.MenuItem.OriginalId == newItemVm.OriginalId &&
                ci.MenuItem.EsteMeniuCompus == newItemVm.EsteMeniuCompus);

            if (existingCartItem != null)
            {
                MessageBox.Show($"Produsul '{existingCartItem.MenuItem.Denumire}' există deja în coș. Puteți modifica cantitatea din coș.", "Produs Existent", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"  AddItemToCart: Produs nou. Se adaugă '{newItemVm.Denumire}' cu cantitatea {quantityToAdd}");
                // Adăugarea în _cartItems va declanșa CartItems_CollectionChanged,
                // care se va ocupa de abonarea la PropertyChanged al noului item.
                _cartItems.Add(new CartItemViewModel(newItemVm, quantityToAdd));
            }
        }

        // Metodă publică pentru ștergerea unui item din coș
        public void RemoveItemFromCart(CartItemViewModel cartItemToRemove)
        {
            if (cartItemToRemove != null)
            {
                System.Diagnostics.Debug.WriteLine($"ShoppingCartService.RemoveItemFromCart: Încercare ștergere '{cartItemToRemove.MenuItem.Denumire}'.");
                // Ștergerea din _cartItems va declanșa CartItems_CollectionChanged,
                // care se va ocupa de dezabonarea de la PropertyChanged al item-ului șters.
                _cartItems.Remove(cartItemToRemove);
            }
            // NotifyCartChanged(); // Nu mai este necesar direct aici, va fi apelat de CollectionChanged
        }

        // Metodă publică pentru actualizarea cantității unui item (dacă nu se face direct binding TwoWay pe Quantity)
        // Dacă se face TwoWay binding pe Quantity în CartItemViewModel, și CartItemViewModel notifică
        // PropertyChanged, atunci CartItem_PropertyChanged din acest serviciu va fi apelat.
        public void UpdateItemQuantity(CartItemViewModel cartItemToUpdate, int newQuantity)
        {
            if (cartItemToUpdate != null && _cartItems.Contains(cartItemToUpdate))
            {
                System.Diagnostics.Debug.WriteLine($"ShoppingCartService.UpdateItemQuantity: Actualizare cantitate pentru '{cartItemToUpdate.MenuItem.Denumire}' la {newQuantity}.");
                if (newQuantity > 0)
                {
                    cartItemToUpdate.Quantity = newQuantity; // Va declanșa PropertyChanged pe item
                }
                else
                {
                    _cartItems.Remove(cartItemToUpdate); // Va declanșa CollectionChanged
                }
            }
            // NotifyCartChanged(); // Nu mai este necesar direct aici
        }

        // Metodă publică pentru golirea coșului
        public void ClearCart()
        {
            System.Diagnostics.Debug.WriteLine("ShoppingCartService.ClearCart: Se golește coșul.");
            // Când _cartItems.Clear() este apelat, CartItems_CollectionChanged
            // va fi declanșat pentru fiecare item șters (sau cu Action=Reset),
            // și se va face dezabonarea.
            if (_cartItems.Any()) // Doar dacă sunt iteme de șters
            {
                _cartItems.Clear();
            }
            // NotifyCartChanged(); // Nu mai este necesar direct aici, va fi apelat de CollectionChanged
        }

        // Handler pentru când colecția de iteme din coș se modifică (adaugă/șterge)
        private void CartItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"ShoppingCartService.CartItems_CollectionChanged: Acțiune = {e.Action}");
            if (e.NewItems != null)
            {
                foreach (object? item in e.NewItems)
                {
                    if (item is CartItemViewModel newItem)
                    {
                        newItem.PropertyChanged += CartItem_PropertyChanged;
                        System.Diagnostics.Debug.WriteLine($"  Abonat la PropertyChanged pentru item nou: {newItem.MenuItem.Denumire}");
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (object? item in e.OldItems)
                {
                    if (item is CartItemViewModel oldItem)
                    {
                        oldItem.PropertyChanged -= CartItem_PropertyChanged;
                        System.Diagnostics.Debug.WriteLine($"  Dezabonat de la PropertyChanged pentru item șters: {oldItem.MenuItem.Denumire}");
                    }
                }
            }
            NotifyCartChanged(); // O schimbare în structura colecției afectează totalurile
        }

        // Handler pentru când o proprietate a unui item individual din coș se modifică
        private void CartItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is CartItemViewModel changedItem)
            {
                System.Diagnostics.Debug.WriteLine($"ShoppingCartService.CartItem_PropertyChanged: Item '{changedItem.MenuItem.Denumire}', Proprietate '{e.PropertyName}' s-a schimbat.");
                // Dacă se schimbă cantitatea (și implicit TotalPrice al itemului), trebuie să notificăm totalurile coșului
                if (e.PropertyName == nameof(CartItemViewModel.Quantity) || e.PropertyName == nameof(CartItemViewModel.TotalPrice))
                {
                    NotifyCartChanged();
                }
            }
        }

        // Metodă centralizată pentru a notifica schimbările la proprietățile calculate ale coșului
        private void NotifyCartChanged()
        {
            System.Diagnostics.Debug.WriteLine("ShoppingCartService: NotifyCartChanged -> Notificare Subtotal și TotalItems.");
            OnPropertyChanged(nameof(Subtotal)); // Notifică UI-ul că Subtotal s-a schimbat
            OnPropertyChanged(nameof(TotalItems)); // Notifică UI-ul că TotalItems s-a schimbat
        }
    }
}