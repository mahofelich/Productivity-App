namespace ProductivityHub.Services;

/// <summary>
/// The color palettes the app ships with. Each key matches a
/// [data-theme="key"] block in wwwroot/css/site.css — to add your own palette,
/// add a CSS block there and one entry here. The choice is saved in the
/// browser/webview via localStorage (see phTheme in wwwroot/js/app.js).
/// </summary>
public static class ThemeCatalog
{
    public record ThemeInfo(string Key, string Name, string Accent, string Bg);

    public static readonly ThemeInfo[] All =
    [
        new("light",  "Light",  "#5b5bd6", "#f6f7fb"),
        new("dark",   "Dark",   "#8e8ef0", "#0f1119"),
        new("ocean",  "Ocean",  "#22d3ee", "#0a1626"),
        new("forest", "Forest", "#34d399", "#0c1512"),
        new("sunset", "Sunset", "#fb923c", "#1a1210"),
        new("rose",   "Rose",   "#db2777", "#fdf3f7"),
    ];
}

/// <summary>
/// Tiny in-process event hub so the layout (profile button) can refresh when
/// the display name is changed on the Settings page. Single-user local app,
/// so a static event is fine — subscribers must unsubscribe on dispose.
/// </summary>
public static class AppEvents
{
    public static event Action? UserChanged;
    public static void RaiseUserChanged() => UserChanged?.Invoke();
}
