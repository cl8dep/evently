using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class LookupEndpoints
{
    public static void MapLookupEndpoints(this WebApplication app)
    {
        app.MapGet("/api/countries", (AppDb db) => db.Countries.OrderBy(c => c.Name).ToListAsync()).RequireAuthorization().WithName("ListCountries");
        app.MapGet("/api/currencies", (AppDb db) => db.Currencies.OrderBy(c => c.Code).ToListAsync()).RequireAuthorization().WithName("ListCurrencies");
    }
}
