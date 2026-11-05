using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        var events = app.MapGroup("/api/events").RequireAuthorization();

        events.MapGet("/", async ([AsParameters] EventFilter f, AppDb db) =>
            await db.Events.WithDetails().Filter(f).OrderBy(e => e.StartDate).ToListAsync());

        events.MapGet("/export.csv", async ([AsParameters] EventFilter f, AppDb db) =>
        {
            var list = await db.Events.WithDetails().Filter(f).OrderBy(e => e.StartDate).ToListAsync();
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(Csv.Build(list))).ToArray();
            return Results.File(bytes, "text/csv; charset=utf-8", $"events-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        });

        events.MapPost("/import", async (HttpRequest req, AppDb db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            using var reader = new StreamReader(req.Body, Encoding.UTF8);
            var result = await EventImporter.Run(await reader.ReadToEndAsync(ct), db, user.Identity!.Name!, ct);
            return result.Errors.Count > 0 ? Results.UnprocessableEntity(new { error = string.Join(" ", result.Errors) }) : Results.Ok(result);
        }).RequireAuthorization("Editor").WithMetadata(new RequestSizeLimitAttribute(5_000_000));

        events.MapGet("/{id:int}", async (int id, AppDb db) =>
            await db.Events.WithDetails().FirstOrDefaultAsync(e => e.Id == id) is { } e ? Results.Ok(e) : Results.NotFound());

        events.MapGet("/{id:int}/icon", async (int id, AppDb db, HttpResponse res) =>
        {
            var icon = await db.EventIcons.AsNoTracking().FirstOrDefaultAsync(i => i.EventId == id);
            if (icon is null) return Results.NotFound();
            // Icons come from third-party sites: never let a served SVG run script or sniff to another type.
            res.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";
            res.Headers["X-Content-Type-Options"] = "nosniff";
            res.Headers.CacheControl = "private, max-age=3600";
            return Results.File(icon.Data, icon.ContentType);
        });

        events.MapPost("/{id:int}/icon/refresh", async (int id, AppDb db, FaviconFetcher favicons, CancellationToken ct) =>
        {
            var e = await db.Events.WithDetails().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return Results.NotFound();
            if (e.Website is null) return Results.Conflict(new { error = "This event has no website." });
            var site = await favicons.Fetch(e.Website, ct);
            await EventEnrichment.Apply(db, e, site);
            if (site.Icon is null) return Results.Json(new { error = "Could not fetch an icon from the website." }, statusCode: 502);
            return Results.Ok(e);
        }).RequireAuthorization("Editor");

        events.MapPost("/", async (EventIn body, HttpRequest req, AppDb db, ClaimsPrincipal user, FaviconFetcher favicons, CancellationToken ct) =>
        {
            var key = req.Headers["Idempotency-Key"].FirstOrDefault();
            if (key is { Length: > 64 }) return Failure.Invalid("Idempotency-Key", "Key is too long (max 64).");
            if (key is not null && await EventQueries.Seen(db, key, ct) is { } seen) return Results.Ok(seen);
            if (await EventRules.Validate(body, db) is { } problem) return problem;
            var e = new Event { CreatedBy = user.Identity!.Name!, CreatedAt = DateTime.UtcNow };
            EventRules.Apply(e, body);
            await EventRules.SetTopics(db, e, body, ct);
            db.Events.Add(e);
            if (key is not null) db.IdempotencyKeys.Add(new IdempotencyKey { Key = key, Event = e, CreatedAt = DateTime.UtcNow });
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException) when (key is not null)
            {
                // A concurrent request with the same key won the race.
                db.ChangeTracker.Clear();
                if (await EventQueries.Seen(db, key, ct) is { } winner) return Results.Ok(winner);
                throw;
            }
            if (e.Website is not null) await EventEnrichment.Apply(db, e, await favicons.Fetch(e.Website, ct));
            await db.Entry(e).Reference(x => x.Category).LoadAsync(ct);
            return Results.Created($"/api/events/{e.Id}", e);
        }).RequireAuthorization("Editor");

        events.MapPut("/{id:int}", async (int id, EventIn body, AppDb db, FaviconFetcher favicons, CancellationToken ct) =>
        {
            if (await EventRules.Validate(body, db) is { } problem) return problem;
            var e = await db.Events.Include(x => x.Topics).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return Results.NotFound();
            var oldSite = e.Website;
            EventRules.Apply(e, body);
            await EventRules.SetTopics(db, e, body, ct);
            if (e.Website != oldSite)
            {
                await db.EventIcons.Where(i => i.EventId == id).ExecuteDeleteAsync(ct);
                e.IconUpdatedAt = null;
            }
            await db.SaveChangesAsync(ct);
            if (e.Website != oldSite && e.Website is not null) await EventEnrichment.Apply(db, e, await favicons.Fetch(e.Website, ct));
            await db.Entry(e).Reference(x => x.Category).LoadAsync(ct);
            return Results.Ok(e);
        }).RequireAuthorization("Editor");

        events.MapDelete("/{id:int}", async (int id, AppDb db) =>
        {
            var e = await db.Events.FindAsync(id);
            if (e is null) return Results.NotFound();
            db.Events.Remove(e);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("Editor");
    }
}
