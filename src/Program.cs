using System.Configuration;
using CalendarSync.Data;
using CalendarSync.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static CalendarSync.Utils.Constants;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("local.settings.json", true, true)
    .AddEnvironmentVariables()
    .Build();

var vaultName = config["KeyVaultName"] ?? throw new ConfigurationErrorsException("Key vault name not found in app settings");
var connString = await new KeyVaultService(vaultName).GetSecretAsync(SQL_DB_CONNECTION_STRING_SECRET_NAME);

var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddScoped<KeyVaultService>(_ => new KeyVaultService(vaultName));
        services.AddScoped<GoogleCalendarService>(_=> new GoogleCalendarService());
        services.AddScoped<OutlookCalendarService>(_=> new OutlookCalendarService());

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connString);
        });

    })
    .ConfigureFunctionsWebApplication()
    .Build();

// apply migrations
using var scope = host.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await dbContext.Database.MigrateAsync();
await dbContext.Database.EnsureCreatedAsync();


host.Run();