namespace Evently.Api;

public static class PlaceEndpoints
{
    static readonly IResult NotConfigured = Results.Json(new { error = "Google Maps is not configured." }, statusCode: 503);

    public static void MapPlaceEndpoints(this WebApplication app)
    {
        var places = app.MapGroup("/api/places").RequireAuthorization("Editor");

        places.MapGet("/autocomplete", async (string? q, string? session, PlacesClient google, CancellationToken ct) =>
        {
            if (!google.Enabled) return NotConfigured;
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3) return Results.Ok(Array.Empty<PlaceSuggestion>());
            return await Call(() => google.Suggest(q.Trim(), session, ct));
        }).WithName("AutocompletePlaces").Produces<PlaceSuggestion[]>();

        places.MapGet("/{placeId}", async (string placeId, string? session, PlacesClient google, CancellationToken ct) =>
            google.Enabled ? await Call(() => google.Details(placeId, session, ct)) : NotConfigured)
            .WithName("GetPlace").Produces<Location>();

    }

    static async Task<IResult> Call<T>(Func<Task<T>> call)
    {
        try
        {
            return await call() is { } value ? Results.Ok(value) : Results.NotFound();
        }
        catch (PlacesException e)
        {
            return Results.Json(new { error = e.Message }, statusCode: 502);
        }
    }
}
