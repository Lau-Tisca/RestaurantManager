using Microsoft.EntityFrameworkCore;
using RestaurantManagerApp.Data;
using RestaurantManagerApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantManagerApp.DataAccess
{
    public class MeniuRepository : IMeniuRepository
    {
        private readonly RestaurantContext _context;

        public MeniuRepository(RestaurantContext context)
        {
            _context = context;
        }

        public async Task<List<Meniu>> GetAllActiveWithDetailsAsync()
        {
            return await _context.Meniuri
                                 .Where(m => m.EsteActiv)
                                 .Include(m => m.Categorie)
                                 .Include(m => m.MeniuPreparate) // Include tabela de joncțiune
                                     .ThenInclude(mp => mp.Preparat) // Apoi, din joncțiune, include Preparatul
                                 .OrderBy(m => m.Denumire)
                                 .ToListAsync();
        }

        public async Task<Meniu?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Meniuri
                                 .Where(m => m.MeniuID == id) // Nu filtram pe EsteActiv aici, pentru a putea edita și un meniu inactiv
                                 .Include(m => m.Categorie)
                                 .Include(m => m.MeniuPreparate)
                                     .ThenInclude(mp => mp.Preparat)
                                 .FirstOrDefaultAsync();
        }

        public async Task<int> AddAsync(Meniu meniu, List<MeniuPreparat> preparateComponente)
        {
            if (meniu == null) throw new ArgumentNullException(nameof(meniu));

            meniu.EsteActiv = true;
            meniu.Categorie = null; // Asigură-te că nu e setat dacă adaugi prin CategorieID
            meniu.MeniuPreparate = new List<MeniuPreparat>(); // Inițializează

            _context.Meniuri.Add(meniu);
            await _context.SaveChangesAsync(); // Salvează meniul pentru a obține MeniuID

            // Adaugă preparatele componente
            if (preparateComponente != null && preparateComponente.Any())
            {
                foreach (var comp in preparateComponente)
                {
                    // Asociază componenta cu MeniuID-ul nou creat
                    comp.MeniuID = meniu.MeniuID;
                    // Asigură-te că Preparatul este urmărit de context sau are un ID valid
                    // Dacă PreparatID vine de la un obiect Preparat ne-urmărit, EF s-ar putea să încerce să-l creeze.
                    // Cel mai sigur este să avem doar PreparatID și să nu setăm obiectul Preparat în comp.
                    comp.Preparat = null; // Evită inserarea unui nou preparat dacă comp.Preparat e un obiect nou
                    _context.MeniuPreparate.Add(comp);
                }
                await _context.SaveChangesAsync();
            }
            return meniu.MeniuID;
        }

        public async Task UpdateAsync(Meniu meniu, List<MeniuPreparat> noiPreparateComponente)
        {
            if (meniu == null) throw new ArgumentNullException(nameof(meniu));

            var meniuDinDb = await _context.Meniuri
                                           .Include(m => m.MeniuPreparate) // Important să includem componentele existente
                                           .FirstOrDefaultAsync(m => m.MeniuID == meniu.MeniuID);

            if (meniuDinDb == null)
            {
                throw new KeyNotFoundException($"Meniul cu ID {meniu.MeniuID} nu a fost găsit.");
            }

            // Actualizează proprietățile simple ale meniului
            meniuDinDb.Denumire = meniu.Denumire;
            meniuDinDb.CategorieID = meniu.CategorieID;
            meniuDinDb.Descriere = meniu.Descriere;
            meniuDinDb.CaleImagine = meniu.CaleImagine;
            meniuDinDb.EsteActiv = meniu.EsteActiv;

            // Actualizează preparatele componente (abordare: șterge vechile și adaugă noile)
            // Aceasta este cea mai simplă abordare pentru a gestiona schimbările în colecția de joncțiune.
            // Pentru performanță pe seturi mari de date, s-ar putea optimiza.

            // Șterge toate componentele vechi
            _context.MeniuPreparate.RemoveRange(meniuDinDb.MeniuPreparate);
            // Trebuie salvat aici pentru a evita conflicte de cheie dacă un preparat vechi e și unul nou
            // await _context.SaveChangesAsync(); // Sau gestionează mai fin mai jos

            // Adaugă noile componente
            if (noiPreparateComponente != null && noiPreparateComponente.Any())
            {
                foreach (var comp in noiPreparateComponente)
                {
                    comp.MeniuID = meniuDinDb.MeniuID; // Asigură MeniuID corect
                    comp.Meniu = null; // Evită conflicte de urmărire
                    comp.Preparat = null; // Evită conflicte de urmărire
                    // _context.MeniuPreparate.Add(comp); // Adaugă direct în DbSet-ul de joncțiune
                    meniuDinDb.MeniuPreparate.Add(comp); // Sau adaugă la colecția de navigare a meniului din DB
                }
            }
            // Marchează entitatea principală ca modificată (deși proprietățile au fost actualizate direct)
            // _context.Entry(meniuDinDb).State = EntityState.Modified; // Nu e neapărat necesar dacă actualizezi direct proprietățile entității urmărite

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var meniu = await _context.Meniuri.FindAsync(id);
            if (meniu != null)
            {
                meniu.EsteActiv = false;
                _context.Entry(meniu).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> NameExistsAsync(string name, int? currentId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (currentId.HasValue)
            {
                return await _context.Meniuri.AnyAsync(m => m.Denumire.ToLower() == name.ToLower() && m.MeniuID != currentId.Value && m.EsteActiv);
            }
            else
            {
                return await _context.Meniuri.AnyAsync(m => m.Denumire.ToLower() == name.ToLower() && m.EsteActiv);
            }
        }
    }
}