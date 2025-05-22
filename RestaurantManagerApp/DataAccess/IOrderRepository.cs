using RestaurantManagerApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantManagerApp.DataAccess
{
    public interface IOrderRepository
    {
        // Salvează o nouă comandă și elementele sale
        Task<int> AddOrderAsync(Comanda comanda, List<ElementComanda> elementeComanda);

        // Metode viitoare (momentan nu le implementăm complet, dar le putem schița):
        // Task<Comanda?> GetOrderByIdAsync(int comandaId);
        // Task<List<Comanda>> GetOrdersByUserIdAsync(int userId);
        // Task<bool> UpdateOrderStatusAsync(int comandaId, string newStatus);
        // Task<int> GetRecentOrderCountForUserAsync(int userId, DateTime startDate); // Pentru discount de loialitate
    }
}