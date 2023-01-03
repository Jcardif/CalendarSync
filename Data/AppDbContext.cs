using CalendarSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CalendarSync.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<CalendarEvent>? CalendarEvents { get; set; }
        public string ConnectionString { get; }

        public AppDbContext(string connectionString)
        {
            ConnectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            
            // Use the connection string to configure the context
            optionsBuilder.UseSqlServer(ConnectionString);
        }

    }
}