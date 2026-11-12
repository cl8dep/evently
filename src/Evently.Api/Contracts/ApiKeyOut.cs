namespace Evently.Api;

public record ApiKeyOut(int Id, string Name, string Prefix, DateTime CreatedAt, DateTime? LastUsedAt);
