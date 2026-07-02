using Microsoft.EntityFrameworkCore;
using ProductivityHub.Data;

namespace ProductivityHub.Services;

public record Recommendation(string Kind, string Title, string Detail, string Icon);

public interface IRecommendationService
{
    Task<List<Recommendation>> GetAsync(string userId);
}

public class RecommendationService(IDbContextFactory<AppDbContext> dbf) : IRecommendationService
{
    public async Task<List<Recommendation>> GetAsync(string userId)
    {
        await using var db = dbf.CreateDbContext();
        var recs = new List<Recommendation>();

        var categories = await db.Tasks.Where(t => t.UserId == userId)
            .GroupBy(t => t.Category).OrderByDescending(g => g.Count())
            .Select(g => g.Key).Take(2).ToListAsync();

        foreach (var c in categories)
        {
            recs.Add(new("Video", $"Deep-dive: leveling up in {c}", $"Search for highly rated tutorials and talks about {c.ToLower()} workflows.", "fa-circle-play"));
            recs.Add(new("Article", $"Reading list: {c}", $"Curated personal-development and {c.ToLower()} articles for this week.", "fa-newspaper"));
        }

        var waterToday = await db.WaterEntries.Where(w => w.UserId == userId && w.LoggedAt >= DateTime.Today).SumAsync(w => (int?)w.AmountMl) ?? 0;
        if (waterToday < 1500)
            recs.Add(new("Challenge", "Drink 3 liters of water", "Log every glass today and hit 3,000 ml before 9 PM.", "fa-droplet"));

        recs.Add(new("Challenge", "Complete 3 focus sessions", "Run three 25-minute Pomodoro blocks on your top task.", "fa-stopwatch"));
        recs.Add(new("Challenge", "Walk 8,000 steps", "Take a walking break between focus blocks.", "fa-person-walking"));
        recs.Add(new("Quote", "\u201CWhat gets measured gets managed.\u201D", "You're tracking it — now act on it.", "fa-quote-left"));

        return recs;
    }
}
