using Microsoft.EntityFrameworkCore;
using ProductivityHub.Data;
using ProductivityHub.Models;

namespace ProductivityHub.Services;

public record DashboardData(
    List<CalendarEvent> UpcomingEvents,
    List<TaskItem> TasksDueToday,
    int WaterMlToday, int WaterGoalMl,
    int CaloriesToday, int CalorieGoal,
    int ProductivityScore,
    int HabitsDoneToday, int HabitsTotal,
    int JournalStreak,
    string Quote, string Insight,
    int Xp, int Level, int XpForNextLevel);

public interface IDashboardService
{
    Task<DashboardData> BuildAsync(string userId);
}

public class DashboardService(
    IDbContextFactory<AppDbContext> dbf,
    IStreakService streaks,
    IInsightService insights,
    IGamificationService game) : IDashboardService
{
    private static readonly string[] Quotes =
    [
        "Small steps every day add up to big results.",
        "Focus on progress, not perfection.",
        "The secret of getting ahead is getting started.",
        "Discipline is choosing what you want most over what you want now.",
        "You don't have to be great to start, but you have to start to be great.",
        "Done is better than perfect.",
        "Energy and persistence conquer all things."
    ];

    public async Task<DashboardData> BuildAsync(string userId)
    {
        await using var db = dbf.CreateDbContext();
        var user = await db.Users.FirstAsync(u => u.Id == userId);
        var today = DateTime.Today;
        var todayOnly = DateOnly.FromDateTime(today);

        var events = await db.Events
            .Where(e => e.UserId == userId && e.StartTime >= DateTime.Now && e.StartTime < today.AddDays(2))
            .OrderBy(e => e.StartTime).Take(5).ToListAsync();

        var tasks = await db.Tasks
            .Where(t => t.UserId == userId && t.State != TaskState.Done &&
                        t.DueDate != null && t.DueDate < today.AddDays(1))
            .OrderByDescending(t => t.Priority).Take(8).ToListAsync();

        var waterToday = await db.WaterEntries
            .Where(w => w.UserId == userId && w.LoggedAt >= today)
            .SumAsync(w => (int?)w.AmountMl) ?? 0;

        var caloriesToday = await db.FoodEntries
            .Where(f => f.Meal!.UserId == userId && f.Meal.Date == todayOnly)
            .SumAsync(f => (int?)f.Calories) ?? 0;

        var habitIds = await db.Habits.Where(h => h.UserId == userId && !h.IsArchived).Select(h => h.Id).ToListAsync();
        var habitsDone = await db.HabitLogs.CountAsync(l => habitIds.Contains(l.HabitId) && l.Date == todayOnly);

        var score = await ComputeScoreAsync(db, userId, user, todayOnly, waterToday, habitsDone, habitIds.Count);
        var insight = await insights.DailyInsightAsync(userId);
        var quote = Quotes[(int)(today.DayOfYear % Quotes.Length)];

        return new DashboardData(
            events, tasks, waterToday, user.WaterGoalMl, caloriesToday, user.CalorieGoal,
            score, habitsDone, habitIds.Count,
            await streaks.JournalStreakAsync(userId),
            quote, insight, user.Xp, user.Level, game.XpForLevel(user.Level));
    }

    private static async Task<int> ComputeScoreAsync(AppDbContext db, string userId, AppUser user, DateOnly today, int waterMl, int habitsDone, int habitsTotal)
    {
        var dueToday = await db.Tasks.CountAsync(t => t.UserId == userId && t.DueDate != null && DateOnly.FromDateTime(t.DueDate.Value) == today);
        var doneToday = await db.Tasks.CountAsync(t => t.UserId == userId && t.State == TaskState.Done && t.CompletedAt != null && DateOnly.FromDateTime(t.CompletedAt.Value) == today);
        var journaled = await db.JournalEntries.AnyAsync(j => j.UserId == userId && j.Date == today);

        double taskPart = dueToday == 0 ? (doneToday > 0 ? 1 : 0.5) : Math.Min(1.0, (double)doneToday / dueToday);
        double waterPart = Math.Min(1.0, (double)waterMl / user.WaterGoalMl);
        double habitPart = habitsTotal == 0 ? 0.5 : (double)habitsDone / habitsTotal;
        double journalPart = journaled ? 1 : 0;

        int score = (int)Math.Round((taskPart * 40 + waterPart * 20 + habitPart * 25 + journalPart * 15));

        var existing = await db.ProductivityScores.FirstOrDefaultAsync(p => p.UserId == userId && p.Date == today);
        if (existing is null) db.ProductivityScores.Add(new ProductivityScore { UserId = userId, Date = today, Score = score });
        else existing.Score = score;
        await db.SaveChangesAsync();
        return score;
    }
}
