using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using ReliableAcademy.Data;

namespace ReliableAcademy.Data;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true);

        // Load the developer's local User Secrets for EF tooling without putting credentials in source control.
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            configurationBuilder.AddUserSecrets(typeof(ApplicationDbContext).Assembly, optional: true);
        }

        var configuration = configurationBuilder
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection connection string is missing.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString, sqlServerOptions =>
                sqlServerOptions.MigrationsHistoryTable("RCI_EFMigrationsHistory"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
