namespace Evently.Api;

public record SearchEvent(int Id, string Title, DateOnly StartDate, string? City, string? Country, DateTime? IconUpdatedAt);
