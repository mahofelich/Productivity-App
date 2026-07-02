using ProductivityHub.Data;

namespace ProductivityHub.Services;

/// <summary>
/// Cloud-sync settings. The REAL values live in SyncConfig.Secrets.cs, which is
/// gitignored so your keys/RowId never get committed. To enable sync, copy
/// SyncConfig.Secrets.cs.example to SyncConfig.Secrets.cs and fill it in.
/// If that file is absent, sync stays off and the app still runs.
/// </summary>
public static partial class SyncConfig
{
    static partial void LoadSecrets(ref string url, ref string key, ref string rowId);

    public static string SupabaseUrl { get; }
    public static string AnonKey { get; }
    public static string RowId { get; }

    static SyncConfig()
    {
        string url = "https://YOUR-PROJECT.supabase.co";
        string key = "YOUR-PUBLISHABLE-OR-ANON-KEY";
        string rowId = "CHANGE-ME";
        LoadSecrets(ref url, ref key, ref rowId);
        SupabaseUrl = url;
        AnonKey = key;
        RowId = rowId;
    }

    // Resolved per-host via AppPaths.DataDirectory (set in MauiProgram / Program.cs).
    public static string DbPath => AppPaths.DbPath;

    public static bool Configured =>
        SupabaseUrl.StartsWith("https://") && !SupabaseUrl.Contains("YOUR-PROJECT") &&
        AnonKey.Length > 10 && !AnonKey.Contains("YOUR-") &&
        RowId.Length > 0 && !RowId.Contains("CHANGE-ME");
}
