using Microsoft.Extensions.Configuration;
using System.IO;

namespace RestaurantManagerApp.Utils
{
    public static class ConfigurationHelper
    {
        private static IConfigurationRoot _configurationRoot;

        static ConfigurationHelper()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Setează calea către directorul de unde rulează aplicația
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configurationRoot = builder.Build();
        }

        public static string GetConnectionString(string name)
        {
            return _configurationRoot.GetConnectionString(name);
        }

        public static ApplicationSettings GetApplicationSettings()
        {
            return _configurationRoot.GetSection("AppSettings").Get<ApplicationSettings>();
        }
    }
}