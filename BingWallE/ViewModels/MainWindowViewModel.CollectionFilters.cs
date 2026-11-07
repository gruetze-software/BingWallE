using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using BingWallE.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BingWallE.ViewModels;

public partial class MainWindowViewModel
{
    public ObservableCollection<ImageItemViewModel> VisibleWallpapers { get; } = new();
    [ObservableProperty] private string _collectionSearch = "";
    [ObservableProperty] private int _collectionSourceIndex;
    [ObservableProperty] private int _collectionResolutionIndex;
    [ObservableProperty] private int _collectionDateIndex;
    public SelectionOption[] CollectionSources { get; } =
        [new("all", "Alle Quellen"), new("bing", "Bing"), new("spotlight", "Windows-Blickpunkt"), new("local", "Lokale Datei")];
    public SelectionOption[] CollectionResolutions { get; } =
        [new("all", "Alle Auflösungen"), new("hd", "Mindestens HD"), new("fhd", "Mindestens Full HD"), new("uhd", "Mindestens UHD")];
    public SelectionOption[] CollectionDates { get; } =
        [new("all", "Gesamter Zeitraum"), new("7", "Letzte 7 Tage"), new("30", "Letzte 30 Tage"), new("365", "Letztes Jahr")];
    private bool HasCollectionFilters => !string.IsNullOrWhiteSpace(CollectionSearch)
        || CollectionSourceIndex != 0 || CollectionResolutionIndex != 0 || CollectionDateIndex != 0;

    partial void OnCollectionSearchChanged(string value) => ApplyCollectionFilters();
    partial void OnCollectionSourceIndexChanged(int value) => ApplyCollectionFilters();
    partial void OnCollectionResolutionIndexChanged(int value) => ApplyCollectionFilters();
    partial void OnCollectionDateIndexChanged(int value) => ApplyCollectionFilters();

    [RelayCommand]
    private void ResetCollectionFilters()
    {
        CollectionSearch = "";
        CollectionSourceIndex = CollectionResolutionIndex = CollectionDateIndex = 0;
    }

    private void ApplyCollectionFilters()
    {
        var terms = CollectionSearch.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var today = DateTimeOffset.Now.Date;
        IEnumerable<ImageItemViewModel> items = Wallpapers;
        if (IsCollectionView)
            items = items.Where(item => MatchesCollectionFilters(item, terms, today));
        var matches = items.ToArray();
        // Keep thumbnails and the complete collection alive when hiding results.
        if (!VisibleWallpapers.SequenceEqual(matches))
        {
            VisibleWallpapers.Clear();
            foreach (var item in matches) VisibleWallpapers.Add(item);
        }
        if (IsCollectionView) GallerySubtitle = CollectionSubtitle();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyHeading)); OnPropertyChanged(nameof(EmptyHelp));
    }

    private bool MatchesCollectionFilters(ImageItemViewModel item, string[] terms, DateTime today)
    {
        if (CollectionSourceIndex switch
        {
            1 => item.Image.Source != WallpaperSource.Bing,
            2 => item.Image.Source != WallpaperSource.WindowsSpotlight,
            3 => item.Image.Source != WallpaperSource.Local,
            _ => false
        }) return false;
        var minimum = CollectionResolutionIndex switch { 1 => (1280, 720), 2 => (1920, 1080), 3 => (3840, 2160), _ => (0, 0) };
        if (Math.Max(item.PixelWidth, item.PixelHeight) < minimum.Item1
            || Math.Min(item.PixelWidth, item.PixelHeight) < minimum.Item2) return false;
        var days = CollectionDateIndex switch { 1 => 7, 2 => 30, 3 => 365, _ => 0 };
        if (days > 0 && (item.DownloadedAt is not { } date
            || date.LocalDateTime.Date < today.AddDays(1 - days) || date.LocalDateTime.Date > today)) return false;
        if (terms.Length == 0) return true;
        var text = string.Join(" ", item.Title, item.Description, item.CopyrightCredit,
            item.LocalPath == null ? "" : Path.GetFileName(item.LocalPath), item.TextKeywords, item.ModelKeywords);
        return terms.All(term => CultureInfo.InvariantCulture.CompareInfo.IndexOf(text, term,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0);
    }
}
