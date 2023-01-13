namespace CalendarSync.Helpers
{
    // ToDo: use dependency injection to configure this once
    public class AppSettings
    {
        public string? ConnectionString { get; set; }
        public string? KeyVaultUri { get; set; }
        public string? KeyVaultSecretName { get; set; }
        public string? CalendarId { get; set; }
        public string? ClientSecret { get; set; }
        public string? TenantId { get; set; }
        public string? UserPrincipalName { get; set; }
        public string? ClientId { get; set; }


    }
}