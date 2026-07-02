namespace ProductivityHub.Models;

// Plain entity — the Android build is single-user and local, so there is no
// ASP.NET Identity. One AppUser row (Id = Local.UserId) is seeded on first run.
public class AppUser
{
    public string Id { get; set; } = "local-user";
    public string DisplayName { get; set; } = "";
    public int Xp { get; set; }
    public int Level { get; set; } = 1;
    public int WaterGoalMl { get; set; } = 2500;
    public int CalorieGoal { get; set; } = 2200;
    public int ProteinGoalG { get; set; } = 120;
    public int CarbGoalG { get; set; } = 250;
    public int FatGoalG { get; set; } = 70;
    public bool PrefersDarkMode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CalendarEvent> Events { get; set; } = [];
    public List<TaskItem> Tasks { get; set; } = [];
    public List<Note> Notes { get; set; } = [];
    public List<Habit> Habits { get; set; } = [];
    public List<WaterEntry> WaterEntries { get; set; } = [];
    public List<Meal> Meals { get; set; } = [];
    public List<UserAchievement> Achievements { get; set; } = [];
}
