using System.Globalization;
using Avalonia.Media.Imaging;
using BingWallE.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BingWallE.ViewModels;

public partial class ImageItemViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(SaveLabel))] private bool _isSaved;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(SaveLabel))] private string? _downloadError;
    public Wallpaper Image { get; }
    public string Title => Image.Title;
    public string Date => Image.Date;
    public string DisplayDate => System.DateTime.TryParseExact(Date, "yyyyMMdd", CultureInfo.InvariantCulture,
        DateTimeStyles.None, out var day) ? day.ToString("d. MMMM yyyy", CultureInfo.GetCultureInfo("de-DE")) : Date;
    public string Copyright => string.IsNullOrWhiteSpace(Image.Copyright) ? "Keine weiteren Bildinformationen von Bing verfügbar." : Image.Copyright;
    public string SaveLabel => DownloadError != null ? "Download fehlgeschlagen" : IsSaved ? "Bereits gespeichert" : "Neu in deiner Sammlung";
    public Bitmap? Preview { get; }

    public ImageItemViewModel(string fullImageUrl, string title, string date, Bitmap? preview = null)
        : this(new Wallpaper(fullImageUrl, title, date), preview) { }

    public ImageItemViewModel(Wallpaper image, Bitmap? preview = null)
    {
        Image = image;
        Preview = preview;
    }
}
