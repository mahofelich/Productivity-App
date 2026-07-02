namespace ProductivityHub.Models;

public class JournalEntry
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }
    public DateOnly Date { get; set; }
    public string Content { get; set; } = "";
    public int Mood { get; set; } = 3; // 1-5
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
