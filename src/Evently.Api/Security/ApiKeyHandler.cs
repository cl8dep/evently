using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Evently.Api;

// Signs a request in as the key's owner, with the owner's current roles.
public class ApiKeyHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, AppDb db, UserManager<AppUser> users)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        var secret = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : Request.Headers["X-Api-Key"].ToString();
        if (!secret.StartsWith(ApiKeys.Prefix)) return AuthenticateResult.NoResult();

        var hash = ApiKeys.Hash(secret);
        var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Hash == hash);
        var user = key is null ? null : await users.FindByIdAsync(key.UserId);
        if (key is null || user is null) return AuthenticateResult.Fail("Invalid API key.");

        if (key.LastUsedAt is null || key.LastUsedAt < DateTime.UtcNow.AddHours(-1))
        {
            key.LastUsedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id), new(ClaimTypes.Name, user.UserName!) };
        claims.AddRange((await users.GetRolesAsync(user)).Select(r => new Claim(ClaimTypes.Role, r)));
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name));
    }
}
