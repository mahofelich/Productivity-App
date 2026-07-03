using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProductivityHub.Data;

namespace ProductivityHub.Services;

/// <summary>
/// Cloud sync backed by Google Drive. Same model as the Supabase version:
/// the whole SQLite file is the unit of sync — Backup pushes this device's
/// database to a Drive folder, Restore replaces this device's database with
/// the Drive copy. Auth is a Google service account (server-to-server JWT),
/// so there is no per-device sign-in: the key in DriveSyncConfig.Secrets.cs
/// is compiled into every build and all devices share one backup file.
///
/// Implemented against the Drive v3 REST API with plain HttpClient — no
/// Google SDK packages needed.
/// </summary>
public class GoogleDriveSyncService : ISyncService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private const string TokenUrl = "https://oauth2.googleapis.com/token";
    private const string Scope = "https://www.googleapis.com/auth/drive";
    private const string FilesUrl = "https://www.googleapis.com/drive/v3/files";
    private const string UploadUrl = "https://www.googleapis.com/upload/drive/v3/files";

    private string? _token;
    private DateTimeOffset _tokenExpires = DateTimeOffset.MinValue;

    public bool Configured => DriveSyncConfig.Configured;
    public string ProviderName => "Google Drive";

    public async Task<RemoteInfo> GetRemoteInfoAsync()
    {
        if (!Configured) return new RemoteInfo(false, null);
        var file = await FindBackupAsync();
        if (file is null) return new RemoteInfo(false, null);
        return new RemoteInfo(true, file.Value.ModifiedTime);
    }

    public async Task BackupAsync()
    {
        if (!Configured) throw new InvalidOperationException(
            "Drive sync isn't configured yet. Copy Services/DriveSyncConfig.Secrets.cs.example to DriveSyncConfig.Secrets.cs and fill it in.");

        FlushLocalDb();
        var bytes = await File.ReadAllBytesAsync(AppPaths.DbPath);

        var existing = await FindBackupAsync();
        if (existing is null)
            await CreateFileAsync(bytes);
        else
            await UpdateFileAsync(existing.Value.Id, bytes);
    }

    public async Task RestoreAsync()
    {
        if (!Configured) throw new InvalidOperationException("Drive sync isn't configured yet.");

        var existing = await FindBackupAsync()
            ?? throw new Exception("No cloud backup found in the Drive folder yet. Back up from another device first.");

        using var req = await NewRequestAsync(HttpMethod.Get, $"{FilesUrl}/{existing.Value.Id}?alt=media&supportsAllDrives=true");
        using var res = await Http.SendAsync(req);
        await EnsureOkAsync(res, "Restore");
        var bytes = await res.Content.ReadAsByteArrayAsync();
        if (bytes.Length == 0) throw new Exception("The cloud backup is empty.");

        FlushLocalDb();

        // Remove WAL/SHM sidecar files so they can't conflict with the new db.
        foreach (var ext in new[] { "-wal", "-shm" })
        {
            var p = AppPaths.DbPath + ext;
            if (File.Exists(p)) File.Delete(p);
        }

        await File.WriteAllBytesAsync(AppPaths.DbPath, bytes);
    }

    // ---------------- Drive REST helpers ----------------

    private async Task<(string Id, DateTimeOffset? ModifiedTime)?> FindBackupAsync()
    {
        var q = Uri.EscapeDataString(
            $"name = '{DriveSyncConfig.FileName.Replace("'", "\\'")}' and '{DriveSyncConfig.FolderId}' in parents and trashed = false");
        var url = $"{FilesUrl}?q={q}&fields=files(id,modifiedTime)&supportsAllDrives=true&includeItemsFromAllDrives=true";

        using var req = await NewRequestAsync(HttpMethod.Get, url);
        using var res = await Http.SendAsync(req);
        await EnsureOkAsync(res, "Cloud check");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var files = doc.RootElement.GetProperty("files");
        if (files.GetArrayLength() == 0) return null;

        var f = files[0];
        DateTimeOffset? modified = null;
        if (f.TryGetProperty("modifiedTime", out var mt) && mt.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(mt.GetString(), out var dto))
            modified = dto;
        return (f.GetProperty("id").GetString()!, modified);
    }

    private async Task CreateFileAsync(byte[] bytes)
    {
        // Multipart upload: JSON metadata part (name + parent folder) + media part.
        var metadata = JsonSerializer.Serialize(new
        {
            name = DriveSyncConfig.FileName,
            parents = new[] { DriveSyncConfig.FolderId }
        });

        var content = new MultipartContent("related");
        var metaPart = new StringContent(metadata, Encoding.UTF8, "application/json");
        var dataPart = new ByteArrayContent(bytes);
        dataPart.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(metaPart);
        content.Add(dataPart);

        using var req = await NewRequestAsync(HttpMethod.Post, $"{UploadUrl}?uploadType=multipart&supportsAllDrives=true");
        req.Content = content;
        using var res = await Http.SendAsync(req);
        await EnsureOkAsync(res, "Backup");
    }

    private async Task UpdateFileAsync(string fileId, byte[] bytes)
    {
        using var req = await NewRequestAsync(HttpMethod.Patch, $"{UploadUrl}/{fileId}?uploadType=media&supportsAllDrives=true");
        req.Content = new ByteArrayContent(bytes);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var res = await Http.SendAsync(req);
        await EnsureOkAsync(res, "Backup");
    }

    private async Task<HttpRequestMessage> NewRequestAsync(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());
        return req;
    }

    private static async Task EnsureOkAsync(HttpResponseMessage res, string what)
    {
        if (res.IsSuccessStatusCode) return;
        var body = await res.Content.ReadAsStringAsync();
        var hint = (int)res.StatusCode switch
        {
            401 => "The service account key was rejected — re-check client_email and private_key in DriveSyncConfig.Secrets.cs.",
            403 => "Permission denied — make sure the Drive folder is shared with the service account's email as Editor, and that the Drive API is enabled in your Google Cloud project.",
            404 => "The folder id wasn't found — re-check folderId in DriveSyncConfig.Secrets.cs.",
            _ => ""
        };
        throw new Exception($"{what} failed (HTTP {(int)res.StatusCode}). {hint} {Truncate(body)}");
    }

    private static string Truncate(string s) => s.Length <= 300 ? s : s[..300] + "…";

    // ---------------- Service-account OAuth (signed JWT) ----------------

    private async Task<string> GetAccessTokenAsync()
    {
        if (_token is not null && DateTimeOffset.UtcNow < _tokenExpires - TimeSpan.FromMinutes(2))
            return _token;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string header = B64Url(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
        string claims = B64Url(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            iss = DriveSyncConfig.ServiceAccountEmail,
            scope = Scope,
            aud = TokenUrl,
            iat = now,
            exp = now + 3600
        })));

        using var rsa = RSA.Create();
        try { rsa.ImportFromPem(DriveSyncConfig.PrivateKeyPem); }
        catch (Exception ex)
        {
            throw new Exception("Couldn't read the service account private key — paste the whole \"private_key\" value from the JSON key file into DriveSyncConfig.Secrets.cs. (" + ex.Message + ")");
        }

        var unsigned = $"{header}.{claims}";
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var jwt = $"{unsigned}.{B64Url(signature)}";

        using var res = await Http.PostAsync(TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            ["assertion"] = jwt
        }));
        var json = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
            throw new Exception($"Google sign-in failed (HTTP {(int)res.StatusCode}). Re-check the values in DriveSyncConfig.Secrets.cs. {Truncate(json)}");

        using var doc = JsonDocument.Parse(json);
        _token = doc.RootElement.GetProperty("access_token").GetString();
        var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;
        _tokenExpires = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        return _token!;
    }

    private static string B64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // Checkpoint and release any pooled connections so the file on disk is
    // complete and not locked before we read or overwrite it.
    private static void FlushLocalDb()
    {
        try
        {
            if (File.Exists(AppPaths.DbPath))
            {
                using var conn = new SqliteConnection($"Data Source={AppPaths.DbPath}");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                cmd.ExecuteNonQuery();
            }
        }
        catch { /* best effort */ }
        SqliteConnection.ClearAllPools();
    }
}
