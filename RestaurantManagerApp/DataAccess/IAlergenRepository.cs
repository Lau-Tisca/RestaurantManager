using RestaurantManagerApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantManagerApp.DataAccess
{
    public interface IAlergenRepository
    {
        Task<List<Alergen>> GetAllActiveAsync();
        Task<Alergen?> GetByIdAsync(int id);
        Task AddAsync(Alergen alergen);
        Task UpdateAsync(Alergen alergen);
        Task DeleteAsync(int id); // Ștergere logică
        Task<bool> ExistsAsync(int id);
        Task<bool> NameExistsAsync(string name, int? currentId = null);
    }
}