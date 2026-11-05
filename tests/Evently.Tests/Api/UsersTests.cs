using System.Net;
using System.Net.Http.Json;

namespace Evently.Tests;

public class UsersTests
{
    static async Task<HttpClient> LoginAs(AppFactory f, string email, string password = "Passw0rd!")
    {
        var c = f.CreateClient();
        Assert.True((await c.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email, password })).IsSuccessStatusCode);
        return c;
    }

    [Fact]
    public async Task Roles_control_access_and_admin_manages_users()
    {
        using var f = new AppFactory();
        var admin = f.CreateClient();
        var first = new { email = "admin@example.com", password = "Passw0rd!" };
        Assert.True((await admin.PostAsJsonAsync("/api/auth/register", first)).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.CreateClient().PostAsJsonAsync("/api/auth/register", new { email = "x@example.com", password = "Passw0rd!" })).StatusCode);
        Assert.True((await admin.PostAsJsonAsync("/api/auth/login?useCookies=true", first)).IsSuccessStatusCode);

        var me = await admin.GetFromJsonAsync<Dictionary<string, object>>("/api/me");
        Assert.Equal("Admin", me!["role"].ToString());

        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/users", new { email = "ed@example.com", password = "Passw0rd!", role = "Editor", firstName = "Ed", lastName = "Itor" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/users", new { email = "vw@example.com", password = "Passw0rd!", role = "Viewer", firstName = "Vi", lastName = "Ewer" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/users", new { email = "bad@example.com", password = "x", role = "Viewer", firstName = "A", lastName = "B" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/users", new { email = "bad@example.com", password = "Passw0rd!", role = "Viewer" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/users", new { email = "bad@example.com", password = "Passw0rd!", role = "Root", firstName = "A", lastName = "B" })).StatusCode);

        var users = (await admin.GetFromJsonAsync<List<UserRow>>("/api/users"))!;
        Assert.Equal(3, users.Count);
        var editorRow = users.Single(u => u.Email == "ed@example.com");

        var editor = await LoginAs(f, "ed@example.com");
        var editorMe = await editor.GetFromJsonAsync<Dictionary<string, object>>("/api/me");
        Assert.Equal(("Ed", "Itor"), (editorMe!["firstName"].ToString(), editorMe["lastName"].ToString()));
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/users/{editorRow.Id}", new { firstName = "Edna" })).StatusCode);
        Assert.Equal("Edna", (await admin.GetFromJsonAsync<List<UserRow>>("/api/users"))!.Single(u => u.Id == editorRow.Id).FirstName);
        var viewer = await LoginAs(f, "vw@example.com");
        Assert.Equal(HttpStatusCode.Created, (await editor.PostAsJsonAsync("/api/categories", new { name = "Conf" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync("/api/categories", new { name = "Nope" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await editor.GetAsync("/api/users")).StatusCode);

        // A demotion applies to the user's existing session.
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/users/{editorRow.Id}", new { role = "Viewer" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await editor.PostAsJsonAsync("/api/categories", new { name = "Later" })).StatusCode);

        var adminRow = users.Single(u => u.Email == "admin@example.com");
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/users/{adminRow.Id}", new { role = "Viewer" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/users/{adminRow.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/users/{editorRow.Id}")).StatusCode);
        Assert.Equal(2, (await admin.GetFromJsonAsync<List<UserRow>>("/api/users"))!.Count);
    }

    [Fact]
    public async Task Same_idempotency_key_creates_one_event()
    {
        using var f = new AppFactory();
        var c = f.CreateClient();
        var creds = new { email = "idem@example.com", password = "Passw0rd!" };
        await c.PostAsJsonAsync("/api/auth/register", creds);
        await c.PostAsJsonAsync("/api/auth/login?useCookies=true", creds);
        var cat = await (await c.PostAsJsonAsync("/api/categories", new { name = "Conf" })).Content.ReadFromJsonAsync<Evently.Api.Category>();

        async Task<HttpResponseMessage> Post(string key)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/events")
            {
                Content = JsonContent.Create(new { title = "Once", categoryId = cat!.Id, startDate = "2026-12-01" }),
            };
            req.Headers.Add("Idempotency-Key", key);
            return await c.SendAsync(req);
        }

        var first = await Post("k1");
        var again = await Post("k1");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal((await first.Content.ReadFromJsonAsync<Evently.Api.Event>())!.Id, (await again.Content.ReadFromJsonAsync<Evently.Api.Event>())!.Id);
        await Post("k2");
        Assert.Equal(2, (await c.GetFromJsonAsync<List<Evently.Api.Event>>("/api/events"))!.Count);
    }

    [Fact]
    public async Task Global_search_finds_across_areas_and_hides_users_from_non_admins()
    {
        using var f = new AppFactory();
        var admin = f.CreateClient();
        var first = new { email = "root@example.com", password = "Passw0rd!" };
        await admin.PostAsJsonAsync("/api/auth/register", first);
        await admin.PostAsJsonAsync("/api/auth/login?useCookies=true", first);
        var cat = await (await admin.PostAsJsonAsync("/api/categories", new { name = "Tourism" })).Content.ReadFromJsonAsync<Evently.Api.Category>();
        var topic = await (await admin.PostAsJsonAsync("/api/topics", new { name = "Tourism tech" })).Content.ReadFromJsonAsync<Evently.Api.Topic>();
        await admin.PostAsJsonAsync("/api/events", new { title = "Latin America Fair", categoryId = cat!.Id, startDate = "2027-10-02", topicIds = new[] { topic!.Id } });
        await admin.PostAsJsonAsync("/api/users", new { email = "tour@example.com", password = "Passw0rd!", role = "Viewer", firstName = "Tour", lastName = "Guide" });

        var all = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/search?q=tour");
        Assert.Equal(1, all.GetProperty("events").GetArrayLength());
        Assert.Equal(1, all.GetProperty("categories").GetArrayLength());
        Assert.Equal(1, all.GetProperty("topics").GetArrayLength());
        Assert.Equal(1, all.GetProperty("users").GetArrayLength());
        Assert.Equal(0, (await admin.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/search?q=t")).GetProperty("events").GetArrayLength());

        var viewer = await LoginAs(f, "tour@example.com");
        Assert.Equal(0, (await viewer.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/search?q=tour")).GetProperty("users").GetArrayLength());
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, (await f.CreateClient().GetAsync("/api/search?q=tour")).StatusCode);
    }
}
