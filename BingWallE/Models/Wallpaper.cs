using CommunityToolkit.Mvvm.ComponentModel;

namespace BingWallE.Models;

public record Wallpaper(string Url, string Title, string Date, string? UrlBase = null,
    string Copyright = "", string? CopyrightUrl = null, string Resolution = "1920x1080", string Market = "de-DE");
public record WallpaperPreview(Wallpaper Image, byte[] PreviewBytes);
public sealed class SelectionOption(string value, string label) : ObservableObject
{
    private string _label = label;
    public string Value { get; } = value;
    public string Label { get => _label; set => SetProperty(ref _label, value); }
    public override string ToString() => Label;
}
