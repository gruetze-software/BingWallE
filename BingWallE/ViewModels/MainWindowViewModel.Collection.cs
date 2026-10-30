using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;

namespace BingWallE.ViewModels;

public partial class MainWindowViewModel
{
    private readonly List<ImageItemViewModel> _discoverCache = new();
    private readonly List<ImageItemViewModel> _collectionCache = new();
    private bool _collectionLoaded;
    private bool _isCollectionView;
    public bool IsCollectionView => _isCollectionView;
    public bool IsDiscoverView => !_isCollectionView;
    private string CollectionSubtitle() => Wallpapers.Count == 1 ? T("Meine Sammlung · 1 Bild") : T("Meine Sammlung · {0} Bilder", Wallpapers.Count);
    public string EmptyHeading => T(IsCollectionView ? "Deine Sammlung ist noch leer." : "Deine nächste Entdeckung wartet.");
    public string EmptyHelp => T(IsCollectionView
        ? "Hier erscheinen die Bilder im eingestellten Ordner. Wechsle zu Entdecken, um neue Bilder zu sammeln."
        : "Lade Bilder aus den gewählten Quellen mit Aktualisieren. Ein Klick auf ein Motiv öffnet die große Vorschau.");
    private sealed record GalleryState(string Status, string Subtitle, bool Error, string? Retry, double Progress);
    private GalleryState? _discoverState;
    private GalleryState? _collectionState;

    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private void ShowDiscover()
    {
        if (IsIdle && !IsPreviewOpen && IsCollectionView) SwitchGallery(false);
    }

    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private async Task ShowCollection()
    {
        if (!IsIdle || IsPreviewOpen || IsCollectionView) return;
        SwitchGallery(true);
        if (!_collectionLoaded) await ReloadCollection();
    }

    private void SwitchGallery(bool collection)
    {
        var state = new GalleryState(StatusMessage, GallerySubtitle, HasError, _retryOperation, ProgressValue);
        var cache = IsCollectionView ? _collectionCache : _discoverCache;
        cache.Clear(); cache.AddRange(Wallpapers);
        if (IsCollectionView) _collectionState = state; else _discoverState = state;
        Wallpapers.Clear();
        _isCollectionView = collection;
        OnPropertyChanged(nameof(IsCollectionView)); OnPropertyChanged(nameof(IsDiscoverView));
        OnPropertyChanged(nameof(EmptyHeading)); OnPropertyChanged(nameof(EmptyHelp));
        foreach (var item in collection ? _collectionCache : _discoverCache)
        {
            if (!collection)
            {
                var selected = item.IsSelected;
                var error = item.DownloadError;
                RefreshSaved(item);
                item.IsSelected = selected && !item.IsIgnored;
                if (!item.IsSaved) item.DownloadError = error;
            }
            Wallpapers.Add(item);
        }
        var restored = collection ? _collectionState : _discoverState;
        StatusMessage = restored == null ? T(collection ? "Lokale Sammlung wird geladen …" : "Region geändert. Mit Aktualisieren neue Bilder laden.") : Text.Translate(restored.Status);
        GallerySubtitle = restored == null ? (collection ? T("Meine Sammlung") : T("{0} · bereit zum Entdecken", SelectedMarket.Label)) : Text.Translate(restored.Subtitle);
        HasError = restored?.Error ?? false; _retryOperation = restored?.Retry;
        ProgressValue = restored?.Progress ?? 0;
        StatusColor = HasError ? Brushes.IndianRed : Brushes.Gray;
    }

    private async Task ReloadCollection()
    {
        if (!IsIdle || !IsCollectionView) return;
        ClosePreview();
        _retryOperation = "collection";
        using var cancellation = BeginOperation(T("Lokale Sammlung wird geladen …"), true);
        var skipped = 0;
        var folder = TargetFolder ?? "";
        try
        {
            ClearGallery();
            await foreach (var local in new Services.LocalCollectionReader().ReadAsync(folder, () => skipped++, cancellation.Token))
            {
                cancellation.Token.ThrowIfCancellationRequested();
                Bitmap? bitmap = null;
                try
                {
                    bitmap = await Task.Run(() =>
                    {
                        using var stream = new MemoryStream(local.Bytes);
                        return Bitmap.DecodeToWidth(stream, 320);
                    }, cancellation.Token);
                    cancellation.Token.ThrowIfCancellationRequested();
                    var item = new ImageItemViewModel(local.Image, bitmap, local.Path)
                    { IsSaved = true, ActualDimensions = $"{local.Width} × {local.Height}" };
                    item.LoadKeywords(local.Keywords);
                    Wallpapers.Add(item);
                    bitmap = null; // Ownership passes to the gallery.
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                { System.Diagnostics.Trace.WriteLine(ex); skipped++; }
                finally { bitmap?.Dispose(); }
            }
            GallerySubtitle = CollectionSubtitle();
            StatusMessage = skipped > 0 ? T("Sammlung geladen. {0} Dateien konnten nicht gelesen werden.", skipped)
                : T("Sammlung geladen. Bilder werden aus deinem lokalen Ordner angezeigt.");
            ProgressValue = 100; _retryOperation = null; _collectionLoaded = true;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            GallerySubtitle = CollectionSubtitle();
            StatusMessage = T("Laden abgebrochen. Bereits geladene Bilder bleiben verfügbar.");
        }
        catch (Exception ex) { ReportError(ex); }
        finally { EndOperation(); }
    }

    private void InvalidateCollection()
    {
        _collectionLoaded = false; _collectionState = null;
        if (IsCollectionView) { _ = ReloadCollection(); return; }
        foreach (var item in _collectionCache) item.Preview?.Dispose();
        _collectionCache.Clear();
    }

    private IEnumerable<ImageItemViewModel> AllGalleryItems() =>
        Wallpapers.Concat(_discoverCache).Concat(_collectionCache).Distinct();
}
