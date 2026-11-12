using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Evently.Api;

public static class UserExtensions
{
    extension(ClaimsPrincipal user)
    {
        public string UserId
        {
            get { return user.FindFirstValue(ClaimTypes.NameIdentifier)!; }
        }

        public string UserName
        {
            get { return user.Identity!.Name!; }
        }
    }

    // The signed-in user of the current request, as `this.UserId` and `this.UserName` inside a controller.
    extension(ControllerBase controller)
    {
        public string UserId
        {
            get { return controller.User.UserId; }
        }

        public string UserName
        {
            get { return controller.User.UserName; }
        }
    }
}
