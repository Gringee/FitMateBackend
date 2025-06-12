using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure
{
    // Ta klasa musi być w tym samym projekcie co AppDbContext (Infrastructure).
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Zakładam, że WebApi z appsettings znajduje się w folderze ../WebApi
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "WebApi");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.Development.json", optional: false)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(configuration.GetConnectionString("Default"));

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
