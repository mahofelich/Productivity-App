using System.ComponentModel.DataAnnotations;

namespace ProductivityHub.Models;

public class Note
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }

    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string Content { get; set; } = "";      // markdown
    public NoteKind Kind { get; set; } = NoteKind.Quick;
    [MaxLength(50)] public string Category { get; set; } = "General";
    [MaxLength(300)] public string? Tags { get; set; }
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
