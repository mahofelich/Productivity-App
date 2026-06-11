using System.ComponentModel.DataAnnotations;

namespace ProductivityHub.Models;

public class Habit
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(50)] public string Icon { get; set; } = "fa-check";
    [MaxLength(20)] public string Color { get; set; } = "#10b981";
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<HabitLog> Logs { get; set; } = [];
}

public class HabitLog
{
    public int Id { get; set; }
    public int HabitId { get; set; }
    public Habit? Habit { get; set; }
    public DateOnly Date { get; set; }
}
