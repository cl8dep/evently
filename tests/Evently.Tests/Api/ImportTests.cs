using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Evently.Tests;

public class ImportTests
{
    static async Task<HttpClient> Admin(AppFactory f)
    {
        var c = f.CreateClient();
        var creds = new { email = "admin@example.com", password = "Passw0rd!" };
        await c.PostAsJsonAsync("/api/auth/register", creds);
        await c.PostAsJsonAsync("/api/auth/login?useCookies=true", creds);
        return c;
    }

    static Task<HttpResponseMessage> Import(HttpClient c, string csv) =>
        c.PostAsync("/api/events/import", new StringContent(csv, Encoding.UTF8, "text/csv"));

    [Fact]
    public async Task Exported_csv_imports_into_another_instance_and_reimport_skips_duplicates()
    {
        string csv;
        using (var source = new AppFactory())
        {
            var a = await Admin(source);
            var cat = await (await a.PostAsJsonAsync("/api/categories", new { name = "Conference" })).Content.ReadFromJsonAsync<Evently.Api.Category>();
            var t1 = await (await a.PostAsJsonAsync("/api/topics", new { name = "AI" })).Content.ReadFromJsonAsync<Evently.Api.Topic>();
            var t2 = await (await a.PostAsJsonAsync("/api/topics", new { name = "Cloud edge" })).Content.ReadFromJsonAsync<Evently.Api.Topic>();
            await a.PostAsJsonAsync("/api/events", new
            {
                title = "Summit, \"2027\"", description = "Line one\nLine two", categoryId = cat!.Id, website = "https://example.com/x",
                ticketCost = 450.5, currency = "USD", startDate = "2027-03-10", endDate = "2027-03-12",
                location = new { venue = "Hall", city = "Austin", region = "Texas", country = "US", latitude = 30.25, longitude = -97.75 },
                topicIds = new[] { t1!.Id, t2!.Id },
            });
            await a.PostAsJsonAsync("/api/events", new { title = "=SUM(A1)", categoryId = cat.Id, startDate = "2027-04-01" });
            csv = await a.GetStringAsync("/api/events/export.csv");
        }

        using var f = new AppFactory();
        var admin = await Admin(f);
        var res = await Import(admin, csv);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(2, body.GetProperty("imported").GetInt32());

        var events = (await admin.GetFromJsonAsync<List<Evently.Api.Event>>("/api/events"))!;
        var e = events.Single(x => x.Title == "Summit, \"2027\"");
        Assert.Equal("Line one\nLine two", e.Description);
        Assert.Equal("Conference", e.Category!.Name);
        Assert.Equal(["AI", "Cloud edge"], e.Topics.Select(t => t.Name).Order());
        Assert.Equal(450.5m, e.TicketCost);
        Assert.Equal(("Austin", "US", 30.25), (e.Location.City, e.Location.Country, e.Location.Latitude));
        Assert.Contains(events, x => x.Title == "=SUM(A1)");

        var again = await (await Import(admin, csv)).Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal((0, 2), (again.GetProperty("imported").GetInt32(), again.GetProperty("skipped").GetInt32()));
        Assert.Equal(2, (await admin.GetFromJsonAsync<List<Evently.Api.Event>>("/api/events"))!.Count);
    }

    [Fact]
    public async Task A_bad_row_rejects_the_whole_file_and_viewers_cannot_import()
    {
        using var f = new AppFactory();
        var admin = await Admin(f);
        const string header = "Title,Category,Start date,Country\r\n";
        var res = await Import(admin, header + "Good,Talks,2027-01-01,US\r\nBad,Talks,01/02/2027,ZZ\r\n");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
        Assert.Contains("Row 3", await res.Content.ReadAsStringAsync());
        Assert.Empty((await admin.GetFromJsonAsync<List<Evently.Api.Event>>("/api/events"))!);
        Assert.Empty((await admin.GetFromJsonAsync<List<Evently.Api.Category>>("/api/categories"))!);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Import(admin, "Title,Category\r\nX,Y\r\n")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Import(admin, header + "Wrong,Talks,2027-01-01,ZZ\r\n")).StatusCode);

        await admin.PostAsJsonAsync("/api/users", new { email = "v@example.com", password = "Passw0rd!", role = "Viewer", firstName = "V", lastName = "W" });
        var viewer = f.CreateClient();
        await viewer.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email = "v@example.com", password = "Passw0rd!" });
        Assert.Equal(HttpStatusCode.Forbidden, (await Import(viewer, header + "A,Talks,2027-01-01,US\r\n")).StatusCode);
    }
}
