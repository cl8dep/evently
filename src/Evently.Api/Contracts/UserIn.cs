namespace Evently.Api;

public record UserIn(string? Email, string? Password, string? Role, string? FirstName = null, string? LastName = null);
