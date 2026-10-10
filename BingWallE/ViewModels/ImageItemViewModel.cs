using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using BingWallE.Services;
using Avalonia.Media.Imaging;
using BingWallE.Models;
using BingWallE.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BingWallE.ViewModels;

public partial class ImageItemViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(SaveLabel))] private bool _isSaved;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(SaveLabel))] private string? _downloadError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(ResolutionLabel))] private string _downloadResolution = "1920x1080";
    [ObservableProperty][NotifyPropertyChangedFor(nameof(ResolutionLabel))] private string? _actualDimensions;
    public string ResolutionLabel => ActualDimensions != null ? _text.Get("Bild: {0} Pixel", ActualDimensions) : DownloadResolution switch
    {
        "1366x768" => _text.Get("Download: HD · 1366 × 768"),
        "UHD" => _text.Get("Download: UHD · bildabhängige Größe"),
        _ => _text.Get("Download: Full HD · 1920 × 1080")
    };
    private readonly UiText _text = new();
    public void SetLanguage(string language)
    {
        _text.SetLanguage(language);
        OnPropertyChanged(nameof(ResolutionLabel)); OnPropertyChanged(nameof(DisplayDate)); OnPropertyChanged(nameof(Copyright)); OnPropertyChanged(nameof(SaveLabel));
    }
    private IReadOnlyList<ImageTag> _predictions = System.Array.Empty<ImageTag>();
    public string Description => WallpaperCredits.Split(Image.Copyright).Description is { Length: > 0 } caption ? caption : Title;
    public string TextKeywords => string.Join(" · ", WallpaperKeywords.Build(Image, _predictions).Where(x => !x.FromModel).Select(x => x.Label));
    public string ModelKeywords => string.Join(" · ", WallpaperKeywords.Build(Image, _predictions).Where(x => x.FromModel).Select(x => x.Label));
    public bool HasTextKeywords => TextKeywords.Length > 0;
    public bool HasModelKeywords => ModelKeywords.Length > 0;
    public bool HasRecognition { get; private set; }
    public void ApplyPredictions(IReadOnlyList<ImageTag> predictions, bool recognized = true)
    {
        _predictions = predictions; HasRecognition = recognized;
        OnPropertyChanged(nameof(TextKeywords)); OnPropertyChanged(nameof(ModelKeywords));
        OnPropertyChanged(nameof(HasTextKeywords)); OnPropertyChanged(nameof(HasModelKeywords));
    }
    public Wallpaper Image { get; }
    public string Title => Image.Title;
    public string Date => Image.Date;
    public string DisplayDate => System.DateTime.TryParseExact(Date, "yyyyMMdd", CultureInfo.InvariantCulture,
        DateTimeStyles.None, out var day) ? day.ToString(_text.Language == "en" ? "d MMMM yyyy" : "d. MMMM yyyy", _text.Culture) : Date;
    public string CopyrightCredit => WallpaperCredits.Split(Image.Copyright).Copyright;
    public bool HasCopyrightCredit => !string.IsNullOrWhiteSpace(CopyrightCredit);
    public string Copyright => string.IsNullOrWhiteSpace(Image.Copyright) ? _text.Get("Keine weiteren Bildinformationen von Bing verfügbar.") : Image.Copyright;
    public string SaveLabel => DownloadError != null ? _text.Get("Download fehlgeschlagen") : IsSaved ? _text.Get("Bereits gespeichert") : _text.Get("Neu in deiner Sammlung");
    public Bitmap? Preview { get; }

    public ImageItemViewModel(string fullImageUrl, string title, string date, Bitmap? preview = null)
        : this(new Wallpaper(fullImageUrl, title, date), preview) { }

    public ImageItemViewModel(Wallpaper image, Bitmap? preview = null)
    {
        Image = image;
        Preview = preview;
    }
}
