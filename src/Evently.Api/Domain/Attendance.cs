namespace Evently.Api;

public class Attendance
{
    public int EventId { get; set; }
    public string UserName { get; set; } = "";
    public AttendanceStatus Status { get; set; }
    public DateTime UpdatedAt { get; set; }
}
