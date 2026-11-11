using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class CommentQueries
{
    public const int MaxBody = 2000;

    // Newest first. Filter before projecting: EF cannot translate a filter on the constructed record.
    public static IQueryable<CommentOut> Thread(AppDb db, int eventId, int? only = null, int? before = null) =>
        from c in db.Comments.AsNoTracking()
        where c.EventId == eventId && (only == null || c.Id == only) && (before == null || c.Id < before)
        join u in db.Users on c.Author equals u.UserName into users
        from u in users.DefaultIfEmpty()
        orderby c.Id descending
        select new CommentOut(c.Id, c.Author, u == null ? c.Author : (u.FirstName + " " + u.LastName).Trim(), c.Body, c.CreatedAt, c.EditedAt);
}
