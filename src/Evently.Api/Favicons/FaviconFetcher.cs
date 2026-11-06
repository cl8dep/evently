using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Evently.Api;

public sealed partial class FaviconFetcher
{
    const int MaxHtml = 1 << 20;
    const int MaxIcon = 512 * 1024;
    const int Vector = 100_000;

    readonly HttpClient http;
    readonly ILogger<FaviconFetcher> log;

    public FaviconFetcher(ILogger<FaviconFetcher> log)
    {
        this.log = log;
        // The URL is user-supplied, so every connection (including redirects) is checked against public addresses only.
        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            MaxAutomaticRedirections = 3,
            ConnectTimeout = TimeSpan.FromSeconds(4),
            ConnectCallback = async (ctx, ct) =>
            {
                var addresses = (await Dns.GetHostAddressesAsync(ctx.DnsEndPoint.Host, ct)).Where(IsPublic).ToArray();
                if (addresses.Length == 0) throw new HttpRequestException("Blocked address.");
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(addresses, ctx.DnsEndPoint.Port, ct);
                    return new NetworkStream(socket, true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        };
        http = new HttpClient(handler, true) { Timeout = TimeSpan.FromSeconds(6) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Evently/1.0 (+favicon fetch)");
    }

    public async Task<Site> Fetch(string website, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(website, UriKind.Absolute, out var page) || page.Scheme is not ("http" or "https")) return new Site(null, null);
        Icon? icon = null;
        string? description = null;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(8));
            var candidates = new List<Uri>();
            var final = page;
            var html = await Download(page, MaxHtml, "text/html", cts.Token);
            if (html is not null)
            {
                final = html.Value.Final;
                var text = System.Text.Encoding.UTF8.GetString(html.Value.Data);
                description = Description(text);
                candidates.AddRange(Candidates(text, final));
            }
            candidates.Add(new Uri(final, "/favicon.ico"));
            foreach (var url in candidates.Distinct())
            {
                var got = await Download(url, MaxIcon, "image/", cts.Token);
                if (got is { } g) { icon = new Icon(g.Data, g.ContentType); break; }
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or OperationCanceledException or UriFormatException)
        {
            log.LogWarning("Favicon fetch for {Website} failed: {Reason}", website, e.Message);
        }
        if (icon is null) log.LogWarning("No icon found for {Website}", website);
        return new Site(icon, description);
    }

    async Task<(byte[] Data, string ContentType, Uri Final)?> Download(Uri url, int max, string typePrefix, CancellationToken ct)
    {
        try
        {
            using var res = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!res.IsSuccessStatusCode)
            {
                log.LogWarning("{Url} returned {Status}", url, (int)res.StatusCode);
                return null;
            }
            var type = res.Content.Headers.ContentType?.MediaType ?? "";
            if (typePrefix == "image/" && type is "" or "application/octet-stream" && url.AbsolutePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                type = "image/x-icon";
            if (!type.StartsWith(typePrefix, StringComparison.OrdinalIgnoreCase))
            {
                log.LogWarning("{Url} has content type '{Type}', expected {Prefix}*", url, type, typePrefix);
                return null;
            }
            if (res.Content.Headers.ContentLength > max)
            {
                log.LogWarning("{Url} is larger than {Max} bytes", url, max);
                return null;
            }
            await using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var buffer = new MemoryStream();
            var chunk = new byte[16 * 1024];
            int n;
            while ((n = await stream.ReadAsync(chunk, ct)) > 0)
            {
                buffer.Write(chunk, 0, n);
                if (buffer.Length > max) return null;
            }
            return buffer.Length == 0 ? null : (buffer.ToArray(), type, res.RequestMessage?.RequestUri ?? url);
        }
        catch (HttpRequestException e)
        {
            log.LogWarning("{Url} failed: {Reason}", url, e.Message);
            return null;
        }
    }

    // Declared icons, best quality first: vector, then the largest declared size, then icons with no size.
    // ponytail: regex over <link> tags is enough for favicons; swap for an HTML parser if a site defeats it.
    public static List<Uri> Candidates(string html, Uri page)
    {
        var found = new List<(Uri Url, int Score)>();
        foreach (Match tag in LinkTag().Matches(html))
        {
            var attrs = Attrs(tag.Value);
            if (!attrs.TryGetValue("href", out var href) || !attrs.TryGetValue("rel", out var rel)) continue;
            var rels = rel.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var apple = rels.Contains("apple-touch-icon") || rels.Contains("apple-touch-icon-precomposed");
            if (!apple && !rels.Contains("icon")) continue;
            if (!Uri.TryCreate(page, href.Trim(), out var url) || url.Scheme is not ("http" or "https")) continue;

            attrs.TryGetValue("sizes", out var sizes);
            attrs.TryGetValue("type", out var type);
            var score = type == "image/svg+xml" || url.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ? Vector
                : string.Equals(sizes, "any", StringComparison.OrdinalIgnoreCase) ? Vector - 1
                : Size().Matches(sizes ?? "").Select(m => Math.Max(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value))).DefaultIfEmpty(apple ? 180 : 16).Max();
            found.Add((url, score));
        }
        return found.OrderByDescending(f => f.Score).Select(f => f.Url).Distinct().ToList();
    }

    static Dictionary<string, string> Attrs(string tag) => Attr().Matches(tag).ToDictionary(
        m => m.Groups[1].Value.ToLowerInvariant(),
        m => m.Groups[3].Success ? m.Groups[3].Value : m.Groups[4].Success ? m.Groups[4].Value : m.Groups[5].Value,
        StringComparer.Ordinal);

    // Page summary from <meta name="description">, falling back to Open Graph and Twitter tags.
    public static string? Description(string html)
    {
        var metas = new Dictionary<string, string>();
        foreach (Match tag in MetaTag().Matches(html))
        {
            var a = Attrs(tag.Value);
            var key = (a.GetValueOrDefault("name") ?? a.GetValueOrDefault("property"))?.ToLowerInvariant();
            if (key is "description" or "og:description" or "twitter:description" && a.TryGetValue("content", out var c))
                metas.TryAdd(key, c);
        }
        foreach (var key in new[] { "description", "og:description", "twitter:description" })
        {
            if (!metas.TryGetValue(key, out var raw)) continue;
            var text = Whitespace().Replace(WebUtility.HtmlDecode(raw), " ").Trim();
            if (text.Length > 0) return text.Length > 600 ? text[..600] : text;
        }
        return null;
    }

    public static bool IsPublic(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any)) return false;
        var b = ip.GetAddressBytes();
        if (ip.AddressFamily == AddressFamily.InterNetwork)
            return !(b[0] is 0 or 10 or 127 or >= 224
                || (b[0] == 100 && b[1] is >= 64 and <= 127)
                || (b[0] == 169 && b[1] == 254)
                || (b[0] == 172 && b[1] is >= 16 and <= 31)
                || (b[0] == 192 && b[1] == 168));
        return !(ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast || (b[0] & 0xFE) == 0xFC);
    }

    [GeneratedRegex(@"<link\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex LinkTag();

    [GeneratedRegex(@"<meta\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex MetaTag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"([\w-]+)\s*=\s*(?:(""([^""]*)"")|'([^']*)'|([^\s>""']+))")]
    private static partial Regex Attr();

    [GeneratedRegex(@"(\d+)x(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex Size();
}
