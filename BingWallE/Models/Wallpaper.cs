namespace BingWallE.Models;

public record Wallpaper(string Url, string Title, string Date, string? UrlBase = null,
    string Copyright = "", string? CopyrightUrl = null, string Resolution = "1920x1080");
public record WallpaperPreview(Wallpaper Image, byte[] PreviewBytes);
public record SelectionOption(string Value, string Label)
{
    public override string ToString() => Label;
}
