using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AISupportTriage.Infrastructure.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Walk up from Infrastructure bin folder to find the API's appsettings.json
        var basePath = Path.Combine(Directory.GetCurrentDirectory(),
            "..", "AISupportTriage.API");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString,
            b => b.MigrationsAssembly("AISupportTriage.Infrastructure"));

        // Pass null for ICurrentTenantService — safe at design time
        return new ApplicationDbContext(optionsBuilder.Options, null);
    }
}