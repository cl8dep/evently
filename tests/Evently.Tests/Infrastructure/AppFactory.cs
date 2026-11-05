using Microsoft.Extensions.Configuration;
using Evently.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Evently.Tests;

// Needs the postgres from docker-compose.yml; each factory migrates its own database and drops it on dispose.
public class AppFactory : WebApplicationFactory<Program>
{
    readonly string conn = $"Host=localhost;Port=5433;Database=evently_test_{Guid.NewGuid():N};Username=evently;Password=evently";

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            using var db = new AppDb(new DbContextOptionsBuilder<AppDb>().UseNpgsql(conn).Options);
            db.Database.EnsureDeleted();
        }
        base.Dispose(disposing);
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder b)
    {
        b.UseSetting("ConnectionStrings:Default", conn);
        // Never let a developer's user-secrets key make tests call Google.
        b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["GoogleMaps:ApiKey"] = "" }));
    }
}
