using System;
using System.IO;

namespace BingWallE.Models;

public class AppSettings
{
    public string Market { get; set; } = System.Globalization.CultureInfo.CurrentCulture.Name;
    public string Resolution { get; set; } = "1920x1080";
    public bool AutoTags { get; set; }
    public bool AutoRefreshOnStartup { get; set; }
    public string[] IgnoredImages { get; set; } = [];
    public SavedWindowState Window { get; set; } = new();
    public string Language { get; set; } = "de";
    public string Theme { get; set; } = "System";
    public string WallpaperPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "BingImages");
}
