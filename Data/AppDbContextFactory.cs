using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContestApi.Data;

/// <summary>
/// Used only by the EF Core CLI tools (dotnet ef migrations / database update).
/// The connection string here is for local development only — production uses
/// the value from Key Vault / App Service environment variables at runtime.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Read from the environment variable first so CI/CD or a .env can override,
        // then fall back to the local appsettings.Development.json value.
        var connStr =
            Environment.GetEnvironmentVariable("ConnectionStrings__Sql")
            ?? "Server=localhost;Database=ContestDb;Trusted_Connection=True;TrustServerCertificate=True;";

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connStr)
            .Options;

        return new AppDbContext(opts);
    }
}
