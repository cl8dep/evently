using System.Net;
using System.Net.Http.Json;

namespace Evently.Tests;

public class AttendanceTests
{
    [Fact]
    public async Task Users_set_their_own_status_and_everyone_sees_it()
    {
        using var f = new AppFactory();
        var admin = f.CreateClient();
        var creds = new { email = "admin@example.com", password = "Passw0rd!" };
        await admin.PostAsJsonAsync("/api/auth/register", creds);
        await admin.PostAsJsonAsync("/api/auth/login?useCookies=true", creds);
        var cat = await (await admin.PostAsJsonAsync("/api/categories", new { name = "Conf" })).Content.ReadFromJsonAsync<Evently.Api.Category>();
        var ev = await (await admin.PostAsJsonAsync("/api/events", new { title = "Summit", categoryId = cat!.Id, startDate = "2027-01-01" })).Content.ReadFromJsonAsync<Evently.Api.Event>();
        await admin.PostAsJsonAsync("/api/users", new { email = "vw@example.com", password = "Passw0rd!", role = "Viewer", firstName = "Vi", lastName = "Ewer" });
        var viewer = f.CreateClient();
        await viewer.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email = "vw@example.com", password = "Passw0rd!" });

        var url = $"/api/events/{ev!.Id}/attendance";
        Assert.Equal(HttpStatusCode.OK, (await viewer.PutAsJsonAsync(url, new { status = "Going" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.PutAsJsonAsync(url, new { status = "Maybe" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(url, new { status = "Declined" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await viewer.PutAsJsonAsync(url, new { status = "Sure" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await viewer.PutAsJsonAsync("/api/events/9999/attendance", new { status = "Going" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.CreateClient().PutAsJsonAsync(url, new { status = "Going" })).StatusCode);

        var list = (await admin.GetFromJsonAsync<List<Evently.Api.AttendeeOut>>($"/api/events/{ev.Id}/attendees"))!;
        Assert.Equal([("Vi Ewer", Evently.Api.AttendanceStatus.Maybe), ("admin@example.com", Evently.Api.AttendanceStatus.Declined)], list.Select(a => (a.Name, a.Status)));

        Assert.Equal(HttpStatusCode.NoContent, (await viewer.DeleteAsync(url)).StatusCode);
        Assert.Single((await admin.GetFromJsonAsync<List<Evently.Api.AttendeeOut>>($"/api/events/{ev.Id}/attendees"))!);
    }
}
