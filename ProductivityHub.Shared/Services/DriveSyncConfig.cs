namespace ProductivityHub.Services;

/// <summary>
/// Google Drive sync settings — an alternative backend to Supabase. Uses a
/// Google *service account*: one credential in a gitignored secrets file,
/// compiled into every build, so you set it up once and all of your devices
/// (web and mobile) sync through the same Drive folder. No per-device sign-in.
///
/// SETUP (one time, ~5 minutes):
///  1. console.cloud.google.com → create a project (any name).
///  2. "APIs &amp; Services" → Library → enable the **Google Drive API**.
///  3. "IAM &amp; Admin" → Service Accounts → Create service account → done
///     (no roles needed). Open it → Keys → Add key → JSON. A file downloads.
///  4. In YOUR Google Drive, create a folder (e.g. "ProductivityHub").
///     Share it with the service account's email (from the JSON,
///     ends in .iam.gserviceaccount.com) as **Editor**.
///  5. Copy the folder id from its URL: drive.google.com/drive/folders/&lt;THIS&gt;.
///  6. Copy DriveSyncConfig.Secrets.cs.example → DriveSyncConfig.Secrets.cs
///     and paste in client_email, private_key, and the folder id, then rebuild.
///
/// When this is configured it takes priority over Supabase (see the ISyncService
/// registration in Program.cs / MauiProgram.cs).
/// </summary>
public static partial class DriveSyncConfig
{
    static partial void LoadSecrets(
        ref string serviceAccountEmail, ref string privateKeyPem,
        ref string folderId, ref string fileName);

    /// <summary>client_email from the service account JSON key.</summary>
    public static string ServiceAccountEmail { get; }
    /// <summary>private_key from the JSON key (the whole "-----BEGIN PRIVATE KEY-----..." string).</summary>
    public static string PrivateKeyPem { get; }
    /// <summary>Id of the Drive folder you shared with the service account.</summary>
    public static string FolderId { get; }
    /// <summary>Backup file name inside that folder.</summary>
    public static string FileName { get; }

    static DriveSyncConfig()
    {
        string email = "your-sa@your-project.iam.gserviceaccount.com";
        string pem = "PASTE-PRIVATE-KEY";
        string folder = "PASTE-FOLDER-ID";
        string file = "productivityhub-backup.db3";
        LoadSecrets(ref email, ref pem, ref folder, ref file);
        ServiceAccountEmail = email;
        PrivateKeyPem = pem;
        FolderId = folder;
        FileName = string.IsNullOrWhiteSpace(file) ? "productivityhub-backup.db3" : file;
    }

    public static bool Configured =>
        ServiceAccountEmail.Contains("@") && !ServiceAccountEmail.StartsWith("your-sa@") &&
        PrivateKeyPem.Contains("BEGIN PRIVATE KEY") &&
        FolderId.Length > 5 && !FolderId.Contains("PASTE-");
}
