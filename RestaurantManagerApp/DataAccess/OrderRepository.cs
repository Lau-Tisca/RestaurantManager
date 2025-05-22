using Microsoft.EntityFrameworkCore;
using RestaurantManagerApp.Data;
using RestaurantManagerApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantManagerApp.DataAccess
{
    public class OrderRepository : IOrderRepository
    {
        private readonly RestaurantContext _context;

        public OrderRepository(RestaurantContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<int> AddOrderAsync(Comanda comanda, List<ElementComanda> elementeComanda)
        {
            if (comanda == null) throw new ArgumentNullException(nameof(comanda));
            if (elementeComanda == null || !elementeComanda.Any())
                throw new ArgumentException("O comandă trebuie să conțină cel puțin un element.", nameof(elementeComanda));

            // Setăm data comenzii și un cod unic dacă nu sunt deja setate
            if (comanda.DataComanda == default) comanda.DataComanda = DateTime.Now;
            if (string.IsNullOrWhiteSpace(comanda.CodUnic)) comanda.CodUnic = GenerateUniqueOrderCode();
            if (string.IsNullOrWhiteSpace(comanda.StareComanda)) comanda.StareComanda = "Inregistrata"; // Stare inițială

            // Adaugă comanda principală
            _context.Comenzi.Add(comanda);
            // SaveChanges aici pentru a obține ComandaID generat de BD, necesar pentru ElementeComanda
            await _context.SaveChangesAsync();

            // Asociază fiecare ElementComanda cu ComandaID și adaugă-le
            foreach (var element in elementeComanda)
            {
                element.ComandaID = comanda.ComandaID; // Setează FK
                _context.ElementeComanda.Add(element);
            }
            // Salvează elementele comenzii
            await _context.SaveChangesAsync();

            return comanda.ComandaID; // Returnează ID-ul comenzii nou create
        }

        private string GenerateUniqueOrderCode()
        {
            // O metodă simplă de a genera un cod (poate fi îmbunătățită)
            // O combinație de timestamp și un număr aleatoriu
            return $"ORD-{DateTime.Now:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
        }

        // Implementări placeholder pentru celelalte metode din interfață (pentru viitor)
        // public async Task<Comanda?> GetOrderByIdAsync(int comandaId) { /* ... */ throw new NotImplementedException(); }
        // public async Task<List<Comanda>> GetOrdersByUserIdAsync(int userId) { /* ... */ throw new NotImplementedException(); }
        // public async Task<bool> UpdateOrderStatusAsync(int comandaId, string newStatus) { /* ... */ throw new NotImplementedException(); }
        // public async Task<int> GetRecentOrderCountForUserAsync(int userId, DateTime startDate) { /* ... */ throw new NotImplementedException(); }
    }
}