using System.ComponentModel.DataAnnotations;

namespace ProductivityHub.Models;

public class Achievement
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(300)] public string Description { get; set; } = "";
    [MaxLength(50)] public string Icon { get; set; } = "fa-trophy";
    [MaxLength(50)] public string Code { get; set; } = "";   // e.g. STREAK_7
    public int XpReward { get; set; } = 50;
}

public class UserAchievement
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }
    public int AchievementId { get; set; }
    public Achievement? Achievement { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}

public class ProductivityScore
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }
    public DateOnly Date { get; set; }
    public int Score { get; set; }   // 0-100
}
