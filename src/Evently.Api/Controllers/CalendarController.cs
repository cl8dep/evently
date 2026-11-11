using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

[ApiController, Authorize, Produces("application/json")]
public class CalendarController(AppDb db, UserManager<AppUser> users) : ControllerBase
{
    string Origin => $"{Request.Scheme}://{Request.Host}";

    // One event, for "Add to calendar".
    [HttpGet("/api/events/{id:int}/ics"), ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult> Single(int id) =>
        await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id) is { } e
            ? File(Encoding.UTF8.GetBytes(Ics.Build([e], e.Title, Origin)), "text/calendar; charset=utf-8", $"event-{id}.ics")
            : NotFound();

    // The feed URL embeds a secret token because calendar apps cannot send cookies.
    [HttpGet("/api/me/calendar"), EndpointName("GetCalendarFeed")]
    public async Task<ActionResult<CalendarOut>> Feed()
    {
        var me = (await users.GetUserAsync(User))!;
        if (me.CalendarToken is null) await Rotate(me);
        return new CalendarOut(FeedPath(me.CalendarToken!));
    }

    [HttpPost("/api/me/calendar"), EndpointName("ResetCalendarFeed")]
    public async Task<ActionResult<CalendarOut>> Reset()
    {
        var me = (await users.GetUserAsync(User))!;
        await Rotate(me);
        return new CalendarOut(FeedPath(me.CalendarToken!));
    }

    [AllowAnonymous, HttpGet("/api/calendar/{token}/events.ics"), ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult> Subscription(string token, [FromQuery] EventFilter f, bool? mine)
    {
        var me = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.CalendarToken == token);
        if (me is null) return NotFound();
        var q = db.Events.AsNoTracking().WithDetails().Filter(f);
        if (mine == true)
            q = q.Where(e => db.Attendance.Any(a => a.EventId == e.Id && a.UserName == me.UserName && a.Status != AttendanceStatus.Declined));
        var list = await q.OrderBy(e => e.StartDate).ToListAsync();
        return Content(Ics.Build(list, mine == true ? "My Evently events" : "Evently events", Origin), "text/calendar; charset=utf-8");
    }

    static string FeedPath(string token) => $"/api/calendar/{token}/events.ics";

    async Task Rotate(AppUser user)
    {
        user.CalendarToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(24));
        await users.UpdateAsync(user);
    }
}
