using Microsoft.EntityFrameworkCore;
using RestaurantManagerApp.Data;
using RestaurantManagerApp.Models;
using System.Threading.Tasks;

namespace RestaurantManagerApp.DataAccess
{
    public class UtilizatorRepository : IUtilizatorRepository
    {
        private readonly RestaurantContext _context;

        public UtilizatorRepository(RestaurantContext context)
        {
            _context = context;
        }

        public async Task<Utilizator?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _context.Utilizatori
                                 .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.EsteActiv);
        }

        public async Task AddUserAsync(Utilizator utilizator)
        {
            if (utilizator == null) throw new ArgumentNullException(nameof(utilizator));

            // Parola ar trebui să fie deja hash-uită înainte de a ajunge aici (în serviciul de autentificare)
            utilizator.EsteActiv = true; // Utilizatorii noi sunt activi by default
            utilizator.DataInregistrare = DateTime.Now;

            _context.Utilizatori.Add(utilizator);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return await _context.Utilizatori.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }
    }
}