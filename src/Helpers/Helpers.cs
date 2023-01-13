using Microsoft.Extensions.Configuration;

namespace CalendarSync.Helpers
{
    public static class Helpers
    {
        public static AppSettings GetAppSettings()
        {
            // Get the connection string from the secrets file and key vault on azure
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");
            var keyVaultUri = config["AzureKeyVault:KeyVaultUrl"];
            var keyVaultSecretName = config["AzureKeyVault:SecretName"];
            var calendarId = config["GoogleCloudConsole:CalendarId"];

            var clientId = config["AzureAd:ClientId"];
            var clientSecret = config["AzureAd:ClientSecret"];
            var tenantId = config["AzureAd:TenantId"];
            var userPrincipalName = config["AzureAd:UserPrincipalName"];

            return new AppSettings
            {
                ConnectionString=connectionString,
                KeyVaultSecretName=keyVaultSecretName,
                KeyVaultUri=keyVaultUri,
                CalendarId=calendarId,
                ClientId=clientId,
                ClientSecret=clientSecret,
                TenantId=tenantId,
                UserPrincipalName=userPrincipalName
            };
        }
    }
}