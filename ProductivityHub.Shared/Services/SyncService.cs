using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace ProductivityHub.Services;

public record RemoteInfo(bool Exists, DateTimeOffset? UpdatedAt);

public interface ISyncService
{
    bool Configured { get; }
    string ProviderName { get; }
    Task<RemoteInfo> GetRemoteInfoAsync();
    Task BackupAsync();
    Task RestoreAsync();
}

/// <summary>
/// Tier-1 cloud sync: uploads/downloads the entire on-device SQLite file as a
/// single unit (newest copy wins). Because the app is single-user, the whole
/// database is one user's data, so a file-level copy keeps every row, id and
/// relationship perfectly consistent across devices — no per-record merge.
///
/// Backup  = push this device's database to the cloud.
/// Restore = replace this device's database with the cloud copy.
/// Both are explicit so a device never silently overwrites your data.
/// </summary>
public class SyncService : ISyncService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    public bool Configured => SyncConfig.Configured;
    public string ProviderName => "Supabase";

    private string Endpoint => $"{SyncConfig.SupabaseUrl.TrimEnd('/')}/rest/v1/app_state";

    private HttpRequestMessage NewRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.TryAddWithoutValidation("apikey", SyncConfig.AnonKey);
        // Legacy anon keys are JWTs (start with "eyJ") and also go in the Authorization
        // header. New publishable keys ("sb_publishable_...") must NOT be sent as Bearer
        // — the apikey header alone is correct for them.
        if (SyncConfig.AnonKey.StartsWith("eyJ", StringComparison.Ordinal))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SyncConfig.AnonKey);
        return req;
    }

    public async Task<RemoteInfo> GetRemoteInfoAsync()
    {
        if (!Configured) return new RemoteInfo(false, null);
        var url = $"{Endpoint}?id=eq.{Uri.EscapeDataString(SyncConfig.RowId)}&select=updated_at";
        using var req = NewRequest(HttpMethod.Get, url);
        using var res = await Http.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.GetArrayLength() == 0) return new RemoteInfo(false, null);
        var row = doc.RootElement[0];
        if (row.TryGetProperty("updated_at", out var ts) && ts.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(ts.GetString(), out var dto))
            return new RemoteInfo(true, dto);
        return new RemoteInfo(true, null);
    }

    public async Task BackupAsync()
    {
        if (!Configured) throw new InvalidOperationException("Sync isn't configured yet. Add your Supabase URL and key in Services/SyncConfig.cs.");

        FlushLocalDb();
        var bytes = await File.ReadAllBytesAsync(SyncConfig.DbPath);
        var b64 = Convert.ToBase64String(bytes);

        var payload = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["id"] = SyncConfig.RowId,
            ["db_base64"] = b64,
            ["updated_at"] = DateTimeOffset.UtcNow.ToString("o")
        });

        using var req = NewRequest(HttpMethod.Post, Endpoint);
        // upsert: insert or overwrite the single row by primary key
        req.Headers.TryAddWithoutValidation("Prefer", "resolution=merge-duplicates,return=minimal");
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var res = await Http.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            throw new Exception($"Backup failed (HTTP {(int)res.StatusCode}). {Hint(res.StatusCode)} {err}");
        }
    }

    public async Task RestoreAsync()
    {
        if (!Configured) throw new InvalidOperationException("Sync isn't configured yet.");

        var url = $"{Endpoint}?id=eq.{Uri.EscapeDataString(SyncConfig.RowId)}&select=db_base64";
        using var req = NewRequest(HttpMethod.Get, url);
        using var res = await Http.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            throw new Exception($"Restore failed (HTTP {(int)res.StatusCode}). {Hint(res.StatusCode)} {err}");
        }

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.GetArrayLength() == 0)
            throw new Exception("No cloud backup found yet. Back up from another device first.");

        var b64 = doc.RootElement[0].GetProperty("db_base64").GetString()
                  ?? throw new Exception("The cloud backup is empty.");
        var bytes = Convert.FromBase64String(b64);

        FlushLocalDb();

        // Remove WAL/SHM sidecar files so they can't conflict with the new db.
        foreach (var ext in new[] { "-wal", "-shm" })
        {
            var p = SyncConfig.DbPath + ext;
            if (File.Exists(p)) File.Delete(p);
        }

        await File.WriteAllBytesAsync(SyncConfig.DbPath, bytes);
    }

    // Checkpoint and release any pooled connections so the file on disk is
    // complete and not locked before we read or overwrite it.
    private static void FlushLocalDb()
    {
        try
        {
            if (File.Exists(SyncConfig.DbPath))
            {
                using var conn = new SqliteConnection($"Data Source={SyncConfig.DbPath}");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                cmd.ExecuteNonQuery();
            }
        }
        catch { /* best effort */ }
        SqliteConnection.ClearAllPools();
    }

    private static string Hint(System.Net.HttpStatusCode code) => (int)code switch
    {
        401 or 403 => "Looks like a permissions issue — make sure you ran the GRANT statements from the README.",
        404 => "The 'app_state' table wasn't found — create it with the README SQL.",
        _ => ""
    };
}
