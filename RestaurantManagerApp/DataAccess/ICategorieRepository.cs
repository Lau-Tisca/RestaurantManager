using RestaurantManagerApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantManagerApp.DataAccess
{
    public interface ICategorieRepository
    {
        Task<List<Categorie>> GetAllActiveAsync();
        Task<Categorie?> GetByIdAsync(int id); // Poate returna null dacă nu găsește categoria
        Task AddAsync(Categorie categorie);
        Task UpdateAsync(Categorie categorie);
        Task DeleteAsync(int id); // Sau marcare ca inactiv
        Task<bool> ExistsAsync(int id);
        Task<bool> NameExistsAsync(string name, int? currentId = null); // Pentru validare unicitate nume
    }
}