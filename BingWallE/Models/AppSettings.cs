using System;
using System.IO;

namespace BingWallE.Models;

public class AppSettings
{
    public string Market { get; set; } = System.Globalization.CultureInfo.CurrentCulture.Name;
    public string Resolution { get; set; } = "1920x1080";
    public string Language { get; set; } = "de";
    public string Theme { get; set; } = "System";
    public string WallpaperPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "BingImages");
}
