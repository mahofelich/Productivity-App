using System.ComponentModel.DataAnnotations;

namespace ProductivityHub.Models;

public class CalendarEvent
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }

    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(2000)] public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    [MaxLength(300)] public string? Location { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    [MaxLength(50)] public string Category { get; set; } = "General";
    [MaxLength(20)] public string Color { get; set; } = "#6366f1";
    [MaxLength(2000)] public string? Notes { get; set; }
    [MaxLength(300)] public string? Tags { get; set; } // comma separated
    public bool AllDay { get; set; }

    public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;
    public DateTime? RecurrenceEnd { get; set; }
    public int? ReminderMinutesBefore { get; set; }
}
