using RestaurantManagerApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantManagerApp.DataAccess
{
    public interface IPreparatRepository
    {
        Task<List<Preparat>> GetAllAsync(); // Poate cu opțiune de a include Categoria și Alergenii
        Task<List<Preparat>> GetAllActiveWithDetailsAsync(); // Include Categorie și Alergeni
        Task<decimal?> GetAvailableStockByIdAsync(int preparatId);
        Task<Preparat?> GetByIdAsync(int id);
        Task<Preparat?> GetByIdWithDetailsAsync(int id); // Include Categorie și Alergeni
        Task<int> AddAsync(Preparat preparat, List<int> alergenIds); // Returnează ID-ul noului preparat
        Task UpdateAsync(Preparat preparat, List<int> alergenIds);
        Task DeleteAsync(int id); // Ștergere logică
        Task<bool> ExistsAsync(int id);
        Task<bool> NameExistsAsync(string name, int? currentId = null);
        Task UpdateStockAsync(int preparatId, decimal cantitateConsumata); // Specific pentru stoc
    }
}