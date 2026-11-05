using System.Net;
using System.Net.Http.Json;
using Evently.Api;

namespace Evently.Tests;

public class ApiTests : IClassFixture<AppFactory>
{
    readonly AppFactory factory;
    public ApiTests(AppFactory factory) => this.factory = factory;

    static object Ev(string title, int categoryId, string country) => new
    {
        title, description = "a, \"b\"\nc", categoryId, startDate = "2026-12-01",
        location = new { city = "Madrid", country },
    };

    [Fact]
    public async Task Requires_login()
    {
        var c = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/events/export.csv")).StatusCode);
    }

    [Fact]
    public async Task Crud_conflict_and_csv()
    {
        var c = factory.CreateClient();
        var creds = new { email = "t@example.com", password = "Passw0rd!" };
        Assert.True((await c.PostAsJsonAsync("/api/auth/register", creds)).IsSuccessStatusCode);
        Assert.True((await c.PostAsJsonAsync("/api/auth/login?useCookies=true", creds)).IsSuccessStatusCode);

        var cat = await (await c.PostAsJsonAsync("/api/categories", new { name = "Conf" })).Content.ReadFromJsonAsync<Category>();
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/categories", new { name = "Conf" })).StatusCode);

        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/events", Ev("=Evil", cat!.Id, "es"))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/events", Ev("Other", cat.Id, "fr"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/events", Ev("", cat.Id, "es"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/events", Ev("Bad", cat.Id, "ZZ"))).StatusCode);

        var countries = await c.GetFromJsonAsync<List<Country>>("/api/countries");
        Assert.Equal(249, countries!.Count);
        Assert.Contains(countries, x => x.Code == "UY");
        Assert.Contains(await c.GetFromJsonAsync<List<Currency>>("/api/currencies") ?? [], x => x.Code == "EUR");

        var csv = await c.GetStringAsync("/api/events/export.csv?country=ES");
        Assert.StartsWith("﻿Title,Description,Category,", csv);
        Assert.Contains("'=Evil", csv);
        Assert.Contains("\"a, \"\"b\"\"\nc\"", csv);
        Assert.DoesNotContain("Other", csv);

        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync($"/api/categories/{cat.Id}")).StatusCode);

        var ai = await (await c.PostAsJsonAsync("/api/topics", new { name = "AI" })).Content.ReadFromJsonAsync<Topic>();
        var fin = await (await c.PostAsJsonAsync("/api/topics", new { name = "Fintech" })).Content.ReadFromJsonAsync<Topic>();
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/topics", new { name = "AI" })).StatusCode);
        var tagged = new { title = "Tagged", categoryId = cat.Id, startDate = "2026-12-02", topicIds = new[] { ai!.Id, fin!.Id } };
        var created = await c.PostAsJsonAsync("/api/events", tagged);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(2, (await created.Content.ReadFromJsonAsync<Event>())!.Topics.Count);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/events", new { title = "X", categoryId = cat.Id, startDate = "2026-12-02", topicIds = new[] { 9999 } })).StatusCode);

        var byTopic = await c.GetStringAsync($"/api/events/export.csv?topicId={ai.Id}");
        Assert.Contains("\"AI, Fintech\"", byTopic);
        Assert.DoesNotContain("Other", byTopic);
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync($"/api/topics/{ai.Id}")).StatusCode);
    }
}
