using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class CommentEndpoints
{
    public const int MaxBody = 2000;
    const int PageSize = 20;

    public static void MapCommentEndpoints(this WebApplication app)
    {
        var comments = app.MapGroup("/api").RequireAuthorization();

        // Newest page first by id; `before` is the oldest id the client already has. Items come back oldest to newest.
        comments.MapGet("/events/{id:int}/comments", async (int id, int? before, int? limit, AppDb db, CancellationToken ct) =>
        {
            if (!await db.Events.AnyAsync(e => e.Id == id, ct)) return Results.NotFound();
            var size = Math.Clamp(limit ?? PageSize, 1, 100);
            var rows = await Thread(db, id, before: before).Take(size + 1).ToListAsync(ct);
            return Results.Ok(new CommentPage(rows.Take(size).Reverse().ToList(), rows.Count > size));
        }).WithName("ListComments").Produces<CommentPage>();

        comments.MapPost("/events/{id:int}/comments", async (int id, CommentIn body, AppDb db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var text = body.Body?.Trim();
            if (string.IsNullOrEmpty(text) || text.Length > MaxBody) return Failure.Invalid("body", $"Comment is required (max {MaxBody}).");
            if (!await db.Events.AnyAsync(e => e.Id == id, ct)) return Results.NotFound();
            var c = new Comment { EventId = id, Author = user.Identity!.Name!, Body = text, CreatedAt = DateTime.UtcNow };
            db.Comments.Add(c);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/comments/{c.Id}", await Thread(db, id, c.Id).SingleAsync(ct));
        }).WithName("CreateComment").Produces<CommentOut>(201);

        comments.MapPut("/comments/{id:int}", async (int id, CommentIn body, AppDb db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var text = body.Body?.Trim();
            if (string.IsNullOrEmpty(text) || text.Length > MaxBody) return Failure.Invalid("body", $"Comment is required (max {MaxBody}).");
            var c = await db.Comments.FindAsync([id], ct);
            if (c is null) return Results.NotFound();
            if (c.Author != user.Identity!.Name) return Results.Forbid();
            c.Body = text;
            c.EditedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(await Thread(db, c.EventId, c.Id).SingleAsync(ct));
        }).WithName("UpdateComment").Produces<CommentOut>();

        comments.MapDelete("/comments/{id:int}", async (int id, AppDb db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var c = await db.Comments.FindAsync([id], ct);
            if (c is null) return Results.NotFound();
            if (c.Author != user.Identity!.Name && !user.IsInRole(AppRoles.Admin)) return Results.Forbid();
            db.Comments.Remove(c);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).WithName("DeleteComment");
    }

    public static IQueryable<CommentOut> Thread(AppDb db, int eventId, int? only = null, int? before = null) =>
        from c in db.Comments.AsNoTracking()
        where c.EventId == eventId && (only == null || c.Id == only) && (before == null || c.Id < before)
        join u in db.Users on c.Author equals u.UserName into users
        from u in users.DefaultIfEmpty()
        orderby c.Id descending
        select new CommentOut(c.Id, c.Author, u == null ? c.Author : (u.FirstName + " " + u.LastName).Trim(), c.Body, c.CreatedAt, c.EditedAt);
}
