using System.Text;

namespace Evently.Api;

public static class Ics
{
    public static string Build(IEnumerable<Event> events, string name, string baseUrl)
    {
        var lines = new List<string> { "BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//Evently//EN", "CALSCALE:GREGORIAN", $"X-WR-CALNAME:{Escape(name)}" };
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
        foreach (var e in events)
        {
            var l = e.Location;
            var where = string.Join(", ", new[] { l.Venue, l.Address, l.City, l.Region, l.Country }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var about = string.Join("\n\n", new[] { e.Description, e.Website }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => Escape(x!)));
            lines.Add("BEGIN:VEVENT");
            lines.Add($"UID:event-{e.Id}@evently");
            lines.Add($"DTSTAMP:{stamp}");
            lines.Add($"DTSTART;VALUE=DATE:{e.StartDate:yyyyMMdd}");
            // All-day events end on the day after their last day.
            lines.Add($"DTEND;VALUE=DATE:{(e.EndDate ?? e.StartDate).AddDays(1):yyyyMMdd}");
            lines.Add($"SUMMARY:{Escape(e.Title)}");
            if (about.Length > 0) lines.Add($"DESCRIPTION:{about}");
            if (where.Length > 0) lines.Add($"LOCATION:{Escape(where)}");
            lines.Add($"URL:{baseUrl}/events/{e.Id}");
            lines.Add("END:VEVENT");
        }
        lines.Add("END:VCALENDAR");
        return string.Concat(lines.Select(Fold));
    }

    static string Escape(string s) => s.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r\n", "\\n").Replace("\n", "\\n");

    // Content lines are at most 75 octets; continuation lines start with a space.
    static string Fold(string line)
    {
        var sb = new StringBuilder();
        var size = 0;
        foreach (var r in line.EnumerateRunes())
        {
            var n = r.Utf8SequenceLength;
            if (size + n > 74) { sb.Append("\r\n "); size = 0; }
            sb.Append(r.ToString());
            size += n;
        }
        return sb.Append("\r\n").ToString();
    }
}
