using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Evently.Api;

// Every Google call happens here so the API key never reaches the browser.
public sealed class PlacesClient(HttpClient http, IConfiguration config)
{
    const string Places = "https://places.googleapis.com/v1";
    readonly string? key = config["GoogleMaps:ApiKey"];

    public bool Enabled => !string.IsNullOrEmpty(key);

    public async Task<IReadOnlyList<PlaceSuggestion>> Suggest(string input, string? session, CancellationToken ct)
    {
        var body = new Dictionary<string, object> { ["input"] = input, ["languageCode"] = "en" };
        if (session is not null) body["sessionToken"] = session;
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{Places}/places:autocomplete") { Content = JsonContent.Create(body) };
        req.Headers.Add("X-Goog-Api-Key", key);
        return ParseSuggestions(await Send(req, ct));
    }

    public async Task<Location?> Details(string placeId, string? session, CancellationToken ct)
    {
        var url = $"{Places}/places/{Uri.EscapeDataString(placeId)}?languageCode=en";
        if (session is not null) url += $"&sessionToken={Uri.EscapeDataString(session)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-Goog-Api-Key", key);
        req.Headers.Add("X-Goog-FieldMask", "displayName,formattedAddress,addressComponents,location");
        using var res = await http.SendAsync(req, ct);
        if (res.StatusCode == HttpStatusCode.NotFound) return null;
        return ParseDetails(await Read(res, ct));
    }

    async Task<string> Send(HttpRequestMessage req, CancellationToken ct)
    {
        using var res = await http.SendAsync(req, ct);
        return await Read(res, ct);
    }

    static async Task<string> Read(HttpResponseMessage res, CancellationToken ct)
    {
        var text = await res.Content.ReadAsStringAsync(ct);
        if (res.IsSuccessStatusCode) return text;
        var message = $"Google Places rejected the request ({(int)res.StatusCode}).";
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("error", out var e) && e.TryGetProperty("message", out var m)) message += $" {m.GetString()}";
        }
        catch (JsonException)
        {
            // not JSON, keep the generic message
        }
        throw new PlacesException(message);
    }

    public static List<PlaceSuggestion> ParseSuggestions(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<PlaceSuggestion>();
        if (!doc.RootElement.TryGetProperty("suggestions", out var items)) return list;
        foreach (var s in items.EnumerateArray())
        {
            if (!s.TryGetProperty("placePrediction", out var p)) continue;
            var text = p.TryGetProperty("text", out var t) && t.TryGetProperty("text", out var tt) ? tt.GetString() : null;
            if (p.TryGetProperty("placeId", out var id) && id.GetString() is { } placeId && text is not null) list.Add(new PlaceSuggestion(placeId, text));
        }
        return list;
    }

    public static Location ParseDetails(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        var components = r.TryGetProperty("addressComponents", out var ac) ? ac.EnumerateArray().ToList() : [];

        string? Part(string type, bool shortText = false)
        {
            foreach (var c in components)
                if (c.GetProperty("types").EnumerateArray().Any(t => t.GetString() == type))
                    return c.GetProperty(shortText ? "shortText" : "longText").GetString();
            return null;
        }

        double? Coord(string name) => r.TryGetProperty("location", out var l) && l.TryGetProperty(name, out var v) ? v.GetDouble() : null;

        return new Location
        {
            Venue = r.TryGetProperty("displayName", out var n) && n.TryGetProperty("text", out var nt) ? nt.GetString() : null,
            Address = r.TryGetProperty("formattedAddress", out var a) ? a.GetString() : null,
            City = Part("locality") ?? Part("postal_town") ?? Part("administrative_area_level_2"),
            Region = Part("administrative_area_level_1"),
            Country = Part("country", shortText: true)?.ToUpperInvariant(),
            Latitude = Coord("latitude"),
            Longitude = Coord("longitude"),
        };
    }
}
