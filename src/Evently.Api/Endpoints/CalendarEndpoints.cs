using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class CalendarEndpoints
{
    public static void MapCalendarEndpoints(this WebApplication app)
    {
        // One event, for "Add to calendar".
        app.MapGet("/api/events/{id:int}/ics", async (int id, AppDb db, HttpRequest req) =>
            await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id) is { } e
                ? Results.File(System.Text.Encoding.UTF8.GetBytes(Ics.Build([e], e.Title, Origin(req))), "text/calendar; charset=utf-8", $"event-{id}.ics")
                : Results.NotFound()).RequireAuthorization().ExcludeFromDescription();

        // The feed URL embeds a secret token because calendar apps cannot send cookies.
        app.MapGet("/api/me/calendar", async (ClaimsPrincipal user, UserManager<AppUser> um) =>
        {
            var me = (await um.GetUserAsync(user))!;
            if (me.CalendarToken is null) await Rotate(um, me);
            return new CalendarOut(FeedPath(me.CalendarToken!));
        }).RequireAuthorization().WithName("GetCalendarFeed").Produces<CalendarOut>();

        app.MapPost("/api/me/calendar", async (ClaimsPrincipal user, UserManager<AppUser> um) =>
        {
            var me = (await um.GetUserAsync(user))!;
            await Rotate(um, me);
            return new CalendarOut(FeedPath(me.CalendarToken!));
        }).RequireAuthorization().WithName("ResetCalendarFeed").Produces<CalendarOut>();

        app.MapGet("/api/calendar/{token}/events.ics", async ([AsParameters] EventFilter f, bool? mine, string token, AppDb db, HttpRequest req) =>
        {
            var me = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.CalendarToken == token);
            if (me is null) return Results.NotFound();
            var q = db.Events.AsNoTracking().WithDetails().Filter(f);
            if (mine == true)
                q = q.Where(e => db.Attendance.Any(a => a.EventId == e.Id && a.UserName == me.UserName && a.Status != AttendanceStatus.Declined));
            var list = await q.OrderBy(e => e.StartDate).ToListAsync();
            return Results.Text(Ics.Build(list, mine == true ? "My Evently events" : "Evently events", Origin(req)), "text/calendar; charset=utf-8");
        }).ExcludeFromDescription();
    }

    static string Origin(HttpRequest req) => $"{req.Scheme}://{req.Host}";

    static string FeedPath(string token) => $"/api/calendar/{token}/events.ics";

    static async Task Rotate(UserManager<AppUser> um, AppUser user)
    {
        user.CalendarToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(24));
        await um.UpdateAsync(user);
    }
}
