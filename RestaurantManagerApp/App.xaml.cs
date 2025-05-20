using Microsoft.Extensions.DependencyInjection;
using RestaurantManagerApp.Data; // Pentru RestaurantContext
using RestaurantManagerApp.DataAccess;
using RestaurantManagerApp.ViewModels;
using RestaurantManagerApp.Views;
// using RestaurantManagerApp.ViewModels;
using RestaurantManagerApp.Services;
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
            services.AddTransient<IAlergenRepository, AlergenRepository>();
            services.AddTransient<IPreparatRepository, PreparatRepository>();
            services.AddTransient<IMeniuRepository, MeniuRepository>();
            services.AddTransient<IUtilizatorRepository, UtilizatorRepository>();

            // Servicii
            services.AddSingleton<IAuthenticationService, AuthenticationService>();

            // ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegistrationViewModel>();
            services.AddTransient<CategoryManagementViewModel>();
            services.AddTransient<AlergenManagementViewModel>();
            services.AddTransient<PreparatManagementViewModel>();
            services.AddTransient<MeniuManagementViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();
            services.AddTransient<EmployeeDashboardViewModel>();

            // Views (ca UserControls, nu Windows, dacă sunt în ContentControl)
            // Vom modifica acest aspect la pasul următor
            // Momentan, dacă sunt Windows, nu le înregistrăm aici pentru a fi puse în ContentControl.
            // Le vom deschide direct dacă sunt Windows, sau le vom transforma în UserControl.
            // Pentru DataTemplates, nu e nevoie să le înregistrăm în DI ca tipuri de View.
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (ServiceProvider == null)
            {
                MessageBox.Show("Eroare critică: ServiceProvider nu a fost inițializat.", "Eroare Pornire", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            //var categoryManagementView = ServiceProvider.GetService<CategoryManagementView>();
            //categoryManagementView?.Show();

            //var alergenManagementView = ServiceProvider.GetService<AlergenManagementView>();
            //alergenManagementView?.Show();

            //var preparatManagementView = ServiceProvider.GetService<PreparatManagementView>();
            //preparatManagementView?.Show();

            //var meniuManagementView = ServiceProvider.GetService<MeniuManagementView>();
            //meniuManagementView?.Show();

            var mainWindow = ServiceProvider.GetService<MainWindow>();
            if (mainWindow != null)
            {
                mainWindow.Show();
            }
            else
            {
                MessageBox.Show("Eroare critică: MainWindow nu a putut fi creată.", "Eroare Pornire", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }

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