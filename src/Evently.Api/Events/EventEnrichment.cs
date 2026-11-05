using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class EventEnrichment
{
    // Saves what the site gave us; the description only fills an empty one.
    public static async Task Apply(AppDb db, Event e, Site site)
    {
        if (string.IsNullOrWhiteSpace(e.Description)) e.Description = site.Description;
        if (site.Icon is { } icon) await SaveIcon(db, e, icon);
        else await db.SaveChangesAsync();
    }

    static async Task SaveIcon(AppDb db, Event e, Icon icon)
    {
        var row = await db.EventIcons.FindAsync(e.Id);
        if (row is null) db.EventIcons.Add(new EventIcon { EventId = e.Id, Data = icon.Data, ContentType = icon.ContentType });
        else { row.Data = icon.Data; row.ContentType = icon.ContentType; }
        e.IconUpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
