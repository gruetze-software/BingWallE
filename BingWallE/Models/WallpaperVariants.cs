using System;

namespace BingWallE.Models;

public static class WallpaperVariants
{
    public static Wallpaper ForResolution(Wallpaper image, string resolution)
    {
        if (resolution is not ("1920x1080" or "1366x768" or "UHD"))
            throw new ArgumentException("Unbekannte Auflösung.", nameof(resolution));
        if (resolution == "1920x1080") return image;
        if (string.IsNullOrWhiteSpace(image.UrlBase))
            throw new InvalidOperationException("Bing bietet für dieses Bild keine alternative Auflösung an.");
        return image with
        {
            Url = new Uri(new Uri("https://bing.com"), image.UrlBase + "_" + resolution + ".jpg").AbsoluteUri,
            Resolution = resolution
        };
    }

    public static string FileName(Wallpaper image)
    {
        var name = WallpaperFileName.Create(image.Date, image.Title);
        return image.Resolution == "1920x1080" ? name : name[..^4] + "_" + image.Resolution + ".jpg";
    }
}
