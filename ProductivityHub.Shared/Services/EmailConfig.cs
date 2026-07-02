namespace ProductivityHub.Services;

/// <summary>
/// Email (IMAP) settings. Works the same way as SyncConfig: the REAL values live
/// in EmailConfig.Secrets.cs, which is gitignored so your mail credentials never
/// get committed. To enable the Email tab, copy EmailConfig.Secrets.cs.example
/// to EmailConfig.Secrets.cs and fill it in. If that file is absent, the tab
/// shows setup instructions and the app still runs.
///
/// Any IMAP provider works: Gmail (imap.gmail.com, app password required),
/// Outlook (outlook.office365.com), Fastmail, iCloud, or self-hosted.
/// </summary>
public static partial class EmailConfig
{
    static partial void LoadSecrets(
        ref string host, ref int port, ref bool useStartTls,
        ref string username, ref string password,
        ref string folder, ref int fetchCount);

    public static string Host { get; }
    public static int Port { get; }
    /// <summary>False (default) = implicit SSL on port 993. True = STARTTLS, typically port 143.</summary>
    public static bool UseStartTls { get; }
    public static string Username { get; }
    public static string Password { get; }
    /// <summary>Mailbox to read. "INBOX" on virtually every server.</summary>
    public static string Folder { get; }
    /// <summary>How many of the most recent messages to list.</summary>
    public static int FetchCount { get; }

    static EmailConfig()
    {
        string host = "imap.YOUR-PROVIDER.com";
        int port = 993;
        bool useStartTls = false;
        string username = "you@example.com";
        string password = "CHANGE-ME";
        string folder = "INBOX";
        int fetchCount = 25;
        LoadSecrets(ref host, ref port, ref useStartTls, ref username, ref password, ref folder, ref fetchCount);
        Host = host;
        Port = port;
        UseStartTls = useStartTls;
        Username = username;
        Password = password;
        Folder = string.IsNullOrWhiteSpace(folder) ? "INBOX" : folder;
        FetchCount = Math.Clamp(fetchCount, 1, 200);
    }

    public static bool Configured =>
        Host.Length > 0 && !Host.Contains("YOUR-PROVIDER") &&
        Username.Length > 0 && !Username.Equals("you@example.com", StringComparison.OrdinalIgnoreCase) &&
        Password.Length > 0 && !Password.Contains("CHANGE-ME");
}
