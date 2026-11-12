namespace Evently.Api;

public record CommentPage(List<CommentOut> Items, bool HasMore);
