using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
    var auth = app.MapGroup("/api/auth");
    // Public sign-up only bootstraps the first account (an Admin); everyone else is added from the Users page.
    auth.AddEndpointFilter(async (ctx, next) =>
    {
        if (!ctx.HttpContext.Request.Path.Value!.EndsWith("/register", StringComparison.OrdinalIgnoreCase)) return await next(ctx);
        var um = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
        if (await um.Users.AnyAsync()) return Results.Json(new { error = "Registration is closed. Ask an admin to add you." }, statusCode: 403);
        var result = await next(ctx);
        if (await um.Users.FirstOrDefaultAsync() is { } first) await um.AddToRoleAsync(first, AppRoles.Admin);
        return result;
    });
    auth.MapIdentityApi<AppUser>();
    auth.MapPost("/logout", async (SignInManager<AppUser> s) => { await s.SignOutAsync(); return Results.NoContent(); }).WithName("Logout");
    // The browser key only loads the interactive map. Places calls use a separate, server-side key.
    app.MapGet("/api/config", (PlacesClient places, IConfiguration c) => new ConfigOut(places.Enabled, c["GoogleMaps:BrowserKey"])).RequireAuthorization().WithName("GetConfig");
    app.MapGet("/api/me", async (ClaimsPrincipal u, UserManager<AppUser> um) =>
    {
        var user = await um.GetUserAsync(u);
        return new MeOut(
            u.Identity?.Name, user?.FirstName, user?.LastName, u.FindFirstValue(ClaimTypes.Role),
            u.Identity?.IsAuthenticated != true && !await um.Users.AnyAsync());
    }).WithName("GetMe");
    }
}
