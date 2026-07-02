using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductivityHub.Data;
using ProductivityHub.Services;

namespace ProductivityHub;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // On-device SQLite in the app's private data directory.
        // The shared library is platform-agnostic, so tell it where this host
        // stores data before anything resolves the database path.
        AppPaths.DataDirectory = FileSystem.AppDataDirectory;
        var dbPath = SyncConfig.DbPath;
        builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite($"Filename={dbPath}"));

        builder.Services.AddScoped<IStreakService, StreakService>();
        builder.Services.AddScoped<IGamificationService, GamificationService>();
        builder.Services.AddScoped<IInsightService, InsightService>();
        builder.Services.AddScoped<IRecommendationService, RecommendationService>();
        builder.Services.AddScoped<IDashboardService, DashboardService>();
        builder.Services.AddSingleton<ISyncService, SyncService>();
        builder.Services.AddSingleton<IEmailService, EmailService>();

        var app = builder.Build();

        // Create + seed the database on first launch.
        using (var scope = app.Services.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = factory.CreateDbContext();
            db.Database.EnsureCreated();
            DbSeeder.Seed(db);
        }

        return app;
    }
}
