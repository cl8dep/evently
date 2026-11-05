using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class EventQueries
{
    public static IQueryable<Event> WithDetails(this IQueryable<Event> q) => q.Include(e => e.Category).Include(e => e.Topics);

    public static IQueryable<Event> Filter(this IQueryable<Event> q, EventFilter f)
    {
        if (f.CategoryId is { } c) q = q.Where(e => e.CategoryId == c);
        if (f.TopicId is { } t) q = q.Where(e => e.Topics.Any(x => x.Id == t));
        if (!string.IsNullOrWhiteSpace(f.Country)) q = q.Where(e => e.Location.Country == f.Country.ToUpper());
        if (!string.IsNullOrWhiteSpace(f.Region)) q = q.Where(e => e.Location.Region == f.Region);
        if (!string.IsNullOrWhiteSpace(f.Q))
            q = q.Where(e => EF.Functions.ILike(e.Title, $"%{f.Q}%") || EF.Functions.ILike(e.Description!, $"%{f.Q}%") || EF.Functions.ILike(e.Location.City!, $"%{f.Q}%"));
        return q;
    }

    public static Task<Event?> Seen(AppDb db, string key, CancellationToken ct) =>
        db.Events.WithDetails().Where(e => db.IdempotencyKeys.Any(k => k.Key == key && k.EventId == e.Id)).FirstOrDefaultAsync(ct);
}
