using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Evently.Tests;

public class ApiKeyTests
{
    static async Task<JsonElement> Rpc(HttpClient c, object body)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
        req.Headers.Accept.ParseAdd("application/json");
        req.Headers.Accept.ParseAdd("text/event-stream");
        var res = await c.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var text = await res.Content.ReadAsStringAsync();
        // The reply is either plain JSON or a single server-sent event.
        var json = text.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.StartsWith("data:"))?[5..] ?? text;
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    [Fact]
    public async Task A_key_opens_mcp_as_its_owner_and_nothing_else()
    {
        using var f = new AppFactory();
        var admin = f.CreateClient();
        var creds = new { email = "admin@example.com", password = "Passw0rd!" };
        await admin.PostAsJsonAsync("/api/auth/register", creds);
        await admin.PostAsJsonAsync("/api/auth/login?useCookies=true", creds);
        var cat = await (await admin.PostAsJsonAsync("/api/categories", new { name = "Conf" })).Content.ReadFromJsonAsync<Evently.Api.Category>();
        var ev = await (await admin.PostAsJsonAsync("/api/events", new { title = "Summit", categoryId = cat!.Id, startDate = "2027-01-01" })).Content.ReadFromJsonAsync<Evently.Api.Event>();

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/me/keys", new { name = " " })).StatusCode);
        var created = (await (await admin.PostAsJsonAsync("/api/me/keys", new { name = "Claude" })).Content.ReadFromJsonAsync<Evently.Api.ApiKeyCreated>())!;
        Assert.StartsWith("evt_", created.Secret);
        var listed = (await admin.GetFromJsonAsync<List<Evently.Api.ApiKeyOut>>("/api/me/keys"))!;
        Assert.Equal(["Claude"], listed.Select(k => k.Name));
        Assert.DoesNotContain(created.Secret, await admin.GetStringAsync("/api/me/keys"));

        var anon = f.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsync("/mcp", new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode);
        var bad = f.CreateClient();
        bad.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "evt_wrong");
        Assert.Equal(HttpStatusCode.Unauthorized, (await bad.PostAsync("/mcp", new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode);

        var bot = f.CreateClient();
        bot.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", created.Secret);
        Assert.Equal(HttpStatusCode.Unauthorized, (await bot.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await bot.PostAsJsonAsync("/api/me/keys", new { name = "more" })).StatusCode);

        await Rpc(bot, new { jsonrpc = "2.0", id = 1, method = "initialize", @params = new { protocolVersion = "2025-03-26", capabilities = new { }, clientInfo = new { name = "test", version = "1" } } });
        var tools = await Rpc(bot, new { jsonrpc = "2.0", id = 2, method = "tools/list" });
        Assert.Contains("set_attendance", tools.GetRawText());

        var found = await Rpc(bot, new { jsonrpc = "2.0", id = 3, method = "tools/call", @params = new { name = "list_events", arguments = new { query = "Summ" } } });
        Assert.Contains("Summit", found.GetRawText());
        var set = await Rpc(bot, new { jsonrpc = "2.0", id = 4, method = "tools/call", @params = new { name = "set_attendance", arguments = new { eventId = ev!.Id, status = "Going" } } });
        Assert.DoesNotContain("\"isError\":true", set.GetRawText());
        await Rpc(bot, new { jsonrpc = "2.0", id = 5, method = "tools/call", @params = new { name = "add_comment", arguments = new { eventId = ev.Id, body = "From the bot" } } });

        Assert.Equal(Evently.Api.AttendanceStatus.Going, (await admin.GetFromJsonAsync<List<Evently.Api.AttendeeOut>>($"/api/events/{ev.Id}/attendees"))!.Single().Status);
        Assert.Equal("admin@example.com", (await admin.GetFromJsonAsync<Evently.Api.CommentPage>($"/api/events/{ev.Id}/comments"))!.Items.Single().Author);
        Assert.NotNull((await admin.GetFromJsonAsync<List<Evently.Api.ApiKeyOut>>("/api/me/keys"))!.Single().LastUsedAt);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/me/keys/{created.Key.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await bot.PostAsync("/mcp", new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode);
    }
}
