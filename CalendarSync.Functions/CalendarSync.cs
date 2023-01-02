using System.IO;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CalendarSync.Functions
{
    public static class CalendarSync
    {
        [FunctionName("CalendarSync")]
        public static void Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Read the values for the new row from the body of the request
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            Product product = JsonConvert.DeserializeObject<Product>(requestBody);

            // Use EF Core to insert a record into the "Products" table
            using (var context = new ProductsContext())
            {
                // Apply any pending migrations
                context.Database.Migrate();

                context.Products.Add(product);
                context.SaveChanges();
            }
        }
    }

    // The ProductsContext class represents the context for the Products database
    public class ProductsContext : DbContext
    {
        public DbSet<Product> Products { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Get the connection string from the secrets file and key vault on azure
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("secrets.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            
            var connectionString = config.GetConnectionString("DefaultConnection");

            // Use the connection string to configure the context
            optionsBuilder.UseSqlServer(connectionString);
        }

    }

    // The Product class represents a row in the Products table
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
    }
}