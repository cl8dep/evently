using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class AttendanceQueries
{
    public static IQueryable<AttendeeOut> Attendees(AppDb db, int eventId, string? only = null) =>
        from a in db.Attendance.AsNoTracking()
        where a.EventId == eventId && (only == null || a.UserName == only)
        join u in db.Users on a.UserName equals u.UserName into users
        from u in users.DefaultIfEmpty()
        orderby a.UpdatedAt
        select new AttendeeOut(a.UserName, u == null || (u.FirstName + u.LastName) == "" ? a.UserName : (u.FirstName + " " + u.LastName).Trim(), a.Status);
}
