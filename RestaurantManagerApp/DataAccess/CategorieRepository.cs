using Microsoft.EntityFrameworkCore; // Pentru ToListAsync, FirstOrDefaultAsync etc.
using RestaurantManagerApp.Data;     // Pentru RestaurantContext
using RestaurantManagerApp.Models;
using System.Collections.Generic;
using System.Linq; // Pentru Where, etc.
using System.Threading.Tasks;

namespace RestaurantManagerApp.DataAccess
{
    public class CategorieRepository : ICategorieRepository
    {
        private readonly RestaurantContext _context;

        // Constructorul primește DbContext-ul prin Dependency Injection
        public CategorieRepository(RestaurantContext context)
        {
            _context = context;
        }

        public async Task<List<Categorie>> GetAllActiveAsync()
        {
            // Returnează toate categoriile care sunt active
            return await _context.Categorii.Where(c => c.EsteActiv).ToListAsync();
        }

        public async Task<Categorie?> GetByIdAsync(int id)
        {
            // Găsește o categorie după ID, doar dacă este activă
            return await _context.Categorii.FirstOrDefaultAsync(c => c.CategorieID == id && c.EsteActiv);
        }

        public async Task AddAsync(Categorie categorie)
        {
            if (categorie == null)
            {
                throw new ArgumentNullException(nameof(categorie));
            }
            // Setează ca activă implicit la adăugare, dacă nu e deja setat
            categorie.EsteActiv = true;
            _context.Categorii.Add(categorie);
            await _context.SaveChangesAsync(); // Salvează modificările în baza de date
        }

        public async Task UpdateAsync(Categorie categorie)
        {
            if (categorie == null)
            {
                throw new ArgumentNullException(nameof(categorie));
            }

            // Verifică dacă entitatea este deja urmărită de context
            var existingCategorie = await _context.Categorii.FindAsync(categorie.CategorieID);
            if (existingCategorie != null)
            {
                // Actualizează proprietățile entității urmărite
                _context.Entry(existingCategorie).CurrentValues.SetValues(categorie);
            }
            else
            {
                // Dacă nu este urmărită, atașeaz-o și marcheaz-o ca modificată
                // Aceasta este mai puțin comună dacă încarci entitatea înainte de a o actualiza
                _context.Entry(categorie).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var categorie = await _context.Categorii.FindAsync(id);
            if (categorie != null)
            {
                // Implementăm ștergere logică (marcare ca inactiv)
                categorie.EsteActiv = false;
                _context.Entry(categorie).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            // Dacă am vrea ștergere fizică:
            // _context.Categorii.Remove(categorie);
            // await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Categorii.AnyAsync(c => c.CategorieID == id && c.EsteActiv);
        }

        public async Task<bool> NameExistsAsync(string name, int? currentId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (currentId.HasValue)
            {
                // La actualizare, verificăm dacă numele există pentru ALT ID
                return await _context.Categorii.AnyAsync(c => c.Nume.ToLower() == name.ToLower() && c.CategorieID != currentId.Value && c.EsteActiv);
            }
            else
            {
                // La adăugare, verificăm dacă numele există deja
                return await _context.Categorii.AnyAsync(c => c.Nume.ToLower() == name.ToLower() && c.EsteActiv);
            }
        }
    }
}