using Microsoft.Extensions.DependencyInjection;
using RestaurantManagerApp.Data; // Pentru RestaurantContext
using RestaurantManagerApp.DataAccess;
using RestaurantManagerApp.ViewModels;
using RestaurantManagerApp.Views;
// using RestaurantManagerApp.ViewModels;
// using RestaurantManagerApp.Services;
using System.Windows;
using System.Globalization;
using System.Threading;

namespace RestaurantManagerApp
{
    public partial class App : Application
    {
        public static ServiceProvider? ServiceProvider { get; private set; }

        public App()
        {
            CultureInfo ci = new CultureInfo("ro-RO");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage(ci.IetfLanguageTag)));
            InitializeComponent();
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Înregistrează DbContext-ul
            // AddDbContext este o metodă de extensie din Microsoft.EntityFrameworkCore
            // care configurează contextul pentru a fi folosit cu DI.
            // Ea se ocupă și de citirea șirului de conexiune dacă este configurat corect
            // în OnConfiguring sau dacă îi dăm opțiunile direct aici.
            // Deoarece am configurat deja UseSqlServer în OnConfiguring din RestaurantContext,
            // un simplu AddDbContext<RestaurantContext>() ar trebui să fie suficient.
            services.AddDbContext<RestaurantContext>();

            services.AddTransient<ICategorieRepository, CategorieRepository>();
            services.AddTransient<CategoryManagementViewModel>();
            services.AddTransient<CategoryManagementView>();

            services.AddTransient<IAlergenRepository, AlergenRepository>();
            services.AddTransient<AlergenManagementViewModel>();
            services.AddTransient<AlergenManagementView>();

            services.AddTransient<IPreparatRepository, PreparatRepository>();
            services.AddTransient<PreparatManagementViewModel>();
            services.AddTransient<PreparatManagementView>();

            // Aici vom înregistra și alte servicii și ViewModels pe măsură ce le creăm:
            // Exemplu (decomentează și adaptează când le creezi):
            // services.AddTransient<IMainWindowViewModel, MainWindowViewModel>(); // Dacă ai o interfață
            // services.AddTransient<MainWindowViewModel>(); // Dacă nu ai interfață și vrei o instanță nouă de fiecare dată
            // services.AddSingleton<NumeleMeuServiciuSingleton>(); // Pentru servicii care trebuie să fie instanță unică

            // Înregistrează fereastra principală (dacă vrei să o rezolvi prin DI)
            // services.AddTransient<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //var categoryManagementView = ServiceProvider.GetService<CategoryManagementView>();
            //categoryManagementView?.Show();

            //var alergenManagementView = ServiceProvider.GetService<AlergenManagementView>();
            //alergenManagementView?.Show();

            var preparatManagementView = ServiceProvider.GetService<PreparatManagementView>();
            preparatManagementView?.Show();

            // Deschide fereastra principală
            // Dacă ai înregistrat MainWindow pentru DI:
            // var mainWindow = ServiceProvider.GetService<MainWindow>();
            // mainWindow?.Show();

            //// Sau metoda tradițională, dacă nu vrei să o injectezi:
            //var mainWindow = new MainWindow();
            //// Aici ai putea injecta ViewModel-ul în MainWindow dacă MainWindow are un constructor care acceptă ViewModel-ul
            //// Exemplu:
            //// var mainViewModel = ServiceProvider.GetService<MainWindowViewModel>();
            //// mainWindow.DataContext = mainViewModel; // Setezi DataContext-ul
            //mainWindow.Show();
        }
    }
}