using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class TopicEndpoints
{
    public static void MapTopicEndpoints(this WebApplication app)
    {
        var topics = app.MapGroup("/api/topics").RequireAuthorization();

        topics.MapGet("/", (AppDb db) => db.Topics.OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name, EventCount = db.Events.Count(e => e.Topics.Any(x => x.Id == t.Id)) }).ToListAsync());

        topics.MapPost("/", async (NameIn body, AppDb db) =>
        {
            var name = body.Name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 100) return Failure.Invalid("name", "Name is required (max 100).");
            if (await db.Topics.AnyAsync(t => t.Name == name)) return Results.Conflict(new { error = "Topic already exists." });
            var t = new Topic { Name = name };
            db.Topics.Add(t);
            await db.SaveChangesAsync();
            return Results.Created($"/api/topics/{t.Id}", t);
        }).RequireAuthorization("Editor");

        topics.MapPut("/{id:int}", async (int id, NameIn body, AppDb db) =>
        {
            var name = body.Name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 100) return Failure.Invalid("name", "Name is required (max 100).");
            var t = await db.Topics.FindAsync(id);
            if (t is null) return Results.NotFound();
            if (await db.Topics.AnyAsync(x => x.Name == name && x.Id != id)) return Results.Conflict(new { error = "Topic already exists." });
            t.Name = name;
            await db.SaveChangesAsync();
            return Results.Ok(t);
        }).RequireAuthorization("Editor");

        topics.MapDelete("/{id:int}", async (int id, AppDb db) =>
        {
            var t = await db.Topics.FindAsync(id);
            if (t is null) return Results.NotFound();
            if (await db.Events.AnyAsync(e => e.Topics.Any(x => x.Id == id))) return Results.Conflict(new { error = "Topic is used by events." });
            db.Topics.Remove(t);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("Editor");
    }
}
