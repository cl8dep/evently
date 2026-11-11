using Microsoft.AspNetCore.Mvc;

namespace Evently.Api;

public static class ControllerExtensions
{
    // Same 400 validation problem the minimal endpoints return through Failure.Invalid.
    public static ActionResult Invalid(this ControllerBase c, string field, string message)
    {
        c.ModelState.AddModelError(field, message);
        return c.ValidationProblem(c.ModelState);
    }
}
