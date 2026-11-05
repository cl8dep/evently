using System.Globalization;
using System.Text;

namespace Evently.Api;

public static class Csv
{
    static readonly string[] Header =
    [
        "Title", "Description", "Category", "Topics", "Website", "Ticket cost", "Currency", "Start date", "End date",
        "Venue", "Address", "City", "Region", "Country", "Latitude", "Longitude",
    ];

    public static string Build(IEnumerable<Event> events)
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(',', Header)).Append("\r\n");
        foreach (var e in events)
        {
            var l = e.Location;
            string?[] row =
            [
                e.Title, e.Description, e.Category?.Name, string.Join(", ", e.Topics.Select(t => t.Name).Order()), e.Website,
                e.TicketCost?.ToString(CultureInfo.InvariantCulture), e.Currency,
                e.StartDate.ToString("yyyy-MM-dd"), e.EndDate?.ToString("yyyy-MM-dd"),
                l.Venue, l.Address, l.City, l.Region, l.Country,
                l.Latitude?.ToString(CultureInfo.InvariantCulture), l.Longitude?.ToString(CultureInfo.InvariantCulture),
            ];
            sb.Append(string.Join(',', row.Select(Cell))).Append("\r\n");
        }
        return sb.ToString();
    }

    // Leading = + - @ (and tab/CR) would be run as formulas by Excel, so prefix a quote.
    public static string Cell(string? v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        if ("=+-@\t\r".Contains(v[0])) v = "'" + v;
        return v.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
    }
}
