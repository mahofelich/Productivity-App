using Microsoft.EntityFrameworkCore;
using ProductivityHub.Data;
using ProductivityHub.Models;

namespace ProductivityHub.Services;

public interface IGamificationService
{
    Task AwardXpAsync(string userId, int xp);
    Task CheckAchievementsAsync(string userId);
    int XpForLevel(int level);
}

public class GamificationService(IDbContextFactory<AppDbContext> dbf, IStreakService streaks) : IGamificationService
{
    public int XpForLevel(int level) => level * level * 100;

    public async Task AwardXpAsync(string userId, int xp)
    {
        await using var db = dbf.CreateDbContext();
        var user = await db.Users.FindAsync(userId);
        if (user is null) return;
        user.Xp += xp;
        while (user.Xp >= XpForLevel(user.Level)) user.Level++;
        await db.SaveChangesAsync();
    }

    public async Task CheckAchievementsAsync(string userId)
    {
        await using var db = dbf.CreateDbContext();
        var owned = await db.UserAchievements.Where(a => a.UserId == userId)
            .Select(a => a.Achievement!.Code).ToListAsync();
        var all = await db.Achievements.ToListAsync();

        async Task GrantAsync(string code)
        {
            if (owned.Contains(code)) return;
            var ach = all.FirstOrDefault(a => a.Code == code);
            if (ach is null) return;
            db.UserAchievements.Add(new UserAchievement { UserId = userId, AchievementId = ach.Id });
            await db.SaveChangesAsync();
            owned.Add(code);
            await AwardXpAsync(userId, ach.XpReward);
        }

        var completedTasks = await db.Tasks.CountAsync(t => t.UserId == userId && t.State == TaskState.Done);
        if (completedTasks >= 1) await GrantAsync("FIRST_TASK");
        if (completedTasks >= 50) await GrantAsync("TASK_CRUSHER");

        var journalCount = await db.JournalEntries.CountAsync(j => j.UserId == userId);
        if (journalCount >= 10) await GrantAsync("JOURNAL_10");

        var user = await db.Users.FindAsync(userId);
        if (user is not null && await streaks.WaterStreakAsync(userId, user.WaterGoalMl) >= 7)
            await GrantAsync("HYDRATION_MASTER");

        var habits = await db.Habits.Where(h => h.UserId == userId && !h.IsArchived).Select(h => h.Id).ToListAsync();
        foreach (var h in habits)
        {
            if (await streaks.HabitStreakAsync(h) >= 7) { await GrantAsync("STREAK_7"); break; }
        }
    }
}
