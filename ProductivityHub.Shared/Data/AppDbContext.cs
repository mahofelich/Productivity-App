using Microsoft.EntityFrameworkCore;
using ProductivityHub.Models;

namespace ProductivityHub.Data;

// The single local user's id. Used everywhere the web app used the auth claim.
public static class Local
{
    public const string UserId = "local-user";
}

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<CalendarEvent> Events => Set<CalendarEvent>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitLog> HabitLogs => Set<HabitLog>();
    public DbSet<WaterEntry> WaterEntries => Set<WaterEntry>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<FoodEntry> FoodEntries => Set<FoodEntry>();
    public DbSet<ReflectionPrompt> ReflectionPrompts => Set<ReflectionPrompt>();
    public DbSet<ReflectionResponse> ReflectionResponses => Set<ReflectionResponse>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<ProductivityScore> ProductivityScores => Set<ProductivityScore>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<AppUser>().HasKey(u => u.Id);

        b.Entity<TaskItem>()
            .HasOne(t => t.ParentTask).WithMany(t => t.Subtasks)
            .HasForeignKey(t => t.ParentTaskId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TaskItem>()
            .HasOne(t => t.DependsOnTask).WithMany()
            .HasForeignKey(t => t.DependsOnTaskId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TaskItem>().HasIndex(t => new { t.UserId, t.State });

        b.Entity<CalendarEvent>().HasIndex(e => new { e.UserId, e.StartTime });
        b.Entity<HabitLog>().HasIndex(l => new { l.HabitId, l.Date }).IsUnique();
        b.Entity<WaterEntry>().HasIndex(w => new { w.UserId, w.LoggedAt });
        b.Entity<Meal>().HasIndex(m => new { m.UserId, m.Date });
        b.Entity<JournalEntry>().HasIndex(j => new { j.UserId, j.Date }).IsUnique();
        b.Entity<ProductivityScore>().HasIndex(p => new { p.UserId, p.Date }).IsUnique();
        b.Entity<ReflectionResponse>().HasIndex(r => new { r.UserId, r.Date, r.PromptId }).IsUnique();
        b.Entity<UserAchievement>().HasIndex(a => new { a.UserId, a.AchievementId }).IsUnique();
    }
}
