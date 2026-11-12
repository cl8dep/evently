namespace Evently.Api;

public class ApiKey
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Prefix { get; set; } = "";
    public string Hash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
