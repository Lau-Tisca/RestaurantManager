using Microsoft.EntityFrameworkCore;
using RestaurantManagerApp.Data;
using RestaurantManagerApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantManagerApp.DataAccess
{
    public class AlergenRepository : IAlergenRepository
    {
        private readonly RestaurantContext _context;

        public AlergenRepository(RestaurantContext context)
        {
            _context = context;
        }

        public async Task<List<Alergen>> GetAllActiveAsync()
        {
            return await _context.Alergeni.Where(a => a.EsteActiv).OrderBy(a => a.Nume).ToListAsync();
        }

        public async Task<Alergen?> GetByIdAsync(int id)
        {
            return await _context.Alergeni.FirstOrDefaultAsync(a => a.AlergenID == id && a.EsteActiv);
        }

        public async Task AddAsync(Alergen alergen)
        {
            if (alergen == null)
            {
                throw new ArgumentNullException(nameof(alergen));
            }
            alergen.EsteActiv = true; // Asigurăm că e activ la adăugare
            _context.Alergeni.Add(alergen);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Alergen alergen)
        {
            if (alergen == null)
            {
                throw new ArgumentNullException(nameof(alergen));
            }
            // EF Core urmărește modificările dacă entitatea a fost încărcată anterior prin același context.
            // Dacă nu, trebuie să o atașezi și să-i setezi starea.
            // O abordare sigură este să încarci entitatea existentă și să actualizezi valorile.
            var existingAlergen = await _context.Alergeni.FindAsync(alergen.AlergenID);
            if (existingAlergen != null)
            {
                // Copiază valorile din 'alergen' (primit ca parametru) în 'existingAlergen' (cel urmărit de context)
                // Ai grijă să nu suprascrii proprietățile de navigare dacă nu intenționezi asta.
                _context.Entry(existingAlergen).CurrentValues.SetValues(alergen);
                // Sau setează manual proprietățile:
                // existingAlergen.Nume = alergen.Nume;
                // existingAlergen.EsteActiv = alergen.EsteActiv;
            }
            else
            {
                // Aruncă o excepție sau gestionează cazul în care alergenul nu există pentru update
                // Pentru simplitate, vom presupune că va exista sau vom lăsa SaveChanges să eșueze dacă ID-ul e invalid.
                // Alternativ, dacă primești un obiect ne-urmărit și vrei să-l actualizezi direct:
                _context.Entry(alergen).State = EntityState.Modified;
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var alergen = await _context.Alergeni.FindAsync(id);
            if (alergen != null)
            {
                alergen.EsteActiv = false; // Ștergere logică
                _context.Entry(alergen).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Alergeni.AnyAsync(a => a.AlergenID == id && a.EsteActiv);
        }

        public async Task<bool> NameExistsAsync(string name, int? currentId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (currentId.HasValue)
            {
                return await _context.Alergeni.AnyAsync(a => a.Nume.ToLower() == name.ToLower() && a.AlergenID != currentId.Value && a.EsteActiv);
            }
            else
            {
                return await _context.Alergeni.AnyAsync(a => a.Nume.ToLower() == name.ToLower() && a.EsteActiv);
            }
        }
    }
}