using System.ComponentModel.DataAnnotations;

namespace ProductivityHub.Models;

public class ReflectionPrompt
{
    public int Id { get; set; }
    [Required, MaxLength(300)] public string Text { get; set; } = "";
    public PromptTime Time { get; set; }
    public int SortOrder { get; set; }
}

public class ReflectionResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }
    public int PromptId { get; set; }
    public ReflectionPrompt? Prompt { get; set; }
    public DateOnly Date { get; set; }
    public string Answer { get; set; } = "";
}
