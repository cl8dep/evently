namespace Evently.Api;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public List<Topic> Topics { get; set; } = [];
    public string? Website { get; set; }
    public decimal? TicketCost { get; set; }
    public string? Currency { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Location Location { get; set; } = new();
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? IconUpdatedAt { get; set; }
}
