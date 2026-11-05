namespace Evently.Api;

public class EventIcon
{
    public int EventId { get; set; }
    public byte[] Data { get; set; } = [];
    public string ContentType { get; set; } = "";
}
