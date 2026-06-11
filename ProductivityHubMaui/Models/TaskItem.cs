using System.ComponentModel.DataAnnotations;

namespace ProductivityHub.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }

    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(2000)] public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    [MaxLength(50)] public string Category { get; set; } = "General";
    public int? EstimatedMinutes { get; set; }
    public TaskState State { get; set; } = TaskState.Todo;
    public bool IsImportant { get; set; }   // Eisenhower axis
    public bool IsUrgent { get; set; }      // Eisenhower axis
    public bool IsFocusTask { get; set; }   // daily focus
    [MaxLength(300)] public string? Tags { get; set; }
    public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public int? ParentTaskId { get; set; }          // subtasks
    public TaskItem? ParentTask { get; set; }
    public List<TaskItem> Subtasks { get; set; } = [];

    public int? DependsOnTaskId { get; set; }       // dependency
    public TaskItem? DependsOnTask { get; set; }
}
