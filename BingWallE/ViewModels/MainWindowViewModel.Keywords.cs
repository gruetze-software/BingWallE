using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BingWallE.Services;
using BingWallE.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BingWallE.ViewModels;

public partial class MainWindowViewModel
{
    private CancellationTokenSource? _keywordCts;
    private byte[]? _previewBytes;
    private string? _previewFilePath;
    private bool _previewIsJpeg;
    [ObservableProperty] private string _keywordInput = "";
    [ObservableProperty] private string _keywordStatus = "";
    [ObservableProperty] private bool _isKeywordOperationRunning;
    public bool CanEditKeywords => PreviewItem != null && !PreviewFailed && !IsPreviewLoading && !IsKeywordOperationRunning;
    public bool CanRecognizeKeywords => CanEditKeywords && _tagger != null && _previewBytes != null;
    public bool CanSaveKeywords => CanEditKeywords && _previewFilePath != null && _previewIsJpeg;
    public string KeywordSaveHint => T(_previewFilePath == null
        ? "Keyword-Änderungen werden beim Download gespeichert."
        : !_previewIsJpeg ? "Keywords können derzeit nur in JPEG-Dateien gespeichert werden."
        : "Änderungen werden erst mit Keywords speichern in die Datei übernommen.");

    private void NotifyKeywordActions()
    {
        OnPropertyChanged(nameof(CanEditKeywords)); OnPropertyChanged(nameof(CanRecognizeKeywords));
        OnPropertyChanged(nameof(CanSaveKeywords)); OnPropertyChanged(nameof(KeywordSaveHint));
        RecognizePreviewKeywordsCommand.NotifyCanExecuteChanged(); SavePreviewKeywordsCommand.NotifyCanExecuteChanged();
        AddPreviewKeywordCommand.NotifyCanExecuteChanged(); RemovePreviewKeywordCommand.NotifyCanExecuteChanged();
    }
    partial void OnIsPreviewLoadingChanged(bool value) => NotifyKeywordActions();
    partial void OnPreviewFailedChanged(bool value) => NotifyKeywordActions();
    partial void OnIsKeywordOperationRunningChanged(bool value) => NotifyKeywordActions();
    partial void OnPreviewItemChanged(ImageItemViewModel? value)
    {
        KeywordInput = ""; KeywordStatus = ""; NotifyKeywordActions();
    }

    [RelayCommand(CanExecute = nameof(CanEditKeywords))]
    private void AddPreviewKeyword()
    {
        if (!CanEditKeywords || PreviewItem == null) return;
        if (PreviewItem.AddKeyword(KeywordInput))
        { KeywordInput = ""; KeywordStatus = T("Keyword hinzugefügt. Noch nicht gespeichert."); }
        else KeywordStatus = T("Bitte einen neuen Begriff mit höchstens 100 Zeichen ohne Semikolon eingeben.");
    }

    [RelayCommand(CanExecute = nameof(CanEditKeywords))]
    private void RemovePreviewKeyword(KeywordChip? keyword)
    {
        if (!CanEditKeywords || keyword == null || PreviewItem == null) return;
        PreviewItem.RemoveKeyword(keyword.Label);
        KeywordStatus = T("Keyword entfernt. Noch nicht gespeichert.");
    }

    [RelayCommand(CanExecute = nameof(CanRecognizeKeywords))]
    private async Task RecognizePreviewKeywords()
    {
        if (!CanRecognizeKeywords || PreviewItem == null || _previewBytes == null || _tagger == null) return;
        var item = PreviewItem; var bytes = _previewBytes;
        using var cancellation = new CancellationTokenSource();
        _keywordCts = cancellation; IsKeywordOperationRunning = true;
        KeywordStatus = T("Bildmotive werden lokal erkannt …");
        try
        {
            var tags = await _tagger.RecognizeAsync(bytes, cancellation.Token, message => Dispatcher.UIThread.Post(() =>
            {
                if (ReferenceEquals(_keywordCts, cancellation)) KeywordStatus = Text.Progress(message);
            }));
            cancellation.Token.ThrowIfCancellationRequested();
            item.ApplyPredictions(tags);
            KeywordStatus = T("Vorschläge aktualisiert. Bitte prüfen und speichern.");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex)
        { System.Diagnostics.Trace.WriteLine(ex); if (ReferenceEquals(_keywordCts, cancellation)) KeywordStatus = ErrorMessage(ex); }
        finally
        {
            if (ReferenceEquals(_keywordCts, cancellation)) { _keywordCts = null; IsKeywordOperationRunning = false; }
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveKeywords))]
    private async Task SavePreviewKeywords()
    {
        if (!CanSaveKeywords || PreviewItem == null || _previewFilePath == null) return;
        var path = _previewFilePath; var preview = PreviewItem; var state = preview.ExportKeywords();
        using var cancellation = new CancellationTokenSource();
        _keywordCts = cancellation; IsKeywordOperationRunning = true;
        KeywordStatus = T("Keywords werden gespeichert …");
        try
        {
            await KeywordMetadata.SaveAsync(path, state, cancellation.Token);
            foreach (var item in AllGalleryItems().Append(preview).Distinct().Where(x => ReferenceEquals(x, preview) || SamePath(SavedPath(x), path))) item.LoadKeywords(state, force: true);
            if (ReferenceEquals(PreviewItem, preview)) KeywordStatus = T("Keywords gespeichert.");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex)
        { System.Diagnostics.Trace.WriteLine(ex); if (ReferenceEquals(_keywordCts, cancellation)) KeywordStatus = ErrorMessage(ex); }
        finally
        {
            if (ReferenceEquals(_keywordCts, cancellation)) { _keywordCts = null; IsKeywordOperationRunning = false; }
        }
    }

    private string? SavedPath(ImageItemViewModel item)
    {
        if (item.LocalPath != null) return item.LocalPath;
        if (!item.IsSaved) return null;
        try { return _images.GetPath(TargetFolder ?? "", WallpaperVariants.ForResolution(item.Image, SelectedResolution.Value)); }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException) { return null; }
    }
    private static bool SamePath(string? left, string right) => left != null && string.Equals(Path.GetFullPath(left), Path.GetFullPath(right),
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
