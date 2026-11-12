namespace Evently.Api;

public class Comment
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Author { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
}
