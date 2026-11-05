namespace Evently.Api;

public record EventIn(
    string? Title, string? Description, int CategoryId, string? Website,
    decimal? TicketCost, string? Currency, DateOnly StartDate, DateOnly? EndDate, Location? Location, int[]? TopicIds = null);
