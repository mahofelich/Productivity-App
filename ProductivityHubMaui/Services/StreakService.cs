using Microsoft.EntityFrameworkCore;
using ProductivityHub.Data;

namespace ProductivityHub.Services;

public interface IStreakService
{
    Task<int> HabitStreakAsync(int habitId);
    Task<int> WaterStreakAsync(string userId, int goalMl);
    Task<int> JournalStreakAsync(string userId);
}

public class StreakService(IDbContextFactory<AppDbContext> dbf) : IStreakService
{
    public async Task<int> HabitStreakAsync(int habitId)
    {
        await using var db = dbf.CreateDbContext();
        var dates = await db.HabitLogs.Where(l => l.HabitId == habitId)
            .Select(l => l.Date).OrderByDescending(d => d).ToListAsync();
        return CountStreak(dates);
    }

    public async Task<int> WaterStreakAsync(string userId, int goalMl)
    {
        await using var db = dbf.CreateDbContext();
        var byDay = await db.WaterEntries.Where(w => w.UserId == userId)
            .GroupBy(w => w.LoggedAt.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(x => x.AmountMl) })
            .ToListAsync();
        var dates = byDay.Where(x => x.Total >= goalMl)
            .Select(x => DateOnly.FromDateTime(x.Date))
            .OrderByDescending(d => d).ToList();
        return CountStreak(dates);
    }

    public async Task<int> JournalStreakAsync(string userId)
    {
        await using var db = dbf.CreateDbContext();
        var dates = await db.JournalEntries.Where(j => j.UserId == userId)
            .Select(j => j.Date).OrderByDescending(d => d).ToListAsync();
        return CountStreak(dates);
    }

    private static int CountStreak(List<DateOnly> datesDesc)
    {
        if (datesDesc.Count == 0) return 0;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expected = datesDesc[0] == today ? today
                     : datesDesc[0] == today.AddDays(-1) ? today.AddDays(-1)
                     : default;
        if (expected == default) return 0;

        int streak = 0;
        foreach (var d in datesDesc.Distinct())
        {
            if (d != expected) break;
            streak++;
            expected = expected.AddDays(-1);
        }
        return streak;
    }
}
