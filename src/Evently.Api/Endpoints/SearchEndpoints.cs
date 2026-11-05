using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class SearchEndpoints
{
    const int Limit = 6;

    public static void MapSearchEndpoints(this WebApplication app)
    {
        app.MapGet("/api/search", async (string? q, HttpContext http, AppDb db, CancellationToken ct) =>
        {
            var term = q?.Trim();
            if (string.IsNullOrEmpty(term) || term.Length < 2 || term.Length > 100) return Results.Ok(Empty);
            var like = $"%{term}%";

            var events = await db.Events
                .Where(e => EF.Functions.ILike(e.Title, like) || EF.Functions.ILike(e.Description!, like) || EF.Functions.ILike(e.Website!, like)
                    || EF.Functions.ILike(e.Location.City!, like) || EF.Functions.ILike(e.Location.Venue!, like)
                    || EF.Functions.ILike(e.Category!.Name, like) || e.Topics.Any(t => EF.Functions.ILike(t.Name, like)))
                .OrderBy(e => e.StartDate)
                .Take(Limit)
                .Select(e => new { e.Id, e.Title, e.StartDate, e.Location.City, e.Location.Country, e.IconUpdatedAt })
                .ToListAsync(ct);
            var categories = await db.Categories.Where(c => EF.Functions.ILike(c.Name, like)).OrderBy(c => c.Name).Take(Limit)
                .Select(c => new { c.Id, c.Name }).ToListAsync(ct);
            var topics = await db.Topics.Where(t => EF.Functions.ILike(t.Name, like)).OrderBy(t => t.Name).Take(Limit)
                .Select(t => new { t.Id, t.Name }).ToListAsync(ct);
            var users = http.User.IsInRole(AppRoles.Admin)
                ? await db.Users.Where(u => EF.Functions.ILike(u.Email!, like) || EF.Functions.ILike(u.FirstName, like) || EF.Functions.ILike(u.LastName, like))
                    .OrderBy(u => u.Email).Take(Limit).Select(u => new { u.Id, u.FirstName, u.LastName, u.Email }).ToListAsync(ct)
                : [];

            return Results.Ok(new { events, categories, topics, users });
        }).RequireAuthorization();
    }

    static readonly object Empty = new { events = Array.Empty<object>(), categories = Array.Empty<object>(), topics = Array.Empty<object>(), users = Array.Empty<object>() };
}
