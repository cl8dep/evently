namespace Evently.Api;

public record CommentOut(int Id, string Author, string AuthorName, string Body, DateTime CreatedAt);
