using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class AttendanceEndpoints
{
    public static void MapAttendanceEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/events/{id:int}").RequireAuthorization();

        g.MapGet("/attendees", async (int id, AppDb db, CancellationToken ct) =>
        {
            if (!await db.Events.AnyAsync(e => e.Id == id, ct)) return Results.NotFound();
            return Results.Ok(await Attendees(db, id).ToListAsync(ct));
        }).WithName("ListAttendees").Produces<List<AttendeeOut>>();

        g.MapPut("/attendance", async (int id, AttendanceIn body, AppDb db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!Enum.IsDefined(body.Status)) return Failure.Invalid("status", "Unknown status.");
            if (!await db.Events.AnyAsync(e => e.Id == id, ct)) return Results.NotFound();
            var me = user.Identity!.Name!;
            var row = await db.Attendance.FindAsync([id, me], ct);
            if (row is null) db.Attendance.Add(row = new Attendance { EventId = id, UserName = me });
            row.Status = body.Status;
            row.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(await Attendees(db, id, me).SingleAsync(ct));
        }).RequireAuthorization().WithName("SetAttendance").Produces<AttendeeOut>();

        g.MapDelete("/attendance", async (int id, AppDb db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            await db.Attendance.Where(a => a.EventId == id && a.UserName == user.Identity!.Name).ExecuteDeleteAsync(ct);
            return Results.NoContent();
        }).WithName("ClearAttendance");
    }

    public static IQueryable<AttendeeOut> Attendees(AppDb db, int eventId, string? only = null) =>
        from a in db.Attendance.AsNoTracking()
        where a.EventId == eventId && (only == null || a.UserName == only)
        join u in db.Users on a.UserName equals u.UserName into users
        from u in users.DefaultIfEmpty()
        orderby a.UpdatedAt
        select new AttendeeOut(a.UserName, u == null || (u.FirstName + u.LastName) == "" ? a.UserName : (u.FirstName + " " + u.LastName).Trim(), a.Status);
}
