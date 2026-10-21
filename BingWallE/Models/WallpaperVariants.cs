using System;

namespace BingWallE.Models;

public static class WallpaperVariants
{
    public static Wallpaper ForResolution(Wallpaper image, string resolution)
    {
        if (resolution is not ("1920x1080" or "1366x768" or "UHD"))
            throw new ArgumentException("Unbekannte Auflösung.", nameof(resolution));
        if (image.Source == WallpaperSource.WindowsSpotlight || resolution == "1920x1080") return image;
        if (string.IsNullOrWhiteSpace(image.UrlBase))
            throw new InvalidOperationException("Bing bietet für dieses Bild keine alternative Auflösung an.");
        return image with
        {
            Url = new Uri(new Uri("https://bing.com"), image.UrlBase + "_" + resolution + ".jpg").AbsoluteUri,
            Resolution = resolution
        };
    }

    public static string FileName(Wallpaper image) => FileName(image, false);
    internal static string LegacyFileName(Wallpaper image) => FileName(image, true);

    private static string FileName(Wallpaper image, bool legacy)
    {
        if (image.Source == WallpaperSource.WindowsSpotlight)
        {
            var identity = image.SourceId ?? new Uri(image.Url).GetLeftPart(UriPartial.Path);
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity)))[..16];
            return legacy ? WallpaperFileName.CreateLegacy("Spotlight_" + hash, image.Title)
                : WallpaperFileName.CreateWithIdentity("Spotlight_" + hash, image.Title);
        }
        var name = legacy ? WallpaperFileName.CreateLegacy(image.Date, image.Title) : WallpaperFileName.Create(image.Date, image.Title);
        return image.Resolution == "1920x1080" ? name : name[..^4] + "_" + image.Resolution + ".jpg";
    }
}
