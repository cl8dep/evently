using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class CommentEndpoints
{
    const int MaxBody = 2000;

    public static void MapCommentEndpoints(this WebApplication app)
    {
        var comments = app.MapGroup("/api").RequireAuthorization();

        // ponytail: returns the whole thread; add paging when a single event passes a few hundred comments.
        comments.MapGet("/events/{id:int}/comments", async (int id, AppDb db, CancellationToken ct) =>
        {
            if (!await db.Events.AnyAsync(e => e.Id == id, ct)) return Results.NotFound();
            return Results.Ok(await Thread(db, id).ToListAsync(ct));
        }).WithName("ListComments").Produces<List<CommentOut>>();

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

    static IQueryable<CommentOut> Thread(AppDb db, int eventId, int? only = null) =>
        from c in db.Comments.AsNoTracking()
        where c.EventId == eventId && (only == null || c.Id == only)
        join u in db.Users on c.Author equals u.UserName into users
        from u in users.DefaultIfEmpty()
        orderby c.CreatedAt, c.Id
        select new CommentOut(c.Id, c.Author, u == null ? c.Author : (u.FirstName + " " + u.LastName).Trim(), c.Body, c.CreatedAt);
}
