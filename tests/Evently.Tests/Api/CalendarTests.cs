using System.Net;
using System.Net.Http.Json;

namespace Evently.Tests;

public class CalendarTests
{
    [Fact]
    public async Task Feed_needs_the_secret_token_and_lists_all_day_events()
    {
        using var f = new AppFactory();
        var c = f.CreateClient();
        var creds = new { email = "admin@example.com", password = "Passw0rd!" };
        await c.PostAsJsonAsync("/api/auth/register", creds);
        await c.PostAsJsonAsync("/api/auth/login?useCookies=true", creds);
        var cat = await (await c.PostAsJsonAsync("/api/categories", new { name = "Conf" })).Content.ReadFromJsonAsync<Evently.Api.Category>();
        var going = await (await c.PostAsJsonAsync("/api/events", new { title = "Summit, Brazil; 2027", description = "Line one\nLine two", categoryId = cat!.Id, startDate = "2027-01-01", endDate = "2027-01-03" })).Content.ReadFromJsonAsync<Evently.Api.Event>();
        await c.PostAsJsonAsync("/api/events", new { title = "Other", categoryId = cat.Id, startDate = "2027-02-01" });
        await c.PutAsJsonAsync($"/api/events/{going!.Id}/attendance", new { status = "Going" });

        var path = (await c.GetFromJsonAsync<Evently.Api.CalendarOut>("/api/me/calendar"))!.Path;
        Assert.Equal(path, (await c.GetFromJsonAsync<Evently.Api.CalendarOut>("/api/me/calendar"))!.Path);

        var anon = f.CreateClient();
        var all = await anon.GetStringAsync(path);
        Assert.Contains("SUMMARY:Summit\\, Brazil\\; 2027", all);
        Assert.Contains("DTSTART;VALUE=DATE:20270101", all);
        Assert.Contains("DTEND;VALUE=DATE:20270104", all);
        Assert.Contains("DTEND;VALUE=DATE:20270202", all);
        Assert.Contains("DESCRIPTION:Line one\\nLine two", all);
        Assert.Equal(2, all.Split("BEGIN:VEVENT").Length - 1);

        var mine = await anon.GetStringAsync(path + "?mine=true");
        Assert.Equal(1, mine.Split("BEGIN:VEVENT").Length - 1);

        Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync("/api/calendar/nope/events.ics")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/events/{going.Id}/ics")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync($"/api/events/{going.Id}/ics")).StatusCode);

        var rotated = (await (await c.PostAsync("/api/me/calendar", null)).Content.ReadFromJsonAsync<Evently.Api.CalendarOut>())!.Path;
        Assert.NotEqual(path, rotated);
        Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync(path)).StatusCode);
    }
}
