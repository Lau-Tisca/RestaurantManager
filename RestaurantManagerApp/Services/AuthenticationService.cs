using RestaurantManagerApp.DataAccess;
using RestaurantManagerApp.Models;
using System.Threading.Tasks;

namespace RestaurantManagerApp.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUtilizatorRepository _utilizatorRepository;

        public Utilizator? CurrentUser { get; private set; }

        public AuthenticationService(IUtilizatorRepository utilizatorRepository)
        {
            _utilizatorRepository = utilizatorRepository;
        }

        public async Task<Utilizator?> LoginAsync(string email, string parola)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(parola))
                return null;

            var utilizator = await _utilizatorRepository.GetByEmailAsync(email);

            if (utilizator != null && utilizator.EsteActiv)
            {
                // Verifică parola folosind BCrypt
                // BCrypt.Verify returnează true dacă parola corespunde hash-ului
                if (BCrypt.Net.BCrypt.Verify(parola, utilizator.ParolaHash))
                {
                    CurrentUser = utilizator;
                    return utilizator;
                }
            }
            return null; // Login eșuat
        }

        public async Task<bool> RegisterClientAsync(string nume, string prenume, string email, string parola, string? numarTelefon, string? adresaLivrare)
        {
            if (string.IsNullOrWhiteSpace(nume) ||
                string.IsNullOrWhiteSpace(prenume) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(parola))
            {
                // Sau aruncă o excepție specifică pentru validare
                return false;
            }

            if (await _utilizatorRepository.EmailExistsAsync(email))
            {
                // Emailul există deja
                return false;
            }

            // Creează hash-ul pentru parolă
            string parolaHash = BCrypt.Net.BCrypt.HashPassword(parola);

            var newUser = new Utilizator
            {
                Nume = nume,
                Prenume = prenume,
                Email = email.ToLower(), // Stochează emailul în lowercase pentru consistență
                ParolaHash = parolaHash,
                NumarTelefon = numarTelefon,
                AdresaLivrare = adresaLivrare,
                TipUtilizator = "Client", // Implicit pentru această metodă
                // DataInregistrare și EsteActiv sunt setate în repository sau default în model
            };

            await _utilizatorRepository.AddUserAsync(newUser);
            return true; // Înregistrare reușită
        }

        public void Logout()
        {
            CurrentUser = null;
            // Aici ai putea adăuga și alte acțiuni de curățare a stării sesiunii dacă e cazul
        }
    }
}