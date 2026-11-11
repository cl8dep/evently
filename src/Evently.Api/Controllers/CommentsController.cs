using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

[ApiController, Authorize, Produces("application/json")]
public class CommentsController(AppDb db) : ControllerBase
{
    const int PageSize = 20;

    // Newest page first by id; `before` is the oldest id the client already has. Items come back oldest to newest.
    [HttpGet("/api/events/{id:int}/comments"), EndpointName("ListComments")]
    public async Task<ActionResult<CommentPage>> List(int id, int? before, int? limit, CancellationToken ct)
    {
        if (!await db.Events.AnyAsync(e => e.Id == id, ct)) return NotFound();
        var size = Math.Clamp(limit ?? PageSize, 1, 100);
        var rows = await CommentQueries.Thread(db, id, before: before).Take(size + 1).ToListAsync(ct);
        return new CommentPage(rows.Take(size).Reverse().ToList(), rows.Count > size);
    }

    [HttpPost("/api/events/{id:int}/comments"), EndpointName("CreateComment")]
    [ProducesResponseType<CommentOut>(StatusCodes.Status201Created)]
    [Consumes("application/json")]
    public async Task<ActionResult> Create(int id, CommentIn body, CancellationToken ct)
    {
        var text = body.Body?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > CommentQueries.MaxBody) return this.Invalid("body", $"Comment is required (max {CommentQueries.MaxBody}).");
        if (!await db.Events.AnyAsync(e => e.Id == id, ct)) return NotFound();
        var c = new Comment { EventId = id, Author = User.Identity!.Name!, Body = text, CreatedAt = DateTime.UtcNow };
        db.Comments.Add(c);
        await db.SaveChangesAsync(ct);
        return Created($"/api/comments/{c.Id}", await CommentQueries.Thread(db, id, c.Id).SingleAsync(ct));
    }

    [HttpPut("/api/comments/{id:int}"), EndpointName("UpdateComment")]
    [Consumes("application/json")]
    public async Task<ActionResult<CommentOut>> Update(int id, CommentIn body, CancellationToken ct)
    {
        var text = body.Body?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > CommentQueries.MaxBody) return this.Invalid("body", $"Comment is required (max {CommentQueries.MaxBody}).");
        var c = await db.Comments.FindAsync([id], ct);
        if (c is null) return NotFound();
        if (c.Author != User.Identity!.Name) return Forbid();
        c.Body = text;
        c.EditedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await CommentQueries.Thread(db, c.EventId, c.Id).SingleAsync(ct);
    }

    [HttpDelete("/api/comments/{id:int}"), EndpointName("DeleteComment")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var c = await db.Comments.FindAsync([id], ct);
        if (c is null) return NotFound();
        if (c.Author != User.Identity!.Name && !User.IsInRole(AppRoles.Admin)) return Forbid();
        db.Comments.Remove(c);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
