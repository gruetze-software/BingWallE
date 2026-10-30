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
using BingWallE.Localization;
using BingWallE.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BingWallE.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IBingWallpaperClient _bing;
    private readonly IBingWallpaperClient? _spotlight;
    private readonly IWallpaperStore _images;
    private readonly ISettingsStore _settings;
    private readonly IAppDialogs _dialogs;
    private readonly IImageTagger? _tagger;
    private readonly HashSet<ImageItemViewModel> _observed = new();
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _previewCts;
    private string? _retryOperation;
    private readonly HashSet<string> _ignoredImages;
    private bool _startupInitialized;
    private bool _localActionRunning;
    public SavedWindowState WindowSettings { get; private set; }
    public SavedWindowState PreviewWindowSettings { get; private set; }
    [ObservableProperty] private bool _autoRefreshOnStartup;
    [ObservableProperty] private bool _includeWindowsSpotlight;
    private static string IgnoreKey(ImageItemViewModel item) => item.LocalPath != null ? "Local|" + item.LocalPath : item.Image.Source == WallpaperSource.WindowsSpotlight
        ? "WindowsSpotlight|" + item.Image.Market + "|" + (item.Image.SourceId ?? item.Image.Url)
        : item.Image.Market + "|" + (item.Image.UrlBase ?? item.Date + "|" + item.Title);

    public async Task InitializeAsync()
    {
        if (_startupInitialized) return;
        _startupInitialized = true;
        if (AutoRefreshOnStartup && IsIdle) await StartStop();
    }
    public void SaveWindowState(SavedWindowState state) { WindowSettings = state; SavePreferences(); }
    public void SavePreviewWindowState(SavedWindowState state) { PreviewWindowSettings = state; SavePreferences(); }


    public UiText Text { get; } = new();
    private string T(string text, params object[] args) => Text.Get(text, args);
    public string AppTitle => $"{AppInfo.DisplayName} · {T("Bildersammlung")}";
    public SelectionOption[] Languages { get; } = [new("de", "Deutsch"), new("en", "English")];
    [ObservableProperty] private SelectionOption _selectedLanguage;
    [ObservableProperty] private bool _autoTags;
    public string DisplayVersion => AppInfo.Version;
    public SelectionOption[] Markets { get; private set; } =
    [
        new("de-DE", "Deutschland"), new("en-US", "USA"), new("en-GB", "Großbritannien"),
        new("fr-FR", "Frankreich"), new("it-IT", "Italien"), new("es-ES", "Spanien"),
        new("ja-JP", "Japan"), new("en-CA", "Kanada"), new("en-AU", "Australien"),
        new("en-IN", "Indien"), new("pt-BR", "Brasilien"), new("zh-CN", "China")
    ];
    public SelectionOption[] Resolutions { get; private set; } =
    [new("1920x1080", "Full HD · 1920 × 1080"), new("1366x768", "HD · 1366 × 768"), new("UHD", "UHD · falls verfügbar")];
    public SelectionOption[] Themes { get; private set; } = [new("System", "System"), new("Light", "Hell"), new("Dark", "Dunkel")];
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
    [NotifyCanExecuteChangedFor(nameof(ShowDiscoverCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowCollectionCommand))]
    private bool _isRunning;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsPreviewOpen))] private ImageItemViewModel? _previewItem;
    [ObservableProperty] private Bitmap? _largePreview;
    [ObservableProperty] private bool _isPreviewLoading;
    [ObservableProperty] private string _previewStatus = "";
    [ObservableProperty] private bool _previewFailed;
    public bool IsPreviewOpen => PreviewItem != null;
    public bool IsIdle => !IsRunning;
    public bool CanRetry => HasError && IsIdle && _retryOperation is "scan" or "download" or "collection";
    public bool HasSelection => SelectedCount > 0;
    public bool IsEmpty => Wallpapers.Count == 0;
    public int SelectedCount => Wallpapers.Count(x => x.IsSelected && !x.IsIgnored);
    public string SelectionSummary => T("{0} von {1} Bildern ausgewählt", SelectedCount, Wallpapers.Count);
    public string DownloadLabel => SelectedCount == 1 ? T("1 Bild herunterladen") : T("{0} Bilder herunterladen", SelectedCount);

    public MainWindowViewModel(IBingWallpaperClient bing, IWallpaperStore images, ISettingsStore settings, IAppDialogs dialogs, IImageTagger? tagger = null, IBingWallpaperClient? spotlight = null)
    {
        _bing = bing; _spotlight = spotlight; _images = images; _settings = settings; _dialogs = dialogs; _tagger = tagger;
        var saved = settings.Load();
        _ignoredImages = new(saved.IgnoredImages ?? Array.Empty<string>(), StringComparer.Ordinal);
        WindowSettings = saved.Window ?? new();
        PreviewWindowSettings = saved.PreviewWindow ?? new(980, 850);
        _autoRefreshOnStartup = saved.AutoRefreshOnStartup;
        _includeWindowsSpotlight = saved.IncludeWindowsSpotlight;
        Text.SetLanguage(saved.Language);
        _selectedLanguage = Languages.First(x => x.Value == Text.Language);
        TranslateOptions();
        Text.ApplyResources();
        _statusMessage = T(_statusMessage); _buttonText = T(_buttonText); _gallerySubtitle = T(_gallerySubtitle);
        _autoTags = saved.AutoTags;
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
            { item.IsIgnored = _ignoredImages.Contains(IgnoreKey(item)); if (item.IsIgnored) item.IsSelected = false; item.SetLanguage(Text.Language); item.DownloadResolution = item.IsLocalImage || item.Image.Source == WallpaperSource.WindowsSpotlight ? "Original" : SelectedResolution.Value; item.PropertyChanged += ItemChanged; _observed.Add(item); }
            NotifySelection();
            OnPropertyChanged(nameof(IsEmpty));
        };
    }

    private void ItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ImageItemViewModel.IsSelected) or nameof(ImageItemViewModel.IsIgnored)) NotifySelection();
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
        HasError = false; IsRunning = true; ButtonText = T("Abbrechen");
        ProgressValue = 0; IsIndeterminate = indeterminate;
        StatusMessage = message; StatusColor = Brushes.Gray;
        return _cts;
    }
    private void EndOperation()
    {
        Trace.WriteLine($"[{DateTimeOffset.Now:O}] Operation END: {StatusMessage}");
        _cts = null; IsRunning = false; IsIndeterminate = false; ButtonText = T("Aktualisieren");
    }
    private string ErrorMessage(Exception ex) => ex switch
    {
        OperationCanceledException => T("Zeitüberschreitung. Bitte erneut versuchen."),
        HttpRequestException { StatusCode: HttpStatusCode.NotFound } => T("Diese Bildauflösung ist bei Bing nicht verfügbar. Bitte Full HD wählen."),
        _ => T("Fehler: {0}", FriendlyError(ex))
    };
    private string FriendlyError(Exception ex)
    {
        var translated = Text.Translate(ex.Message);
        if (UiText.Entries.Any(x => x.De == ex.Message || x.En == ex.Message)) return translated;
        return ex switch
        {
            UnauthorizedAccessException => T("Zugriff verweigert. Bitte die Ordnerberechtigungen prüfen."),
            HttpRequestException => T("Netzwerkfehler. Bitte die Verbindung prüfen und erneut versuchen."),
            IOException => T("Dateifehler. Bitte Speicherort und freien Speicherplatz prüfen."),
            _ => T("Die Aktion ist fehlgeschlagen. Weitere Informationen stehen in der Diagnoseausgabe.")
        };
    }
    private void ReportError(Exception ex)
    {
        Trace.WriteLine($"[{DateTimeOffset.Now:O}] Operation FAILED: {ex}");
        StatusMessage = ErrorMessage(ex); StatusColor = Brushes.IndianRed; HasError = true;
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task StartStop()
    {
        if (IsRunning) { Trace.WriteLine("User requested cancellation."); _cts?.Cancel(); return; }
        if (IsCollectionView) { await ReloadCollection(); return; }
        ClosePreview();
        _retryOperation = "scan";
        using var cancellation = BeginOperation(T("Bilder aus den gewählten Quellen suchen …"), true);
        try
        {
            ClearGallery();
            var market = SelectedMarket;
            var failures = new List<string>();
            var includeSpotlight = IncludeWindowsSpotlight;
            async Task LoadSource(IBingWallpaperClient source, string name)
            {
                try
                {
                    await foreach (var preview in source.ScanAsync(market.Value, cancellation.Token, message => StatusMessage = Text.Progress(message)))
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        using var stream = new MemoryStream(preview.PreviewBytes);
                        var item = new ImageItemViewModel(preview.Image, preview.Image.Source == WallpaperSource.WindowsSpotlight
                            ? Bitmap.DecodeToWidth(stream, 640) : new Bitmap(stream));
                        RefreshSaved(item);
                        Wallpapers.Add(item);
                    }
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{name} scan failed: {ex}");
                    failures.Add(T("{0}: {1}", name, ErrorMessage(ex)));
                }
            }
            await LoadSource(_bing, "Bing");
            if (includeSpotlight && _spotlight != null) await LoadSource(_spotlight, T("Windows-Blickpunkt"));
            cancellation.Token.ThrowIfCancellationRequested();
            UpdateGallerySubtitle();
            if (failures.Count > 0)
            {
                StatusMessage = Text.Join(T("Einige Bildquellen konnten nicht geladen werden. "), string.Join(" ", failures));
                HasError = true; StatusColor = Brushes.IndianRed; ProgressValue = 100;
                return;
            }
            StatusMessage = IsEmpty ? T("Keine Bilder für diese Region verfügbar.") : T("Suche abgeschlossen. Bilder zum Download auswählen.");
            ProgressValue = 100; _retryOperation = null;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        { StatusMessage = T("Suche abgebrochen. Bereits geladene Bilder bleiben verfügbar."); }
        catch (Exception ex) { ReportError(ex); }
        finally { EndOperation(); }
    }

    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private async Task DownloadSelected()
    {
        if (IsRunning || IsCollectionView) return;
        var selected = Wallpapers.Where(w => w.IsSelected && !w.IsIgnored).ToList();
        if (selected.Count == 0) { StatusMessage = T("Bitte zuerst Bilder auswählen."); return; }
        var folder = TargetFolder ?? "";
        var resolution = SelectedResolution.Value;
        var completed = 0;
        _retryOperation = "download";
        using var cancellation = BeginOperation(T("{0} Bilder herunterladen …", selected.Count), false);
        try
        {
            foreach (var item in selected)
            {
                item.DownloadError = null;
                StatusMessage = T("Bild {0} von {1}: {2}", completed + 1, selected.Count, item.Title);
                Trace.WriteLine($"Download START {completed + 1}/{selected.Count}, resolution={resolution}");
                try
                {
                    var result = await _images.DownloadAsync(folder, WallpaperVariants.ForResolution(item.Image, resolution), cancellation.Token,
                        AutoTags, message => StatusMessage = Text.Translate(message), item.HasKeywordChanges || item.KeywordsLoaded ? item.ExportKeywords() : null);
                    item.ActualDimensions = $"{result.Width} × {result.Height}";
                    if (result.Keywords != null) item.LoadKeywords(result.Keywords, force: true);
                    else if (AutoTags && !item.HasKeywordChanges) item.ApplyPredictions(result.Tags);
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { throw; }
                catch (Exception ex) { item.DownloadError = ErrorMessage(ex); throw; }
                item.IsSaved = true; item.IsSelected = false;
                completed++; ProgressValue = 100d * completed / selected.Count;
            }
            StatusMessage = T("Download abgeschlossen: {0} Bilder gespeichert.", completed);
            _retryOperation = null;
            InvalidateCollection();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        { InvalidateCollection(); StatusMessage = T("Download abgebrochen: {0} von {1} Bildern gespeichert.", completed, selected.Count); }
        catch (Exception ex) { InvalidateCollection(); ReportError(ex); StatusMessage = Text.Join(StatusMessage, T(" ({0} von {1} Bildern gespeichert.)", completed, selected.Count)); }
        finally { EndOperation(); }
    }

    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private async Task Retry()
    {
        if (IsRunning) return;
        if (_retryOperation == "download") await DownloadSelected();
        else if (_retryOperation == "scan") await StartStop();
        else if (_retryOperation == "collection") await ReloadCollection();
        else HasError = false;
    }
    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private void SelectAll() { foreach (var item in Wallpapers) item.IsSelected = !item.IsIgnored; }
    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private void SelectNone() { foreach (var item in Wallpapers) item.IsSelected = false; }
    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private void SelectNew() { foreach (var item in Wallpapers) { RefreshSaved(item); item.IsSelected = !item.IsSaved && !item.IsIgnored; } }

    private void RefreshSaved(ImageItemViewModel item)
    {
        if (item.LocalPath is { } path)
        {
            item.IsSaved = File.Exists(path); item.IsSelected = false; item.DownloadError = null;
            return;
        }
        var resolution = item.Image.Source == WallpaperSource.WindowsSpotlight ? "Original" : SelectedResolution.Value;
        if (item.DownloadResolution != resolution) item.ActualDimensions = null;
        item.DownloadResolution = resolution;
        // Unavailable alternative resolutions remain selectable, so download can explain the limitation.
        try { item.IsSaved = _images.Exists(TargetFolder ?? "", WallpaperVariants.ForResolution(item.Image, SelectedResolution.Value)); }
        catch (InvalidOperationException) { item.IsSaved = false; }
        item.IsSelected = !item.IsSaved && !item.IsIgnored; item.DownloadError = null;
    }
    [RelayCommand]
    private void ToggleIgnore(ImageItemViewModel? item)
    {
        if (item == null || !IsIdle || _localActionRunning) return;
        item.IsIgnored = !item.IsIgnored;
        if (item.IsIgnored) _ignoredImages.Add(IgnoreKey(item)); else _ignoredImages.Remove(IgnoreKey(item));
        RefreshSaved(item); SavePreferences();
    }

    [RelayCommand]
    private async Task OpenImagePath(ImageItemViewModel? item)
    {
        if (item == null || !IsIdle || _localActionRunning) return;
        try
        {
            var path = item.LocalPath ?? _images.GetPath(TargetFolder ?? "", WallpaperVariants.ForResolution(item.Image, SelectedResolution.Value));
            await _dialogs.ShowFileAsync(path);
        }
        catch (Exception ex) { _retryOperation = null; ReportError(ex); }
    }

    [RelayCommand]
    private async Task DeleteLocal(ImageItemViewModel? item)
    {
        if (item == null || !IsIdle || _localActionRunning) return;
        _localActionRunning = true;
        try
        {
            var folder = TargetFolder ?? "";
            var image = item.IsLocalImage ? item.Image : WallpaperVariants.ForResolution(item.Image, SelectedResolution.Value);
            var path = item.LocalPath ?? _images.GetPath(folder, image);
            if (item.IsLocalImage ? !File.Exists(path) : !_images.Exists(folder, image)) { RefreshSaved(item); return; }
            if (!await _dialogs.ConfirmDeleteAsync(path)) return;
            if (!IsIdle) return;
            if (item.IsLocalImage)
            {
                File.Delete(path);
                Wallpapers.Remove(item); _collectionCache.Remove(item); item.Preview?.Dispose();
                GallerySubtitle = CollectionSubtitle();
            }
            else
            {
                _images.Delete(folder, image);
                item.ActualDimensions = null; RefreshSaved(item);
                InvalidateCollection();
            }
            StatusMessage = T("Lokale Datei gelöscht: {0}", item.Title);
        }
        catch (Exception ex) { _retryOperation = null; ReportError(ex); }
        finally { _localActionRunning = false; }
    }

    private void ClearGallery()
    {
        foreach (var item in Wallpapers) item.Preview?.Dispose();
        Wallpapers.Clear();
        (IsCollectionView ? _collectionCache : _discoverCache).Clear();
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
        PreviewItem = item; PreviewFailed = false; PreviewStatus = T("Große Vorschau wird geladen …");
        using var cancellation = new CancellationTokenSource();
        _previewCts = cancellation; IsPreviewLoading = true;
        try
        {
            var path = SavedPath(item);
            var bytes = path != null && (item.IsLocalImage || File.Exists(path))
                ? await File.ReadAllBytesAsync(path, cancellation.Token)
                : await _bing.LoadPreviewAsync(item.Image, cancellation.Token);
            if (path != null && File.Exists(path))
            {
                item.LoadKeywords(await Task.Run(() => KeywordMetadata.Read(bytes), cancellation.Token));
                cancellation.Token.ThrowIfCancellationRequested();
                _previewFilePath = path;
            }
            cancellation.Token.ThrowIfCancellationRequested();
            _previewBytes = bytes; _previewIsJpeg = KeywordMetadata.IsJpeg(bytes);
            NotifyKeywordActions();
            cancellation.Token.ThrowIfCancellationRequested();
            using var stream = new MemoryStream(bytes);
            LargePreview = new Bitmap(stream); PreviewStatus = "";
            if (AutoTags && _tagger != null && !item.HasRecognition)
            {
                PreviewStatus = T("Bildmotive werden lokal erkannt …");
                var predictions = await _tagger.RecognizeAsync(bytes, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                item.ApplyPredictions(predictions);
                PreviewStatus = predictions.Count == 0 ? T("Keine sicheren zusätzlichen Bildmotive erkannt.") : "";
            }
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
        _keywordCts?.Cancel(); _keywordCts = null; IsKeywordOperationRunning = false;
        _previewBytes = null; _previewFilePath = null; _previewIsJpeg = false;
        var bitmap = LargePreview; LargePreview = null; bitmap?.Dispose();
        PreviewItem = null; IsPreviewLoading = false; PreviewFailed = false;
    }
    public void Shutdown()
    {
        _cts?.Cancel(); ClosePreview();
        foreach (var item in AllGalleryItems()) item.Preview?.Dispose();
        Wallpapers.Clear(); _discoverCache.Clear(); _collectionCache.Clear();
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
                Theme = SelectedTheme.Value,
                Language = SelectedLanguage.Value,
                AutoTags = AutoTags,
                AutoRefreshOnStartup = AutoRefreshOnStartup,
                IncludeWindowsSpotlight = IncludeWindowsSpotlight,
                IgnoredImages = _ignoredImages.ToArray(),
                PreviewWindow = PreviewWindowSettings,
                Window = WindowSettings
            });
        }
        catch (Exception ex) { _retryOperation = null; ReportError(ex); }
    }
    partial void OnTargetFolderChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        SavePreferences();
        InvalidateCollection();
        try { foreach (var item in _discoverCache.Concat(IsDiscoverView ? Wallpapers : Enumerable.Empty<ImageItemViewModel>()).Distinct()) RefreshSaved(item); }
        catch (Exception ex) { ReportError(ex); }
    }
    partial void OnSelectedMarketChanged(SelectionOption value)
    {
        ClosePreview();
        if (IsCollectionView) { foreach (var item in _discoverCache) item.Preview?.Dispose(); _discoverCache.Clear(); _discoverState = null; SavePreferences(); return; }
        ClearGallery();
        GallerySubtitle = T("{0} · bereit zum Entdecken", value.Label);
        StatusMessage = T("Region geändert. Mit Aktualisieren neue Bilder laden.");
        HasError = false; _retryOperation = null; SavePreferences();
    }
    partial void OnSelectedResolutionChanged(SelectionOption value)
    {
        foreach (var item in AllGalleryItems()) RefreshSaved(item);
        HasError = false; _retryOperation = null; SavePreferences();
        StatusMessage = T("Auflösung geändert. Vorhandene Dateien wurden erneut geprüft.");
    }
    partial void OnSelectedThemeChanged(SelectionOption value) { _dialogs.ApplyTheme(value.Value); SavePreferences(); }
    private string GallerySources()
    {
        var spotlight = Wallpapers.Any(x => x.Image.Source == WallpaperSource.WindowsSpotlight);
        var bing = Wallpapers.Any(x => x.Image.Source == WallpaperSource.Bing);
        return spotlight ? (bing ? T("Bing und Windows-Blickpunkt") : T("Windows-Blickpunkt")) : "Bing";
    }
    private void UpdateGallerySubtitle() => GallerySubtitle = T("{0} · {1} Bilder · {2}", SelectedMarket.Label,
        Wallpapers.Count, GallerySources());
    partial void OnIncludeWindowsSpotlightChanged(bool value)
    {
        ClosePreview();
        if (IsCollectionView) { foreach (var item in _discoverCache) item.Preview?.Dispose(); _discoverCache.Clear(); _discoverState = null; SavePreferences(); return; }
        ClearGallery();
        GallerySubtitle = T("{0} · bereit zum Entdecken", SelectedMarket.Label);
        StatusMessage = T("Bildquellen geändert. Mit Aktualisieren neue Bilder laden.");
        HasError = false; _retryOperation = null; SavePreferences();
    }
    partial void OnAutoRefreshOnStartupChanged(bool value) => SavePreferences();
    partial void OnAutoTagsChanged(bool value)
    {
        if (!value)
            foreach (var item in AllGalleryItems().Where(x => !x.KeywordsLoaded)) item.ApplyPredictions(Array.Empty<ImageTag>(), false);
        SavePreferences();
    }

    private void TranslateOptions()
    {
        foreach (var option in Markets.Concat(Resolutions).Concat(Themes)) option.Label = Text.Translate(option.Label);
    }
    partial void OnSelectedLanguageChanged(SelectionOption value)
    {
        Text.SetLanguage(value.Value);
        Text.ApplyResources();
        TranslateOptions();
        OnPropertyChanged(nameof(AppTitle));
        StatusMessage = Text.Translate(StatusMessage); ButtonText = Text.Translate(ButtonText);
        PreviewStatus = Text.Translate(PreviewStatus);
        KeywordStatus = Text.Translate(KeywordStatus); NotifyKeywordActions();
        OnPropertyChanged(nameof(EmptyHeading)); OnPropertyChanged(nameof(EmptyHelp));
        GallerySubtitle = IsCollectionView ? CollectionSubtitle() : Wallpapers.Count > 0
            ? T("{0} · {1} Bilder · {2}", SelectedMarket.Label, Wallpapers.Count, GallerySources())
            : T("{0} · bereit zum Entdecken", SelectedMarket.Label);
        foreach (var item in AllGalleryItems()) { item.SetLanguage(Text.Language); item.DownloadError = item.DownloadError == null ? null : Text.Translate(item.DownloadError); }
        NotifySelection(); SavePreferences();
    }

}
