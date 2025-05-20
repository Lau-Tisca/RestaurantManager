using RestaurantManagerApp.Models;
using System.Threading.Tasks;

namespace RestaurantManagerApp.DataAccess
{
    public interface IUtilizatorRepository
    {
        Task<Utilizator?> GetByEmailAsync(string email);
        Task AddUserAsync(Utilizator utilizator);
        Task<bool> EmailExistsAsync(string email);
        // Alte metode pot fi adăugate ulterior (ex: GetById, UpdateUser etc.)
    }
}