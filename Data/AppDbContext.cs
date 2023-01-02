using CalendarSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CalendarSync.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<CalendarEvent>? CalendarEvents { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Get the connection string from the secrets file and key vault on azure
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");

            // Use the connection string to configure the context
            optionsBuilder.UseSqlServer(connectionString);
        }

    }
}