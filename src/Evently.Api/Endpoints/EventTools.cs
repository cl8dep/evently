using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Evently.Api;

// MCP tools act as the owner of the API key, so they never do more than that user could in the app.
[McpServerToolType]
public class EventTools(AppDb db, IHttpContextAccessor http)
{
    string Me => http.HttpContext!.User.Identity!.Name!;

    [McpServerTool(Name = "list_events", ReadOnly = true), Description("List events by start date. Filters are optional; `from` is a yyyy-MM-dd date.")]
    public async Task<List<Event>> ListEvents(string? query = null, string? country = null, DateOnly? from = null, int limit = 20, CancellationToken ct = default)
    {
        var q = db.Events.AsNoTracking().WithDetails().Filter(new EventFilter(null, null, country, null, query));
        if (from is { } d) q = q.Where(e => e.StartDate >= d || e.EndDate >= d);
        return await q.OrderBy(e => e.StartDate).Take(Math.Clamp(limit, 1, 100)).ToListAsync(ct);
    }

    [McpServerTool(Name = "get_event", ReadOnly = true), Description("Get one event with its category, topics and location.")]
    public async Task<Event> GetEvent(int id, CancellationToken ct = default) =>
        await db.Events.AsNoTracking().WithDetails().FirstOrDefaultAsync(e => e.Id == id, ct) ?? throw new McpException($"Event {id} not found.");

    [McpServerTool(Name = "list_attendees", ReadOnly = true), Description("Who is going, maybe going or not going to an event.")]
    public async Task<List<AttendeeOut>> ListAttendees(int eventId, CancellationToken ct = default) =>
        await AttendanceEndpoints.Attendees(db, eventId).ToListAsync(ct);

    [McpServerTool(Name = "list_comments", ReadOnly = true), Description("The latest comments of an event, oldest first. Pass `before` (a comment id) for older ones.")]
    public async Task<List<CommentOut>> ListComments(int eventId, int? before = null, int limit = 20, CancellationToken ct = default) =>
        (await CommentEndpoints.Thread(db, eventId, before: before).Take(Math.Clamp(limit, 1, 100)).ToListAsync(ct)).AsEnumerable().Reverse().ToList();

    [McpServerTool(Name = "add_comment", Destructive = false), Description("Post a markdown comment on an event as the API key owner.")]
    public async Task<CommentOut> AddComment(int eventId, string body, CancellationToken ct = default)
    {
        var text = body.Trim();
        if (text.Length == 0 || text.Length > CommentEndpoints.MaxBody) throw new McpException($"Comment is required (max {CommentEndpoints.MaxBody}).");
        if (!await db.Events.AnyAsync(e => e.Id == eventId, ct)) throw new McpException($"Event {eventId} not found.");
        var c = new Comment { EventId = eventId, Author = Me, Body = text, CreatedAt = DateTime.UtcNow };
        db.Comments.Add(c);
        await db.SaveChangesAsync(ct);
        return await CommentEndpoints.Thread(db, eventId, c.Id).SingleAsync(ct);
    }

    [McpServerTool(Name = "set_attendance", Idempotent = true, Destructive = false), Description("Set the API key owner's status for an event: Going, Maybe or Declined.")]
    public async Task<AttendeeOut> SetAttendance(int eventId, AttendanceStatus status, CancellationToken ct = default)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId, ct)) throw new McpException($"Event {eventId} not found.");
        var row = await db.Attendance.FindAsync([eventId, Me], ct);
        if (row is null) db.Attendance.Add(row = new Attendance { EventId = eventId, UserName = Me });
        row.Status = status;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await AttendanceEndpoints.Attendees(db, eventId, Me).SingleAsync(ct);
    }
}
