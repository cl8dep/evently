using System.Net;
using Evently.Api;
using System.Net.Http.Json;

namespace Evently.Tests;

public class PlacesTests
{
    [Fact]
    public void Parses_autocomplete_and_skips_query_predictions()
    {
        var json = """
            {"suggestions":[
              {"placePrediction":{"placeId":"abc","text":{"text":"Gramado, State of Rio Grande do Sul, Brazil"}}},
              {"queryPrediction":{"text":{"text":"gramado hotels"}}}]}
            """;
        var list = PlacesClient.ParseSuggestions(json);
        Assert.Single(list);
        Assert.Equal(new PlaceSuggestion("abc", "Gramado, State of Rio Grande do Sul, Brazil"), list[0]);
        Assert.Empty(PlacesClient.ParseSuggestions("{}"));
    }

    [Fact]
    public void Parses_place_details_into_a_location()
    {
        var json = """
            {"displayName":{"text":"Expo Center"},"formattedAddress":"Av. 1, Gramado, Brazil",
             "addressComponents":[
               {"longText":"Gramado","shortText":"Gramado","types":["locality","political"]},
               {"longText":"Rio Grande do Sul","shortText":"RS","types":["administrative_area_level_1","political"]},
               {"longText":"Brazil","shortText":"br","types":["country","political"]}],
             "location":{"latitude":-29.37,"longitude":-50.87}}
            """;
        var l = PlacesClient.ParseDetails(json);
        Assert.Equal(("Expo Center", "Gramado", "Rio Grande do Sul", "BR"), (l.Venue, l.City, l.Region, l.Country));
        Assert.Equal((-29.37, -50.87), (l.Latitude, l.Longitude));
    }

    [Fact]
    public async Task Without_a_key_places_endpoints_are_unavailable_and_login_is_required()
    {
        using var f = new AppFactory();
        var c = f.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/places/autocomplete?q=gramado")).StatusCode);

        var creds = new { email = "p@example.com", password = "Passw0rd!" };
        await c.PostAsJsonAsync("/api/auth/register", creds);
        await c.PostAsJsonAsync("/api/auth/login?useCookies=true", creds);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await c.GetAsync("/api/places/autocomplete?q=gramado")).StatusCode);
        Assert.False((await c.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/config")).GetProperty("places").GetBoolean());
    }
}
