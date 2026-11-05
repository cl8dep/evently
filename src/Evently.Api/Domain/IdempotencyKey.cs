namespace Evently.Api;

public class IdempotencyKey
{
    public string Key { get; set; } = "";
    public int EventId { get; set; }
    public Event? Event { get; set; }
    public DateTime CreatedAt { get; set; }
}
