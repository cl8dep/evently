using Microsoft.AspNetCore.Identity;

namespace Evently.Api;

public static class Failure
{
    public static IResult Invalid(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    public static IResult FromIdentity(IdentityResult r) =>
        Results.ValidationProblem(r.Errors.GroupBy(e => e.Code.StartsWith("Password") ? "password" : "email")
            .ToDictionary(x => x.Key, x => x.Select(e => e.Description).ToArray()));
}
