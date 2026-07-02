using Microsoft.EntityFrameworkCore;
using ProductivityHub.Data;
using ProductivityHub.Services;
using ProductivityHub.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Tell the shared library where this host stores its database.
AppPaths.DataDirectory = AppContext.BaseDirectory;
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite($"Filename={SyncConfig.DbPath}"));

builder.Services.AddScoped<IStreakService, StreakService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<IInsightService, InsightService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddSingleton<ISyncService, SyncService>();
builder.Services.AddSingleton<IEmailService, EmailService>();

var app = builder.Build();

// Create + seed the database on first run.
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ProductivityHub.Components.Routes).Assembly);

app.Run();
