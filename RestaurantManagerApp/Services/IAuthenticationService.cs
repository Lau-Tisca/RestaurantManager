using RestaurantManagerApp.Models;
using System.Threading.Tasks;

namespace RestaurantManagerApp.Services
{
    public interface IAuthenticationService
    {
        Task<Utilizator?> LoginAsync(string email, string parola);
        Task<bool> RegisterClientAsync(string nume, string prenume, string email, string parola, string? numarTelefon, string? adresaLivrare);
        // Vom adăuga o proprietate sau metodă pentru a obține utilizatorul curent autentificat
        Utilizator? CurrentUser { get; }
        void Logout();
    }
}