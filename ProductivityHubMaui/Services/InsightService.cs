using Microsoft.EntityFrameworkCore;
using ProductivityHub.Data;
using ProductivityHub.Models;

namespace ProductivityHub.Services;

public interface IInsightService
{
    Task<string> DailyInsightAsync(string userId);
    Task<List<string>> FullInsightsAsync(string userId);
}

/// <summary>
/// Rule-based "AI" insight engine. Swap the internals for a call to an
/// LLM API later — the interface stays the same.
/// </summary>
public class InsightService(IDbContextFactory<AppDbContext> dbf) : IInsightService
{
    public async Task<string> DailyInsightAsync(string userId)
    {
        var list = await FullInsightsAsync(userId);
        return list.FirstOrDefault() ?? "You're all caught up. Pick one meaningful task and give it a focused block of time.";
    }

    public async Task<List<string>> FullInsightsAsync(string userId)
    {
        await using var db = dbf.CreateDbContext();
        var insights = new List<string>();
        var now = DateTime.Now;
        var today = DateTime.Today;
        var weekAhead = today.AddDays(7);

        var user = await db.Users.FirstAsync(u => u.Id == userId);

        var deadlines = await db.Tasks.CountAsync(t => t.UserId == userId && t.State != TaskState.Done
            && t.DueDate != null && t.DueDate >= today && t.DueDate < weekAhead);

        var avgWater = await db.WaterEntries
            .Where(w => w.UserId == userId && w.LoggedAt >= today.AddDays(-7) && w.LoggedAt < today)
            .GroupBy(w => w.LoggedAt.Date).Select(g => g.Sum(x => x.AmountMl))
            .ToListAsync();
        var waterToday = await db.WaterEntries.Where(w => w.UserId == userId && w.LoggedAt >= today).SumAsync(w => (int?)w.AmountMl) ?? 0;
        var avg = avgWater.Count > 0 ? avgWater.Average() : 0;

        if (deadlines >= 3 && avg > 0 && waterToday < avg * (now.Hour / 24.0))
            insights.Add($"You have {deadlines} deadlines approaching this week and have consumed less water than usual. Consider scheduling focused work blocks and increasing hydration.");
        else if (deadlines >= 3)
            insights.Add($"You have {deadlines} deadlines this week. Try time-blocking your two highest-priority tasks first thing tomorrow.");

        var overdue = await db.Tasks.CountAsync(t => t.UserId == userId && t.State != TaskState.Done && t.DueDate != null && t.DueDate < today);
        if (overdue > 0)
            insights.Add($"{overdue} task{(overdue > 1 ? "s are" : " is")} overdue. Reschedule or break them into smaller subtasks to regain momentum.");

        var lateEvents = await db.Events.CountAsync(e => e.UserId == userId && e.StartTime >= today && e.StartTime < today.AddDays(1) && e.StartTime.Hour >= 19);
        var doneLast3 = await db.Tasks.CountAsync(t => t.UserId == userId && t.CompletedAt >= today.AddDays(-3));
        if (doneLast3 > 15 || lateEvents > 2)
            insights.Add("Your pace has been intense lately. Watch for burnout — schedule a real break and protect your evening.");

        var journaled = await db.JournalEntries.AnyAsync(j => j.UserId == userId && j.Date == DateOnly.FromDateTime(today));
        if (!journaled && now.Hour >= 18)
            insights.Add("You haven't journaled today. A two-minute evening reflection keeps your streak and clears your head.");

        var habitIds = await db.Habits.Where(h => h.UserId == userId && !h.IsArchived).Select(h => h.Id).ToListAsync();
        var habitsDone = await db.HabitLogs.CountAsync(l => habitIds.Contains(l.HabitId) && l.Date == DateOnly.FromDateTime(today));
        if (habitIds.Count > 0 && habitsDone == 0 && now.Hour >= 12)
            insights.Add("No habits checked off yet today. Start with the easiest one — momentum compounds.");

        if (insights.Count == 0)
            insights.Add("Everything looks on track. Use the extra headroom for deep work on your most important goal.");

        return insights;
    }
}
