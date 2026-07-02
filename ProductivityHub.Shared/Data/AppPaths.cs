namespace ProductivityHub.Data;

/// <summary>
/// Host-agnostic location for the on-device SQLite file. Each host (MAUI / Web)
/// sets <see cref="DataDirectory"/> at startup, since they store data in
/// different places. Keeping this out of the shared code means the RCL has no
/// platform dependencies.
/// </summary>
public static class AppPaths
{
    public static string DataDirectory { get; set; } = "";
    public const string DbFileName = "productivityhub.db3";
    public static string DbPath => Path.Combine(DataDirectory, DbFileName);
}
