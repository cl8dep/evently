using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

// Reads the file produced by Csv.Build. All or nothing: any bad row rolls the whole import back.
public static class EventImporter
{
    const int MaxRows = 1000;
    const int MaxReported = 10;
    static readonly string[] Required = ["Title", "Category", "Start date"];

    static ImportResult Fail(string message) => new(0, 0, [message]);

    public static async Task<ImportResult> Run(string text, AppDb db, string user, CancellationToken ct)
    {
        var rows = CsvParser.Parse(text.TrimStart('﻿'));
        if (rows.Count == 0) return Fail("The file is empty.");
        var col = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < rows[0].Length; i++) col.TryAdd(rows[0][i].Trim(), i);
        if (Required.Where(h => !col.ContainsKey(h)).ToArray() is { Length: > 0 } missing)
            return Fail($"Missing column: {string.Join(", ", missing)}.");
        if (rows.Count - 1 > MaxRows) return Fail($"Too many rows (max {MaxRows}).");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var categories = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in await db.Categories.ToListAsync(ct)) categories.TryAdd(c.Name, c);
        var topics = new Dictionary<string, Topic>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in await db.Topics.ToListAsync(ct)) topics.TryAdd(t.Name, t);
        var known = (await db.Events.Select(e => new { e.Title, e.StartDate }).ToListAsync(ct))
            .Select(e => (e.Title.ToLowerInvariant(), e.StartDate)).ToHashSet();

        async Task<int> CategoryId(string name)
        {
            if (!categories.TryGetValue(name, out var c))
            {
                categories[name] = c = new Category { Name = name };
                db.Categories.Add(c);
                await db.SaveChangesAsync(ct);
            }
            return c.Id;
        }

        async Task<int> TopicId(string name)
        {
            if (!topics.TryGetValue(name, out var t))
            {
                topics[name] = t = new Topic { Name = name };
                db.Topics.Add(t);
                await db.SaveChangesAsync(ct);
            }
            return t.Id;
        }

        var errors = new List<string>();
        var created = new List<Event>();
        var skipped = 0;
        for (var n = 1; n < rows.Count; n++)
        {
            var r = rows[n];
            if (r.All(string.IsNullOrWhiteSpace)) continue;
            string? Get(string h) => col.TryGetValue(h, out var i) && i < r.Length ? EventRules.Clean(Unguard(r[i])) : null;
            var bad = new List<string>();

            var title = Get("Title");
            var category = Get("Category");
            if (title is null) bad.Add("Title is required.");
            if (category is null) bad.Add("Category is required.");
            else if (category.Length > 100) bad.Add("Category is too long (max 100).");
            var topicNames = (Get("Topics") ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (topicNames.Any(t => t.Length > 100)) bad.Add("Topic is too long (max 100).");

            DateOnly? start = null, end = null;
            if (DateOnly.TryParseExact(Get("Start date"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var s)) start = s;
            else bad.Add("Start date must be yyyy-MM-dd.");
            if (Get("End date") is { } ed)
            {
                if (DateOnly.TryParseExact(ed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var e)) end = e;
                else bad.Add("End date must be yyyy-MM-dd.");
            }
            decimal? cost = null;
            if (Get("Ticket cost") is { } tc)
            {
                if (decimal.TryParse(tc, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var d)) cost = d;
                else bad.Add("Ticket cost must be a number.");
            }
            double? lat = null, lng = null;
            if (Get("Latitude") is { } la)
            {
                if (double.TryParse(la, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) lat = d;
                else bad.Add("Latitude must be a number.");
            }
            if (Get("Longitude") is { } lo)
            {
                if (double.TryParse(lo, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) lng = d;
                else bad.Add("Longitude must be a number.");
            }
            if (bad.Count > 0) { errors.Add($"Row {n + 1}: {string.Join(" ", bad)}"); continue; }

            if (!known.Add((title!.ToLowerInvariant(), start!.Value))) { skipped++; continue; }

            var topicIds = new List<int>();
            foreach (var name in topicNames) topicIds.Add(await TopicId(name));
            var input = new EventIn(
                title, Get("Description"), await CategoryId(category!), Get("Website"), cost, Get("Currency"), start.Value, end,
                new Location
                {
                    Venue = Get("Venue"), Address = Get("Address"), City = Get("City"), Region = Get("Region"),
                    Country = Get("Country"), Latitude = lat, Longitude = lng,
                }, [.. topicIds]);
            if (await EventRules.Validate(input, db) is ProblemHttpResult { ProblemDetails: HttpValidationProblemDetails v })
            {
                errors.Add($"Row {n + 1}: {string.Join(" ", v.Errors.Values.SelectMany(m => m))}");
                continue;
            }
            var ev = new Event { CreatedBy = user, CreatedAt = DateTime.UtcNow };
            EventRules.Apply(ev, input);
            await EventRules.SetTopics(db, ev, input, ct);
            created.Add(ev);
        }

        if (errors.Count > 0)
        {
            if (errors.Count > MaxReported) errors = [.. errors.Take(MaxReported), $"and {errors.Count - MaxReported} more rows with errors."];
            return new ImportResult(0, 0, errors);
        }
        db.Events.AddRange(created);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new ImportResult(created.Count, skipped, []);
    }

    // Undo the formula guard that Csv.Cell adds on export.
    static string Unguard(string v) => v.Length > 1 && v[0] == '\'' && "=+-@\t\r".Contains(v[1]) ? v[1..] : v;
}
