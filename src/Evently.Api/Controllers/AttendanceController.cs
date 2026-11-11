using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

[ApiController, Authorize, Route("api/events/{id:int}"), Produces("application/json")]
public class AttendanceController(AppDb db) : ControllerBase
{
    [HttpGet("attendees"), EndpointName("ListAttendees")]
    public async Task<ActionResult<List<AttendeeOut>>> List(int id, CancellationToken ct)
    {
        if (!await db.Events.AnyAsync(e => e.Id == id, ct)) return NotFound();
        return await AttendanceQueries.Attendees(db, id).ToListAsync(ct);
    }

    [HttpPut("attendance"), EndpointName("SetAttendance")]
    [Consumes("application/json")]
    public async Task<ActionResult<AttendeeOut>> Set(int id, AttendanceIn body, CancellationToken ct)
    {
        if (!Enum.IsDefined(body.Status)) return this.Invalid("status", "Unknown status.");
        if (!await db.Events.AnyAsync(e => e.Id == id, ct)) return NotFound();
        var me = User.Identity!.Name!;
        var row = await db.Attendance.FindAsync([id, me], ct);
        if (row is null) db.Attendance.Add(row = new Attendance { EventId = id, UserName = me });
        row.Status = body.Status;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await AttendanceQueries.Attendees(db, id, me).SingleAsync(ct);
    }

    [HttpDelete("attendance"), EndpointName("ClearAttendance")]
    public async Task<ActionResult> Clear(int id, CancellationToken ct)
    {
        await db.Attendance.Where(a => a.EventId == id && a.UserName == User.Identity!.Name).ExecuteDeleteAsync(ct);
        return NoContent();
    }
}
