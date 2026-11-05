using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class EventRules
{
    public static async Task<IResult?> Validate(EventIn b, AppDb db)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(b.Title) || b.Title.Length > 200) errors["title"] = ["Title is required (max 200)."];
        if (b.EndDate < b.StartDate) errors["endDate"] = ["End date must not be before start date."];
        if (b.TicketCost < 0) errors["ticketCost"] = ["Ticket cost must not be negative."];
        if (!string.IsNullOrWhiteSpace(b.Website) &&
            !(Uri.TryCreate(b.Website, UriKind.Absolute, out var u) && u.Scheme is "http" or "https"))
            errors["website"] = ["Website must be an http(s) URL."];
        if (Clean(b.Location?.Country)?.ToUpperInvariant() is { } cc && !await db.Countries.AnyAsync(c => c.Code == cc))
            errors["location.country"] = ["Country must be a valid ISO 3166-1 code."];
        if (Clean(b.Currency)?.ToUpperInvariant() is { } cur && !await db.Currencies.AnyAsync(c => c.Code == cur))
            errors["currency"] = ["Currency must be a valid ISO 4217 code."];
        if (b.Location?.Latitude is < -90 or > 90) errors["location.latitude"] = ["Latitude must be between -90 and 90."];
        if (b.Location?.Longitude is < -180 or > 180) errors["location.longitude"] = ["Longitude must be between -180 and 180."];
        if (!await db.Categories.AnyAsync(c => c.Id == b.CategoryId)) errors["categoryId"] = ["Category does not exist."];
        var topicIds = b.TopicIds?.Distinct().ToArray() ?? [];
        if (topicIds.Length > 0 && await db.Topics.CountAsync(t => topicIds.Contains(t.Id)) != topicIds.Length) errors["topicIds"] = ["Topic does not exist."];
        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }

    public static async Task SetTopics(AppDb db, Event e, EventIn b, CancellationToken ct)
    {
        var ids = b.TopicIds?.Distinct().ToArray() ?? [];
        e.Topics.Clear();
        e.Topics.AddRange(await db.Topics.Where(t => ids.Contains(t.Id)).ToListAsync(ct));
    }

    public static void Apply(Event e, EventIn b)
    {
        e.Title = b.Title!.Trim();
        e.Description = Clean(b.Description);
        e.CategoryId = b.CategoryId;
        e.Website = Clean(b.Website);
        e.TicketCost = b.TicketCost;
        e.Currency = Clean(b.Currency)?.ToUpperInvariant();
        e.StartDate = b.StartDate;
        e.EndDate = b.EndDate;
        var l = b.Location ?? new Location();
        e.Location = new Location
        {
            Venue = Clean(l.Venue), Address = Clean(l.Address), City = Clean(l.City), Region = Clean(l.Region),
            Country = Clean(l.Country)?.ToUpperInvariant(), Latitude = l.Latitude, Longitude = l.Longitude,
        };
    }

    public static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
