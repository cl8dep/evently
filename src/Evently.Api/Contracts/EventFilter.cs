namespace Evently.Api;

public record EventFilter(int? CategoryId, int? TopicId, string? Country, string? Region, string? Q);
