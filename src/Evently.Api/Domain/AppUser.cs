using Microsoft.AspNetCore.Identity;

namespace Evently.Api;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}
