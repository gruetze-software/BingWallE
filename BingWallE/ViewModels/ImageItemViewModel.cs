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
    [ObservableProperty][NotifyPropertyChangedFor(nameof(SaveLabel))][NotifyPropertyChangedFor(nameof(IgnoreMenuLabel))] private bool _isIgnored;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(SaveLabel))] private bool _isSaved;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(SaveLabel))] private string? _downloadError;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(ResolutionLabel))] private string _downloadResolution = "1920x1080";
    [ObservableProperty][NotifyPropertyChangedFor(nameof(ResolutionLabel))] private string? _actualDimensions;
    public string SourceLabel => _text.Get("Quelle: {0}", Image.Source switch { WallpaperSource.WindowsSpotlight => _text.Get("Windows-Blickpunkt"), WallpaperSource.Local => _text.Get("Lokale Datei"), _ => "Bing" });
    public string ResolutionLabel => ActualDimensions != null ? _text.Get("Bild: {0} Pixel", ActualDimensions)
        : Image.Source == WallpaperSource.WindowsSpotlight ? _text.Get("Download: Originalauflösung") : DownloadResolution switch
    {
        "1366x768" => _text.Get("Download: HD · 1366 × 768"),
        "UHD" => _text.Get("Download: UHD · bildabhängige Größe"),
        _ => _text.Get("Download: Full HD · 1920 × 1080")
    };
    private readonly UiText _text = new();
    public void SetLanguage(string language)
    {
        _text.SetLanguage(language);
        RebuildKeywords();
        OnPropertyChanged(nameof(SourceLabel)); OnPropertyChanged(nameof(ResolutionLabel)); OnPropertyChanged(nameof(DisplayDate)); OnPropertyChanged(nameof(Copyright)); OnPropertyChanged(nameof(SaveLabel)); OnPropertyChanged(nameof(IgnoreMenuLabel));
    }
    private IReadOnlyList<ImageTag> _predictions = System.Array.Empty<ImageTag>();
    public string Description => !string.IsNullOrWhiteSpace(Image.Description) ? Image.Description : WallpaperCredits.Split(Image.Copyright).Description is { Length: > 0 } caption ? caption : Title;
    public string TextKeywords => string.Join(" · ", KeywordChips.Select(x => x.Label));
    public string ModelKeywords => string.Join(" · ", SuggestedKeywordChips.Select(x => x.Label));
    public bool HasTextKeywords => TextKeywords.Length > 0;
    public bool HasModelKeywords => ModelKeywords.Length > 0;
    public bool HasRecognition { get; private set; }
    public void ApplyPredictions(IReadOnlyList<ImageTag> predictions, bool recognized = true)
    {
        var before = TextKeywords + "|" + ModelKeywords;
        _predictions = predictions; HasRecognition = recognized;
        RebuildKeywords();
        if (before != TextKeywords + "|" + ModelKeywords) HasKeywordChanges = true;
    }
    public string? LocalPath { get; }
    public bool IsLocalImage => LocalPath != null;
    public Wallpaper Image { get; }
    public string Title => Image.Title;
    public string Date => Image.Date;
    public string DisplayDate => Image.Source == WallpaperSource.WindowsSpotlight && !IsLocalImage ? _text.Get("Aktuelle Blickpunkt-Auswahl") : System.DateTime.TryParseExact(Date, "yyyyMMdd", CultureInfo.InvariantCulture,
        DateTimeStyles.None, out var day) ? day.ToString(_text.Language == "en" ? "d MMMM yyyy" : "d. MMMM yyyy", _text.Culture) : Date;
    public string CopyrightCredit => WallpaperCredits.Split(Image.Copyright).Copyright;
    public bool HasCopyrightCredit => !string.IsNullOrWhiteSpace(CopyrightCredit);
    public string Copyright => string.IsNullOrWhiteSpace(Image.Copyright) ? _text.Get("Keine weiteren Bildinformationen verfügbar.") : Image.Copyright;
    public string IgnoreMenuLabel => _text.Get(IsIgnored ? "Wieder berücksichtigen" : "Ignorieren");
    public string SaveLabel => IsIgnored ? _text.Get("Ignoriert") : DownloadError != null ? _text.Get("Download fehlgeschlagen") : IsSaved ? _text.Get("Bereits gespeichert") : _text.Get("Neu in deiner Sammlung");
    public Bitmap? Preview { get; }

    public ImageItemViewModel(string fullImageUrl, string title, string date, Bitmap? preview = null)
        : this(new Wallpaper(fullImageUrl, title, date), preview) { }

    public ImageItemViewModel(Wallpaper image, Bitmap? preview = null, string? localPath = null)
    {
        Image = image;
        LocalPath = localPath;
        Preview = preview;
        RebuildKeywords();
    }
}
