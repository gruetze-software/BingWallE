namespace BingWallE.Models;

public static class AppInfo
{
    public const string Name = "BingWallE";
    public static string Version
    {
        get
        {
            var version = typeof(AppInfo).Assembly.GetName().Version;
            return version?.ToString(version.Build > 0 ? 3 : 2) ?? "1.2";
        }
    }
    public static string DisplayName => $"{Name} {Version}";
    public const string RepositoryUrl = "https://github.com/gruetze-software/BingWallE";
}
