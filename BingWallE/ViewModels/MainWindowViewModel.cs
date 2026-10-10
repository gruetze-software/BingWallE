using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BingWallE.Models;
using BingWallE.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BingWallE.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IBingWallpaperClient _bing;
    private readonly IWallpaperStore _images;
    private readonly ISettingsStore _settings;
    private readonly IAppDialogs _dialogs;
    private readonly HashSet<ImageItemViewModel> _observed = new();
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _previewCts;
    private string? _retryOperation;

    public string AppTitle => $"{AppInfo.DisplayName} · Bildersammlung";
    public string DisplayVersion => AppInfo.Version;
    public SelectionOption[] Markets { get; } =
    [
        new("de-DE", "Deutschland"), new("en-US", "USA"), new("en-GB", "Großbritannien"),
        new("fr-FR", "Frankreich"), new("it-IT", "Italien"), new("es-ES", "Spanien"),
        new("ja-JP", "Japan"), new("en-CA", "Kanada"), new("en-AU", "Australien"),
        new("en-IN", "Indien"), new("pt-BR", "Brasilien"), new("zh-CN", "China")
    ];
    public SelectionOption[] Resolutions { get; } =
    [new("1920x1080", "Full HD · 1920 × 1080"), new("1366x768", "HD · 1366 × 768"), new("UHD", "UHD · falls verfügbar")];
    public SelectionOption[] Themes { get; } = [new("System", "System"), new("Light", "Hell"), new("Dark", "Dunkel")];
    [ObservableProperty] private SelectionOption _selectedMarket;
    [ObservableProperty] private SelectionOption _selectedResolution;
    [ObservableProperty] private SelectionOption _selectedTheme;
    [ObservableProperty] private string? _targetFolder;
    public ObservableCollection<ImageItemViewModel> Wallpapers { get; } = new();
    [ObservableProperty] private string _statusMessage = "Wähle eine Region und entdecke die aktuellen Bing-Bilder.";
    [ObservableProperty] private IBrush _statusColor = Brushes.Gray;
    [ObservableProperty] private string _buttonText = "Aktualisieren";
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private bool _isIndeterminate;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(CanRetry))] private bool _hasError;
    [ObservableProperty] private string _gallerySubtitle = "Die letzten acht Tage, bereit zum Sammeln.";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(CanRetry))]
    [NotifyCanExecuteChangedFor(nameof(DownloadSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectNoneCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectNewCommand))]
    [NotifyCanExecuteChangedFor(nameof(RetryCommand))]
    private bool _isRunning;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsPreviewOpen))] private ImageItemViewModel? _previewItem;
    [ObservableProperty] private Bitmap? _largePreview;
    [ObservableProperty] private bool _isPreviewLoading;
    [ObservableProperty] private string _previewStatus = "";
    [ObservableProperty] private bool _previewFailed;
    public bool IsPreviewOpen => PreviewItem != null;
    public bool IsIdle => !IsRunning;
    public bool CanRetry => HasError && IsIdle && _retryOperation is "scan" or "download";
    public bool HasSelection => SelectedCount > 0;
    public bool IsEmpty => Wallpapers.Count == 0;
    public int SelectedCount => Wallpapers.Count(x => x.IsSelected);
    public string SelectionSummary => $"{SelectedCount} von {Wallpapers.Count} Bildern ausgewählt";
    public string DownloadLabel => SelectedCount == 1 ? "1 Bild herunterladen" : $"{SelectedCount} Bilder herunterladen";

    public MainWindowViewModel(IBingWallpaperClient bing, IWallpaperStore images, ISettingsStore settings, IAppDialogs dialogs)
    {
        _bing = bing; _images = images; _settings = settings; _dialogs = dialogs;
        var saved = settings.Load();
        _targetFolder = saved.WallpaperPath;
        _selectedMarket = Markets.FirstOrDefault(x => x.Value == saved.Market) ?? Markets[0];
        _selectedResolution = Resolutions.FirstOrDefault(x => x.Value == saved.Resolution) ?? Resolutions[0];
        _selectedTheme = Themes.FirstOrDefault(x => x.Value == saved.Theme) ?? Themes[0];
        _dialogs.ApplyTheme(_selectedTheme.Value);
        Wallpapers.CollectionChanged += (_, _) =>
        {
            foreach (var old in _observed.Where(x => !Wallpapers.Contains(x)).ToArray())
            { old.PropertyChanged -= ItemChanged; _observed.Remove(old); }
            foreach (var item in Wallpapers.Where(x => !_observed.Contains(x)))
            { item.PropertyChanged += ItemChanged; _observed.Add(item); }
            NotifySelection();
            OnPropertyChanged(nameof(IsEmpty));
        };
    }

    private void ItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImageItemViewModel.IsSelected)) NotifySelection();
    }
    private void NotifySelection()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectedCount)); OnPropertyChanged(nameof(SelectionSummary)); OnPropertyChanged(nameof(DownloadLabel));
    }
    private bool CanStartOperation() => IsIdle;
    private CancellationTokenSource BeginOperation(string message, bool indeterminate)
    {
        Trace.WriteLine($"[{DateTimeOffset.Now:O}] Operation START: {message}");
        _cts = new CancellationTokenSource();
        HasError = false; IsRunning = true; ButtonText = "Abbrechen";
        ProgressValue = 0; IsIndeterminate = indeterminate;
        StatusMessage = message; StatusColor = Brushes.Gray;
        return _cts;
    }
    private void EndOperation()
    {
        Trace.WriteLine($"[{DateTimeOffset.Now:O}] Operation END: {StatusMessage}");
        _cts = null; IsRunning = false; IsIndeterminate = false; ButtonText = "Aktualisieren";
    }
    private string ErrorMessage(Exception ex) => ex switch
    {
        OperationCanceledException => "Zeitüberschreitung. Bitte erneut versuchen.",
        HttpRequestException { StatusCode: HttpStatusCode.NotFound } => "Diese Bildauflösung ist bei Bing nicht verfügbar. Bitte Full HD wählen.",
        _ => $"Fehler: {ex.Message}"
    };
    private void ReportError(Exception ex)
    {
        Trace.WriteLine($"[{DateTimeOffset.Now:O}] Operation FAILED: {ex}");
        StatusMessage = ErrorMessage(ex); StatusColor = Brushes.IndianRed; HasError = true;
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task StartStop()
    {
        if (IsRunning) { Trace.WriteLine("User requested cancellation."); _cts?.Cancel(); return; }
        ClosePreview();
        _retryOperation = "scan";
        using var cancellation = BeginOperation("Bilder auf Bing suchen …", true);
        try
        {
            ClearGallery();
            var market = SelectedMarket;
            await foreach (var preview in _bing.ScanAsync(market.Value, cancellation.Token, message => StatusMessage = message))
            {
                cancellation.Token.ThrowIfCancellationRequested();
                using var stream = new MemoryStream(preview.PreviewBytes);
                var item = new ImageItemViewModel(preview.Image, new Bitmap(stream));
                RefreshSaved(item);
                Wallpapers.Add(item);
                ProgressValue = Wallpapers.Count / 8d * 100;
            }
            cancellation.Token.ThrowIfCancellationRequested();
            GallerySubtitle = $"{market.Label} · {Wallpapers.Count} Bilder · aktuelle Bing-Auswahl";
            StatusMessage = IsEmpty ? "Bing hat für diese Region keine Bilder geliefert." : "Suche abgeschlossen. Bilder zum Download auswählen.";
            ProgressValue = 100; _retryOperation = null;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        { StatusMessage = "Suche abgebrochen. Bereits geladene Bilder bleiben verfügbar."; }
        catch (Exception ex) { ReportError(ex); }
        finally { EndOperation(); }
    }

    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private async Task DownloadSelected()
    {
        if (IsRunning) return;
        var selected = Wallpapers.Where(w => w.IsSelected).ToList();
        if (selected.Count == 0) { StatusMessage = "Bitte zuerst Bilder auswählen."; return; }
        var folder = TargetFolder ?? "";
        var resolution = SelectedResolution.Value;
        var completed = 0;
        _retryOperation = "download";
        using var cancellation = BeginOperation($"{selected.Count} Bilder herunterladen …", false);
        try
        {
            foreach (var item in selected)
            {
                item.DownloadError = null;
                StatusMessage = $"Bild {completed + 1} von {selected.Count}: {item.Title}";
                Trace.WriteLine($"Download START {completed + 1}/{selected.Count}, resolution={resolution}");
                try { await _images.DownloadAsync(folder, WallpaperVariants.ForResolution(item.Image, resolution), cancellation.Token); }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { throw; }
                catch (Exception ex) { item.DownloadError = ErrorMessage(ex); throw; }
                item.IsSaved = true; item.IsSelected = false;
                completed++; ProgressValue = 100d * completed / selected.Count;
            }
            StatusMessage = $"Download abgeschlossen: {completed} Bilder gespeichert.";
            _retryOperation = null;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        { StatusMessage = $"Download abgebrochen: {completed} von {selected.Count} Bildern gespeichert."; }
        catch (Exception ex) { ReportError(ex); StatusMessage += $" ({completed} von {selected.Count} Bildern gespeichert.)"; }
        finally { EndOperation(); }
    }

    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private async Task Retry()
    {
        if (IsRunning) return;
        if (_retryOperation == "download") await DownloadSelected();
        else if (_retryOperation == "scan") await StartStop();
        else HasError = false;
    }
    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private void SelectAll() { foreach (var item in Wallpapers) item.IsSelected = true; }
    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private void SelectNone() { foreach (var item in Wallpapers) item.IsSelected = false; }
    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private void SelectNew() { foreach (var item in Wallpapers) { RefreshSaved(item); item.IsSelected = !item.IsSaved; } }

    private void RefreshSaved(ImageItemViewModel item)
    {
        // Unavailable alternative resolutions remain selectable, so download can explain the limitation.
        try { item.IsSaved = _images.Exists(TargetFolder ?? "", WallpaperVariants.ForResolution(item.Image, SelectedResolution.Value)); }
        catch (InvalidOperationException) { item.IsSaved = false; }
        item.IsSelected = !item.IsSaved; item.DownloadError = null;
    }
    private void ClearGallery()
    {
        foreach (var item in Wallpapers) item.Preview?.Dispose();
        Wallpapers.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private async Task SelectFolder()
    {
        if (IsRunning) return;
        try { var folder = await _dialogs.SelectFolderAsync(); if (folder != null && IsIdle) TargetFolder = folder; }
        catch (Exception ex) { _retryOperation = null; ReportError(ex); }
    }
    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private async Task OpenFolder()
    {
        try { await _dialogs.OpenFolderAsync(TargetFolder ?? ""); }
        catch (Exception ex) { _retryOperation = null; ReportError(ex); }
    }
    [RelayCommand]
    private async Task ShowInfo()
    {
        try { await _dialogs.ShowInfoAsync(AppTitle); }
        catch (Exception ex) { ReportError(ex); }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task OpenPreview(ImageItemViewModel? item)
    {
        if (item == null || IsRunning) return;
        ClosePreview();
        PreviewItem = item; PreviewFailed = false; PreviewStatus = "Große Vorschau wird geladen …";
        using var cancellation = new CancellationTokenSource();
        _previewCts = cancellation; IsPreviewLoading = true;
        try
        {
            var bytes = await _bing.LoadPreviewAsync(item.Image, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            using var stream = new MemoryStream(bytes);
            LargePreview = new Bitmap(stream); PreviewStatus = "";
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (!cancellation.IsCancellationRequested)
            { Trace.WriteLine(ex); PreviewStatus = ErrorMessage(ex); PreviewFailed = true; }
        }
        finally
        {
            if (ReferenceEquals(_previewCts, cancellation)) { IsPreviewLoading = false; _previewCts = null; }
        }
    }
    [RelayCommand]
    private Task RetryPreview() => OpenPreview(PreviewItem);
    [RelayCommand]
    private void ClosePreview()
    {
        _previewCts?.Cancel(); _previewCts = null;
        var bitmap = LargePreview; LargePreview = null; bitmap?.Dispose();
        PreviewItem = null; IsPreviewLoading = false; PreviewFailed = false;
    }
    public void Shutdown()
    {
        _cts?.Cancel(); ClosePreview(); ClearGallery();
    }

    private void SavePreferences()
    {
        try
        {
            _settings.Save(new AppSettings
            {
                WallpaperPath = TargetFolder ?? new AppSettings().WallpaperPath,
                Market = SelectedMarket.Value,
                Resolution = SelectedResolution.Value,
                Theme = SelectedTheme.Value
            });
        }
        catch (Exception ex) { _retryOperation = null; ReportError(ex); }
    }
    partial void OnTargetFolderChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        SavePreferences();
        try { foreach (var item in Wallpapers) RefreshSaved(item); }
        catch (Exception ex) { ReportError(ex); }
    }
    partial void OnSelectedMarketChanged(SelectionOption value)
    {
        ClosePreview(); ClearGallery();
        GallerySubtitle = value.Label + " · bereit zum Entdecken";
        StatusMessage = "Region geändert. Mit Aktualisieren neue Bilder laden.";
        HasError = false; _retryOperation = null; SavePreferences();
    }
    partial void OnSelectedResolutionChanged(SelectionOption value)
    {
        foreach (var item in Wallpapers) RefreshSaved(item);
        HasError = false; _retryOperation = null; SavePreferences();
        StatusMessage = "Auflösung geändert. Vorhandene Dateien wurden erneut geprüft.";
    }
    partial void OnSelectedThemeChanged(SelectionOption value) { _dialogs.ApplyTheme(value.Value); SavePreferences(); }
}
