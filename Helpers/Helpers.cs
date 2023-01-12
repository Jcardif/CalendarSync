using Microsoft.Extensions.Configuration;

namespace CalendarSync.Helpers
{
    public static class Helpers
    {
        public static void GetAppSettings()
        {
            // Get the connection string from the secrets file and key vault on azure
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            ConnectionString = config.GetConnectionString("DefaultConnection");

            KeyVaultUri = config["AzureKeyVault:KeyVaultUrl"];
            KeyVaultSecretName = config["AzureKeyVault:SecretName"];
            CalendarId = config["GoogleCloudConsole:CalendarId"];
        }
    }
}