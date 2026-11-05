using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static Evently.Api.AppRoles;

namespace Evently.Api;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/users").RequireAuthorization("Admin");

        g.MapGet("/", (AppDb db) =>
            (from u in db.Users
             join ur in db.UserRoles on u.Id equals ur.UserId into urs
             from ur in urs.DefaultIfEmpty()
             join r in db.Roles on ur.RoleId equals r.Id into rs
             from r in rs.DefaultIfEmpty()
             orderby u.FirstName, u.LastName, u.Email
             select new { u.Id, u.FirstName, u.LastName, u.Email, Role = r.Name ?? Viewer }).ToListAsync());

        g.MapPost("/", async (UserIn body, UserManager<AppUser> um) =>
        {
            var email = body.Email?.Trim();
            var first = body.FirstName?.Trim();
            var last = body.LastName?.Trim();
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrEmpty(first) || first.Length > 100) errors["firstName"] = ["First name is required (max 100)."];
            if (string.IsNullOrEmpty(last) || last.Length > 100) errors["lastName"] = ["Last name is required (max 100)."];
            if (string.IsNullOrEmpty(email) || !new EmailAddressAttribute().IsValid(email)) errors["email"] = ["Enter a valid email."];
            if (string.IsNullOrEmpty(body.Password)) errors["password"] = ["Password is required."];
            if (!AppRoles.All.Contains(body.Role)) errors["role"] = ["Role must be Admin, Editor or Viewer."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var user = new AppUser { UserName = email, Email = email, FirstName = first!, LastName = last! };
            var created = await um.CreateAsync(user, body.Password!);
            if (!created.Succeeded) return Failure.FromIdentity(created);
            await um.AddToRoleAsync(user, body.Role!);
            return Results.Created($"/api/users/{user.Id}", new { user.Id, user.FirstName, user.LastName, user.Email, body.Role });
        });

        g.MapPut("/{id}", async (string id, UserIn body, UserManager<AppUser> um) =>
        {
            var user = await um.FindByIdAsync(id);
            if (user is null) return Results.NotFound();
            var email = body.Email?.Trim();
            var first = body.FirstName?.Trim();
            var last = body.LastName?.Trim();
            if (first is not null && (first.Length is 0 or > 100)) return Failure.Invalid("firstName", "First name is required (max 100).");
            if (last is not null && (last.Length is 0 or > 100)) return Failure.Invalid("lastName", "Last name is required (max 100).");
            if (body.Role is not null && !AppRoles.All.Contains(body.Role)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["role"] = ["Role must be Admin, Editor or Viewer."] });
            if (!string.IsNullOrEmpty(email) && !new EmailAddressAttribute().IsValid(email)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["email"] = ["Enter a valid email."] });

            var current = (await um.GetRolesAsync(user)).FirstOrDefault();
            if (body.Role is not null && current == Admin && body.Role != Admin && await IsLastAdmin(um))
                return Results.Conflict(new { error = "This is the only admin. Make someone else an admin first." });

            if (first is not null || last is not null)
            {
                user.FirstName = first ?? user.FirstName;
                user.LastName = last ?? user.LastName;
                var r = await um.UpdateAsync(user);
                if (!r.Succeeded) return Failure.FromIdentity(r);
            }
            if (!string.IsNullOrEmpty(email) && email != user.Email)
            {
                var r = await um.SetUserNameAsync(user, email);
                if (r.Succeeded) r = await um.SetEmailAsync(user, email);
                if (!r.Succeeded) return Failure.FromIdentity(r);
            }
            if (!string.IsNullOrEmpty(body.Password))
            {
                var r = await um.ResetPasswordAsync(user, await um.GeneratePasswordResetTokenAsync(user), body.Password);
                if (!r.Succeeded) return Failure.FromIdentity(r);
            }
            if (body.Role is not null && body.Role != current)
            {
                if (current is not null) await um.RemoveFromRoleAsync(user, current);
                await um.AddToRoleAsync(user, body.Role);
            }
            return Results.Ok(new { user.Id, user.FirstName, user.LastName, user.Email, Role = body.Role ?? current ?? Viewer });
        });

        g.MapDelete("/{id}", async (string id, ClaimsPrincipal me, UserManager<AppUser> um) =>
        {
            var user = await um.FindByIdAsync(id);
            if (user is null) return Results.NotFound();
            if (user.Id == um.GetUserId(me)) return Results.Conflict(new { error = "You can't delete your own account." });
            if (await um.IsInRoleAsync(user, Admin) && await IsLastAdmin(um))
                return Results.Conflict(new { error = "This is the only admin." });
            await um.DeleteAsync(user);
            return Results.NoContent();
        });
    }

    static async Task<bool> IsLastAdmin(UserManager<AppUser> um) => (await um.GetUsersInRoleAsync(Admin)).Count <= 1;

    static IResult Problem(IdentityResult r) =>
        Results.ValidationProblem(r.Errors.GroupBy(e => e.Code.StartsWith("Password") ? "password" : "email")
            .ToDictionary(x => x.Key, x => x.Select(e => e.Description).ToArray()));
}
