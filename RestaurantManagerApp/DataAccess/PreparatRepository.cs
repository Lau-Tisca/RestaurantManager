using Microsoft.EntityFrameworkCore;
using RestaurantManagerApp.Data;
using RestaurantManagerApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantManagerApp.DataAccess
{
    public class PreparatRepository : IPreparatRepository
    {
        private readonly RestaurantContext _context;

        public PreparatRepository(RestaurantContext context)
        {
            _context = context;
        }

        public async Task<List<Preparat>> GetAllAsync()
        {
            return await _context.Preparate
                                 .OrderBy(p => p.Denumire)
                                 .ToListAsync();
        }

        public async Task<List<Preparat>> GetAllActiveWithDetailsAsync()
        {
            return await _context.Preparate
                                 .Where(p => p.EsteActiv)
                                 .Include(p => p.Categorie) // Eager loading pentru Categorie
                                 .Include(p => p.Alergeni)  // Eager loading pentru Alergeni
                                 .OrderBy(p => p.Denumire)
                                 .ToListAsync();
        }

        public async Task<Preparat?> GetByIdAsync(int id)
        {
            return await _context.Preparate.FindAsync(id);
        }

        public async Task<Preparat?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Preparate
                                 .Include(p => p.Categorie)
                                 .Include(p => p.Alergeni)
                                 .FirstOrDefaultAsync(p => p.PreparatID == id);
        }

        public async Task<int> AddAsync(Preparat preparat, List<int> alergenIds)
        {
            if (preparat == null) throw new ArgumentNullException(nameof(preparat));

            preparat.EsteActiv = true;
            // Asigură-te că proprietățile de navigare nu sunt setate dacă adaugi prin ID-uri
            preparat.Categorie = null;
            preparat.Alergeni = new List<Alergen>(); // Inițializează ca goală, o vom popula mai jos

            _context.Preparate.Add(preparat);
            await _context.SaveChangesAsync(); // Salvează preparatul pentru a obține PreparatID

            // Acum adaugă alergenii
            if (alergenIds != null && alergenIds.Any())
            {
                // Re-încarcă preparatul cu colecția Alergeni pentru a o putea modifica
                // Sau, dacă nu ai lazy loading, poți adăuga direct în tabela de joncțiune
                // Dar cel mai curat e să lucrăm cu colecția de navigare.
                var preparatDinDb = await _context.Preparate
                                                .Include(p => p.Alergeni)
                                                .FirstAsync(p => p.PreparatID == preparat.PreparatID);

                foreach (var alergenId in alergenIds)
                {
                    var alergenDeAdaugat = await _context.Alergeni.FindAsync(alergenId);
                    if (alergenDeAdaugat != null)
                    {
                        preparatDinDb.Alergeni.Add(alergenDeAdaugat);
                    }
                }
                await _context.SaveChangesAsync(); // Salvează legăturile cu alergenii
            }
            return preparat.PreparatID;
        }

        public async Task UpdateAsync(Preparat preparat, List<int> alergenIds)
        {
            if (preparat == null) throw new ArgumentNullException(nameof(preparat));

            var preparatDinDb = await _context.Preparate
                                            .Include(p => p.Alergeni) // Important să includem alergenii existenți
                                            .FirstOrDefaultAsync(p => p.PreparatID == preparat.PreparatID);

            if (preparatDinDb == null)
            {
                // Tratează cazul în care preparatul nu a fost găsit
                throw new KeyNotFoundException($"Preparatul cu ID {preparat.PreparatID} nu a fost găsit.");
            }

            // Actualizează proprietățile simple ale preparatului
            // _context.Entry(preparatDinDb).CurrentValues.SetValues(preparat); // Atenție, suprascrie și CategorieID, etc.
            // Mai sigur e manual:
            preparatDinDb.Denumire = preparat.Denumire;
            preparatDinDb.Pret = preparat.Pret;
            preparatDinDb.CantitatePortie = preparat.CantitatePortie;
            preparatDinDb.CantitateTotalaStoc = preparat.CantitateTotalaStoc;
            preparatDinDb.UnitateMasuraStoc = preparat.UnitateMasuraStoc;
            preparatDinDb.CategorieID = preparat.CategorieID; // Asigură-te că aceasta e setată corect în obiectul 'preparat'
            preparatDinDb.Descriere = preparat.Descriere;
            preparatDinDb.CaleImagine = preparat.CaleImagine;
            preparatDinDb.EsteActiv = preparat.EsteActiv;
            // Nu actualizăm Categoria direct aici, ci doar CategorieID.

            // Actualizează alergenii
            // 1. Șterge alergenii existenți care nu mai sunt în noua listă
            var alergeniDeSters = preparatDinDb.Alergeni
                                     .Where(a => alergenIds == null || !alergenIds.Contains(a.AlergenID))
                                     .ToList();
            foreach (var alergen in alergeniDeSters)
            {
                preparatDinDb.Alergeni.Remove(alergen);
            }

            // 2. Adaugă noii alergeni care nu există deja
            if (alergenIds != null)
            {
                foreach (var alergenId in alergenIds)
                {
                    if (!preparatDinDb.Alergeni.Any(a => a.AlergenID == alergenId))
                    {
                        var alergenDeAdaugat = await _context.Alergeni.FindAsync(alergenId);
                        if (alergenDeAdaugat != null)
                        {
                            preparatDinDb.Alergeni.Add(alergenDeAdaugat);
                        }
                    }
                }
            }

            _context.Entry(preparatDinDb).State = EntityState.Modified; // Marchează entitatea principală ca modificată
            await _context.SaveChangesAsync();
        }


        public async Task DeleteAsync(int id)
        {
            var preparat = await _context.Preparate.FindAsync(id);
            if (preparat != null)
            {
                preparat.EsteActiv = false;
                _context.Entry(preparat).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Preparate.AnyAsync(p => p.PreparatID == id && p.EsteActiv);
        }

        public async Task<bool> NameExistsAsync(string name, int? currentId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (currentId.HasValue)
            {
                return await _context.Preparate.AnyAsync(p => p.Denumire.ToLower() == name.ToLower() && p.PreparatID != currentId.Value && p.EsteActiv);
            }
            else
            {
                return await _context.Preparate.AnyAsync(p => p.Denumire.ToLower() == name.ToLower() && p.EsteActiv);
            }
        }

        public async Task UpdateStockAsync(int preparatId, decimal cantitateConsumata)
        {
            var preparat = await _context.Preparate.FindAsync(preparatId);
            if (preparat != null)
            {
                if (preparat.CantitateTotalaStoc >= cantitateConsumata)
                {
                    preparat.CantitateTotalaStoc -= cantitateConsumata;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Tratează cazul stocului insuficient (poate aruncă o excepție specifică)
                    throw new InvalidOperationException($"Stoc insuficient pentru preparatul '{preparat.Denumire}'. Stoc actual: {preparat.CantitateTotalaStoc}, Cantitate cerută: {cantitateConsumata}");
                }
            }
            else
            {
                throw new KeyNotFoundException($"Preparatul cu ID {preparatId} nu a fost găsit pentru actualizarea stocului.");
            }
        }
    }
}