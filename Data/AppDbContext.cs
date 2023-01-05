using CalendarSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CalendarSync.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<CalendarEvent>? CalendarEvents { get; set; }

        public AppDbContext()
        {
            
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var config = new ConfigurationBuilder()
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build()

                optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));
            }
        }

    }
}