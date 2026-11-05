using System.Net;
using System.Net.Http.Json;
using Evently.Api;

namespace Evently.Tests;

public class FaviconTests
{
    static readonly Uri Page = new("https://example.com/events/summit");

    [Fact]
    public void Picks_vector_then_largest_declared_size()
    {
        const string html = """
            <link rel="icon" href="/16.png" sizes="16x16">
            <link rel="shortcut icon" href='/fav.ico'>
            <link rel="apple-touch-icon" href="/apple.png">
            <link rel="icon" sizes="192x192" href="https://cdn.example.com/192.png">
            <link rel="mask-icon" href="/mask.svg">
            <link rel="stylesheet" href="/site.css">
            """;
        Assert.Equal(
            ["https://cdn.example.com/192.png", "https://example.com/apple.png", "https://example.com/16.png", "https://example.com/fav.ico"],
            FaviconFetcher.Candidates(html, Page).Select(u => u.ToString()));

        var withSvg = FaviconFetcher.Candidates("""<link rel="icon" href="a.png" sizes="512x512"><link rel="icon" type="image/svg+xml" href="i.svg">""", Page);
        Assert.Equal("https://example.com/events/i.svg", withSvg[0].ToString());
    }

    [Theory]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("172.20.0.5", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("::1", false)]
    [InlineData("fd00::1", false)]
    [InlineData("::ffff:10.0.0.1", false)]
    [InlineData("93.184.216.34", true)]
    [InlineData("2606:4700::1111", true)]
    public void Only_public_addresses_are_allowed(string ip, bool expected) =>
        Assert.Equal(expected, FaviconFetcher.IsPublic(IPAddress.Parse(ip)));

    [Fact]
    public async Task Private_website_is_saved_without_an_icon()
    {
        using var factory = new AppFactory();
        var c = factory.CreateClient();
        var creds = new { email = "icon@example.com", password = "Passw0rd!" };
        await c.PostAsJsonAsync("/api/auth/register", creds);
        await c.PostAsJsonAsync("/api/auth/login?useCookies=true", creds);
        var cat = await (await c.PostAsJsonAsync("/api/categories", new { name = "Icons" })).Content.ReadFromJsonAsync<Category>();
        var res = await c.PostAsJsonAsync("/api/events", new { title = "Local", categoryId = cat!.Id, startDate = "2026-12-01", website = "http://127.0.0.1:9/" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var ev = await res.Content.ReadFromJsonAsync<Event>();
        Assert.Null(ev!.IconUpdatedAt);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/events/{ev.Id}/icon")).StatusCode);
    }

    [Theory]
    [InlineData("<meta name=\"description\" content=\"Plain  text\">", "Plain text")]
    [InlineData("<meta property='og:description' content='Caf&eacute; &amp; more'>", "Café & more")]
    [InlineData("<meta property=\"og:description\" content=\"og\"><meta name=\"description\" content=\"main\">", "main")]
    [InlineData("<meta name=\"description\" content=\"  \">", null)]
    [InlineData("<p>nothing</p>", null)]
    public void Reads_page_description(string html, string? expected) =>
        Assert.Equal(expected, FaviconFetcher.Description(html));
}
