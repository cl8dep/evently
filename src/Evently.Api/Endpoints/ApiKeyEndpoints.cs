using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class ApiKeyEndpoints
{
    const int MaxKeys = 10;

    public static void MapApiKeyEndpoints(this WebApplication app)
    {
        // Keys only open /mcp. These routes use the session cookie, so a leaked key cannot mint more keys.
        var keys = app.MapGroup("/api/me/keys").RequireAuthorization();

        keys.MapGet("/", async (ClaimsPrincipal user, AppDb db) =>
            await db.ApiKeys.AsNoTracking().Where(k => k.UserId == user.FindFirstValue(ClaimTypes.NameIdentifier)).OrderBy(k => k.Id)
                .Select(k => new ApiKeyOut(k.Id, k.Name, k.Prefix, k.CreatedAt, k.LastUsedAt)).ToListAsync()).WithName("ListApiKeys").Produces<List<ApiKeyOut>>();

        keys.MapPost("/", async (ApiKeyIn body, AppDb db, ClaimsPrincipal user) =>
        {
            var name = body.Name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 100) return Failure.Invalid("name", "Name is required (max 100).");
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (await db.ApiKeys.CountAsync(k => k.UserId == userId) >= MaxKeys) return Failure.Invalid("name", $"At most {MaxKeys} keys per user.");
            var secret = ApiKeys.NewSecret();
            var key = new ApiKey { UserId = userId, Name = name, Prefix = secret[..(ApiKeys.Prefix.Length + 4)], Hash = ApiKeys.Hash(secret), CreatedAt = DateTime.UtcNow };
            db.ApiKeys.Add(key);
            await db.SaveChangesAsync();
            return Results.Created($"/api/me/keys/{key.Id}", new ApiKeyCreated(new ApiKeyOut(key.Id, key.Name, key.Prefix, key.CreatedAt, null), secret));
        }).WithName("CreateApiKey").Produces<ApiKeyCreated>(201);

        keys.MapDelete("/{id:int}", async (int id, AppDb db, ClaimsPrincipal user) =>
        {
            var n = await db.ApiKeys.Where(k => k.Id == id && k.UserId == user.FindFirstValue(ClaimTypes.NameIdentifier)).ExecuteDeleteAsync();
            return n == 0 ? Results.NotFound() : Results.NoContent();
        }).WithName("DeleteApiKey");
    }
}
