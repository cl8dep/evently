using System.Net;
using System.Net.Http.Json;

namespace Evently.Tests;

public class CommentTests
{
    [Fact]
    public async Task Users_post_comments_in_order_and_only_author_or_admin_deletes()
    {
        using var f = new AppFactory();
        var admin = f.CreateClient();
        var creds = new { email = "admin@example.com", password = "Passw0rd!" };
        await admin.PostAsJsonAsync("/api/auth/register", creds);
        await admin.PostAsJsonAsync("/api/auth/login?useCookies=true", creds);
        var cat = await (await admin.PostAsJsonAsync("/api/categories", new { name = "Conf" })).Content.ReadFromJsonAsync<Evently.Api.Category>();
        var ev = await (await admin.PostAsJsonAsync("/api/events", new { title = "Summit", categoryId = cat!.Id, startDate = "2027-01-01" })).Content.ReadFromJsonAsync<Evently.Api.Event>();
        await admin.PostAsJsonAsync("/api/users", new { email = "vw@example.com", password = "Passw0rd!", role = "Viewer", firstName = "Vi", lastName = "Ewer" });
        await admin.PostAsJsonAsync("/api/users", new { email = "other@example.com", password = "Passw0rd!", role = "Viewer", firstName = "Ot", lastName = "Her" });

        var viewer = f.CreateClient();
        await viewer.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email = "vw@example.com", password = "Passw0rd!" });
        var other = f.CreateClient();
        await other.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email = "other@example.com", password = "Passw0rd!" });

        var url = $"/api/events/{ev!.Id}/comments";
        var first = await viewer.PostAsJsonAsync(url, new { body = "  **Going** with the team  " });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var posted = (await first.Content.ReadFromJsonAsync<Evently.Api.CommentOut>())!;
        Assert.Equal(("Vi Ewer", "**Going** with the team"), (posted.AuthorName, posted.Body));
        await admin.PostAsJsonAsync(url, new { body = "Booked." });

        var thread = (await admin.GetFromJsonAsync<Evently.Api.CommentPage>(url))!.Items;
        Assert.Equal(["**Going** with the team", "Booked."], thread.Select(c => c.Body));

        Assert.Equal(HttpStatusCode.BadRequest, (await viewer.PostAsJsonAsync(url, new { body = "   " })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await viewer.PostAsJsonAsync(url, new { body = new string('x', 2001) })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await viewer.PostAsJsonAsync("/api/events/9999/comments", new { body = "hi" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.CreateClient().GetAsync(url)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/comments/{posted.Id}", new { body = "hijack" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync($"/api/comments/{posted.Id}", new { body = "hijack" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await viewer.PutAsJsonAsync($"/api/comments/{posted.Id}", new { body = " " })).StatusCode);
        var edited = (await (await viewer.PutAsJsonAsync($"/api/comments/{posted.Id}", new { body = "Going, edited" })).Content.ReadFromJsonAsync<Evently.Api.CommentOut>())!;
        Assert.Equal("Going, edited", edited.Body);
        Assert.NotNull(edited.EditedAt);
        Assert.Null(thread[1].EditedAt);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/comments/{posted.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await viewer.DeleteAsync($"/api/comments/{posted.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/comments/{thread[1].Id}")).StatusCode);
        Assert.Empty((await admin.GetFromJsonAsync<Evently.Api.CommentPage>(url))!.Items);

        await admin.PostAsJsonAsync(url, new { body = "gone with the event" });
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/events/{ev.Id}")).StatusCode);
    }

    [Fact]
    public async Task Comments_load_a_page_at_a_time_from_the_newest()
    {
        using var f = new AppFactory();
        var c = f.CreateClient();
        var creds = new { email = "admin@example.com", password = "Passw0rd!" };
        await c.PostAsJsonAsync("/api/auth/register", creds);
        await c.PostAsJsonAsync("/api/auth/login?useCookies=true", creds);
        var cat = await (await c.PostAsJsonAsync("/api/categories", new { name = "Conf" })).Content.ReadFromJsonAsync<Evently.Api.Category>();
        var ev = await (await c.PostAsJsonAsync("/api/events", new { title = "Summit", categoryId = cat!.Id, startDate = "2027-01-01" })).Content.ReadFromJsonAsync<Evently.Api.Event>();
        var url = $"/api/events/{ev!.Id}/comments";
        for (var i = 1; i <= 5; i++) await c.PostAsJsonAsync(url, new { body = $"c{i}" });

        var newest = (await c.GetFromJsonAsync<Evently.Api.CommentPage>($"{url}?limit=2"))!;
        Assert.Equal(["c4", "c5"], newest.Items.Select(x => x.Body));
        Assert.True(newest.HasMore);
        var older = (await c.GetFromJsonAsync<Evently.Api.CommentPage>($"{url}?limit=2&before={newest.Items[0].Id}"))!;
        Assert.Equal(["c2", "c3"], older.Items.Select(x => x.Body));
        Assert.True(older.HasMore);
        var last = (await c.GetFromJsonAsync<Evently.Api.CommentPage>($"{url}?limit=2&before={older.Items[0].Id}"))!;
        Assert.Equal(["c1"], last.Items.Select(x => x.Body));
        Assert.False(last.HasMore);
    }
}
