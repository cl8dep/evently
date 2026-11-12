using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

// Keys only open /mcp. These routes use the session cookie, so a leaked key cannot mint more keys.
[ApiController]
[Authorize]
[Route("api/me/keys")]
[Produces("application/json")]
public class ApiKeysController(AppDb db) : ControllerBase
{
    const int MaxKeys = 10;

    [HttpGet]
    [EndpointName("ListApiKeys")]
    public async Task<ActionResult<List<ApiKeyOut>>> List()
    {
        var userId = this.UserId;
        return await db.ApiKeys.AsNoTracking().Where(k => k.UserId == userId).OrderBy(k => k.Id)
            .Select(k => new ApiKeyOut(k.Id, k.Name, k.Prefix, k.CreatedAt, k.LastUsedAt)).ToListAsync();
    }

    [HttpPost]
    [EndpointName("CreateApiKey")]
    [ProducesResponseType<ApiKeyCreated>(StatusCodes.Status201Created)]
    [Consumes("application/json")]
    public async Task<ActionResult> Create(ApiKeyIn body)
    {
        var name = body.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 100) return this.Invalid("name", "Name is required (max 100).");
        var userId = this.UserId;
        if (await db.ApiKeys.CountAsync(k => k.UserId == userId) >= MaxKeys) return this.Invalid("name", $"At most {MaxKeys} keys per user.");
        var secret = ApiKeys.NewSecret();
        var key = new ApiKey { UserId = userId, Name = name, Prefix = secret[..(ApiKeys.Prefix.Length + 4)], Hash = ApiKeys.Hash(secret), CreatedAt = DateTime.UtcNow };
        db.ApiKeys.Add(key);
        await db.SaveChangesAsync();
        return Created($"/api/me/keys/{key.Id}", new ApiKeyCreated(new ApiKeyOut(key.Id, key.Name, key.Prefix, key.CreatedAt, null), secret));
    }

    [HttpDelete("{id:int}")]
    [EndpointName("DeleteApiKey")]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = this.UserId;
        var deleted = await db.ApiKeys.Where(k => k.Id == id && k.UserId == userId).ExecuteDeleteAsync();
        return deleted == 0 ? NotFound() : NoContent();
    }
}
