using RestaurantManagerApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantManagerApp.DataAccess
{
    public interface IMeniuRepository
    {
        Task<List<Meniu>> GetAllActiveWithDetailsAsync(); // Include Categorie și MeniuPreparate (cu Preparatele lor)
        Task<Meniu?> GetByIdWithDetailsAsync(int id);    // Include Categorie și MeniuPreparate (cu Preparatele lor)
        Task<int> AddAsync(Meniu meniu, List<MeniuPreparat> preparateComponente);
        Task UpdateAsync(Meniu meniu, List<MeniuPreparat> preparateComponente);
        Task DeleteAsync(int id); // Ștergere logică
        Task<bool> NameExistsAsync(string name, int? currentId = null);
    }
}