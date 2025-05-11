using Microsoft.EntityFrameworkCore;
using RestaurantManagerApp.Models; // Pentru a avea acces la clasele model
using RestaurantManagerApp.Utils;  // Pentru ConfigurationHelper

namespace RestaurantManagerApp.Data
{
    public class RestaurantContext : DbContext
    {
        // DbSet-uri pentru fiecare tabelă/entitate pe care vrei să o gestionezi prin EF
        public DbSet<Categorie> Categorii { get; set; }
        public DbSet<Alergen> Alergeni { get; set; }
        public DbSet<Preparat> Preparate { get; set; }
        public DbSet<Utilizator> Utilizatori { get; set; }
        public DbSet<Meniu> Meniuri { get; set; }
        public DbSet<Comanda> Comenzi { get; set; }
        public DbSet<ElementComanda> ElementeComanda { get; set; }

        public DbSet<MeniuPreparat> MeniuPreparate { get; set; }

        // Tabele de legătură (dacă vrei să le configurezi explicit sau să le interoghezi direct)
        // Adesea, EF le gestionează implicit prin proprietățile de navigare,
        // dar le poți adăuga dacă ai nevoie de control fin sau atribute suplimentare pe legătură.
        // Momentan, nu le vom adăuga ca DbSet-uri separate pe PreparateAlergeni și MeniuPreparate
        // deoarece relațiile sunt definite în modelele Preparat și Meniu.
        // public DbSet<PreparatAlergen> PreparateAlergeni { get; set; } // Exemplu dacă am avea o clasă model pentru legătură
        // public DbSet<MeniuPreparat> MeniuPreparate { get; set; } // Exemplu

        public RestaurantContext()
        {
        }

        // Constructor folosit pentru dependency injection, dacă vom configura asta ulterior
        public RestaurantContext(DbContextOptions<RestaurantContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Citim șirul de conexiune din appsettings.json folosind ConfigurationHelper
                string connectionString = ConfigurationHelper.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Categorie>().ToTable("tblCategorii");
            modelBuilder.Entity<Alergen>().ToTable("tblAlergeni");
            modelBuilder.Entity<Preparat>().ToTable("tblPreparate");
            modelBuilder.Entity<Utilizator>().ToTable("tblUtilizatori");
            modelBuilder.Entity<Meniu>().ToTable("tblMeniuri");
            modelBuilder.Entity<Comanda>().ToTable("tblComenzi");
            modelBuilder.Entity<ElementComanda>().ToTable("tblElementeComanda");
            modelBuilder.Entity<MeniuPreparat>().ToTable("tblMeniuPreparate");

            modelBuilder.Entity<Preparat>()
                .HasMany(p => p.Alergeni)
                .WithMany(a => a.Preparate) // Presupunând că Alergen are ICollection<Preparat> Preparate
                .UsingEntity(j => j.ToTable("tblPreparateAlergeni"));

            // Configurare cheie primară compusă pentru MeniuPreparat
            modelBuilder.Entity<MeniuPreparat>()
                .HasKey(mp => new { mp.MeniuID, mp.PreparatID });

            // Definirea relației Meniu -> MeniuPreparat (One-to-Many)
            modelBuilder.Entity<MeniuPreparat>()
                .HasOne(mp => mp.Meniu) // Un MeniuPreparat are un Meniu
                .WithMany(m => m.MeniuPreparate) // Un Meniu are multe MeniuPreparate
                .HasForeignKey(mp => mp.MeniuID); // Cheia externă este MeniuID

            // Definirea relației Preparat -> MeniuPreparat (One-to-Many)
            modelBuilder.Entity<MeniuPreparat>()
                .HasOne(mp => mp.Preparat) // Un MeniuPreparat are un Preparat
                .WithMany(p => p.MeniuPreparate) // Un Preparat are multe MeniuPreparate (participă în multe meniuri)
                .HasForeignKey(mp => mp.PreparatID); // Cheia externă este PreparatID

            // Configurare pentru ElementComanda - asigură-te că EF înțelege relațiile opționale
            modelBuilder.Entity<ElementComanda>()
                .HasOne(ec => ec.Preparat)
                .WithMany() // Un preparat poate fi în multe elemente de comandă
                .HasForeignKey(ec => ec.PreparatID)
                .IsRequired(false) // PreparatID este nullable
                .OnDelete(DeleteBehavior.Restrict); // Previne ștergerea în cascadă dacă un preparat e șters

            modelBuilder.Entity<ElementComanda>()
                .HasOne(ec => ec.Meniu)
                .WithMany() // Un meniu poate fi în multe elemente de comandă
                .HasForeignKey(ec => ec.MeniuID)
                .IsRequired(false) // MeniuID este nullable
                .OnDelete(DeleteBehavior.Restrict); // Previne ștergerea în cascadă dacă un meniu e șters

            // Considerații pentru ștergere:
            // Pentru majoritatea celorlalte relații, EF va folosi `DeleteBehavior.Cascade` dacă cheia externă
            // este NOT NULL și `DeleteBehavior.ClientSetNull` (sau `Restrict`) dacă este NULLABLE,
            // sau cum e definit în baza de date (ON DELETE CASCADE).
            // Am setat `ON DELETE CASCADE` în SQL pentru tabelele de legătură pure.
            // Pentru legăturile din `ElementeComanda` către `Preparate` și `Meniuri`, am pus `Restrict`
            // pentru a nu șterge automat elementele de comandă dacă un preparat/meniu este șters (mai sigur pentru istoric).
        }
    }
}