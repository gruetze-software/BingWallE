using System.Net;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using BingWallE.Views;
using ExifLibrary;
using Xunit;

namespace BingWallE.Tests;

public class KeywordEditingTests
{
    private static Wallpaper Wolves => new("https://example.test/wolves.jpg", "Two gray wolves, Yellowstone National Park", "20261030",
        Copyright: "© Jörg Müller / Getty Images", Market: "en-GB", Description: "A quiet winter morning");

    [Fact]
    public void TitleAndDescriptionBothSupplyKeywordsAndRespectSourceLanguage()
    {
        var keywords = WallpaperKeywords.Build(Wolves, []);
        Assert.Contains(keywords, x => x.Label == "Wolf" && !x.FromModel);
        Assert.Contains(keywords, x => x.Label == "Animal" && !x.FromModel);
        Assert.Contains(keywords, x => x.Label == "Snow" || x.Label == "Winter");
        Assert.Contains(WallpaperKeywords.Build(Wolves with { Title = "Wölfe", Market = "de-DE" }, []), x => x.Label == "Tier");
        Assert.Contains(WallpaperKeywords.Build(Wolves with { Title = "animals" }, []), x => x.Label == "Animal");
    }

    [Fact]
    public void RemovedSuggestionsStayExcludedAfterReloadAndRerunWhileCustomTermsSurvive()
    {
        var item = new ImageItemViewModel(Wolves);
        item.ApplyPredictions([new("coyote", .99f)]);
        Assert.Contains(item.SuggestedKeywordChips, x => x.Label == "Coyote");
        item.RemoveKeyword("Coyote");
        Assert.True(item.AddKeyword("Yellowstone trip"));
        Assert.False(item.AddKeyword("Yellowstone TRIP"));
        var bytes = KeywordMetadata.Update(PhotoMetadataWriter.Write(TestImages.Jpeg, Wolves, []), item.ExportKeywords());
        var reopened = new ImageItemViewModel(Wolves);
        reopened.LoadKeywords(KeywordMetadata.Read(bytes));
        reopened.ApplyPredictions([new("COYOTE", .99f)]);
        Assert.DoesNotContain(reopened.SuggestedKeywordChips, x => x.Label.Equals("Coyote", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(reopened.KeywordChips, x => x.Label == "Yellowstone trip");
        Assert.True(reopened.AddKeyword("Coyote")); // Explicitly adding it back removes the exclusion.
        reopened.ApplyPredictions([new("coyote", .99f)]);
        Assert.Single(reopened.KeywordChips, x => x.Label == "Coyote");
        Assert.DoesNotContain(reopened.SuggestedKeywordChips, x => x.Label == "Coyote");
    }

    [Fact]
    public void KeywordUpdatePreservesCompressedImageCameraFieldsAndOtherXmp()
    {
        var file = ImageFile.FromBuffer(TestImages.Jpeg);
        file.Properties.Set(ExifTag.Artist, "Original photographer");
        file.Properties.Set(ExifTag.DateTimeOriginal, new DateTime(2020, 2, 3, 4, 5, 6));
        using var initial = new MemoryStream(); file.Save(initial);
        var before = PhotoMetadataWriter.Write(initial.ToArray(), Wolves, [new("coyote", .99f)], new DateTimeOffset(2026, 10, 30, 10, 0, 0, TimeSpan.Zero));
        var state = new KeywordMetadataState(["Wolf", "Animal", "Urlaub an der Küste"], ["Coyote"], [], ["Urlaub an der Küste"]);
        var after = KeywordMetadata.Update(before, state);
        var oldExif = ImageFile.FromBuffer(before); var newExif = ImageFile.FromBuffer(after);
        foreach (var tag in new[] { ExifTag.WindowsTitle, ExifTag.WindowsSubject, ExifTag.WindowsComment, ExifTag.UserComment,
            ExifTag.Copyright, ExifTag.Artist, ExifTag.DateTimeOriginal, ExifTag.DateTimeDigitized, ExifTag.Software })
            Assert.Equal(oldExif.Properties.Get(tag)?.Value, newExif.Properties.Get(tag)?.Value);
        var oldXmp = KeywordMetadata.ReadXmp(before)!; var newXmp = KeywordMetadata.ReadXmp(after)!;
        foreach (var element in oldXmp.Descendants().Where(x => new[] { "title", "description", "rights", "DownloadDate", "CreateDate", "SourceUrl", "PublicationDate" }.Contains(x.Name.LocalName)))
            Assert.Equal(element.ToString(), newXmp.Descendants(element.Name).Single().ToString());
        Assert.Equal(ScanData(before), ScanData(after));
        Assert.Equal(state.Keywords, KeywordMetadata.Read(after).Keywords);
        Assert.DoesNotContain("coyote", newExif.Properties.Get(ExifTag.WindowsKeywords).Value.ToString()!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, Encoding.UTF8.GetString(after).Split("http://ns.adobe.com/xap/1.0/\0").Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddingExifKeywordEntryPreservesBothTiffByteOrdersAndRepeatedSavesStayBounded(bool little)
    {
        byte[] tiff = little
            ? [73, 73, 42, 0, 8, 0, 0, 0, 1, 0, 14, 1, 2, 0, 4, 0, 0, 0, 111, 108, 100, 0, 0, 0, 0, 0]
            : [77, 77, 0, 42, 0, 0, 0, 8, 0, 1, 1, 14, 0, 2, 0, 0, 0, 4, 111, 108, 100, 0, 0, 0, 0, 0];
        using var jpeg = new MemoryStream(); jpeg.Write(TestImages.Jpeg, 0, 2);
        jpeg.Write([255, 225, 0, (byte)(tiff.Length + 8)]); jpeg.Write("Exif\0\0"u8); jpeg.Write(tiff); jpeg.Write(TestImages.Jpeg, 2, TestImages.Jpeg.Length - 2);
        var bytes = KeywordMetadata.Update(jpeg.ToArray(), new(["Wolf", "Animals"], [], [], []));
        var firstLength = bytes.Length;
        for (var i = 0; i < 20; i++) bytes = KeywordMetadata.Update(bytes, new(["Wolf", "Animals"], [], [], []));
        Assert.Equal(firstLength, bytes.Length);
        Assert.Equal("old", ImageFile.FromBuffer(bytes).Properties.Get(ExifTag.ImageDescription).Value);
        Assert.Equal(new[] { "Wolf", "Animals" }, KeywordMetadata.Read(bytes).Keywords);
        bytes = KeywordMetadata.Update(bytes, KeywordMetadataState.Empty);
        Assert.Empty(KeywordMetadata.Read(bytes).Keywords);
        Assert.Equal("old", ImageFile.FromBuffer(bytes).Properties.Get(ExifTag.ImageDescription).Value);
        Assert.Equal(ScanData(TestImages.Jpeg), ScanData(bytes));
    }

    [Fact]
    public async Task FailedOrCanceledKeywordSaveLeavesOriginalFileUntouched()
    {
        using var scope = new TestScope();
        var path = Path.Combine(scope.Path, "photo.jpg"); File.WriteAllBytes(path, TestImages.Jpeg);
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => KeywordMetadata.SaveAsync(path, KeywordMetadataState.Empty, canceled.Token));
        Assert.Equal(TestImages.Jpeg, File.ReadAllBytes(path));
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try { await Assert.ThrowsAsync<UnauthorizedAccessException>(() => KeywordMetadata.SaveAsync(path, KeywordMetadataState.Empty, TestContext.Current.CancellationToken)); }
        finally { File.SetAttributes(path, FileAttributes.Normal); }
        Assert.Equal(TestImages.Jpeg, File.ReadAllBytes(path)); Assert.Single(Directory.GetFiles(scope.Path));
    }

    [Fact]
    public async Task FutureDownloadUsesReviewedKeywordsInsteadOfReintroducingModelSuggestion()
    {
        using var scope = new TestScope();
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new ByteArrayContent(TestImages.Jpeg) })));
        var item = new ImageItemViewModel(Wolves); item.ApplyPredictions([new("coyote", .99f)]);
        item.RemoveKeyword("Coyote"); item.AddKeyword("Personal album");
        var store = new WallpaperStore(http, new CoyoteTagger());
        await store.DownloadAsync(scope.Path, Wolves, TestContext.Current.CancellationToken, true, keywords: item.ExportKeywords());
        var saved = KeywordMetadata.Read(File.ReadAllBytes(store.GetPath(scope.Path, Wolves)));
        Assert.Contains("Personal album", saved.Keywords); Assert.Contains("Coyote", saved.ExcludedKeywords);
        Assert.DoesNotContain(saved.Keywords, x => x.Equals("coyote", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RedownloadingKeepsPreviouslyReviewedKeywordsAndExclusions()
    {
        using var scope = new TestScope();
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new ByteArrayContent(TestImages.Jpeg) })));
        var store = new WallpaperStore(http, new CoyoteTagger());
        var state = new KeywordMetadataState(["My album", "Wolf"], ["Coyote"], [], ["My album"]);
        await store.DownloadAsync(scope.Path, Wolves, TestContext.Current.CancellationToken, keywords: state);
        await store.DownloadAsync(scope.Path, Wolves, TestContext.Current.CancellationToken, true);
        var saved = KeywordMetadata.Read(File.ReadAllBytes(store.GetPath(scope.Path, Wolves)));
        Assert.Equal(state.Keywords, saved.Keywords); Assert.Equal(state.ExcludedKeywords, saved.ExcludedKeywords);
        Assert.Equal(state.UserKeywords, saved.UserKeywords);
    }

    [AvaloniaFact]
    public async Task EditingIsExplicitSavingWorksOfflineAndSuppressesCoyoteAfterCollectionReload()
    {
        using var scope = new TestScope(); using var http = Offline();
        var path = Path.Combine(scope.Path, "saved.jpg");
        var before = PhotoMetadataWriter.Write(TestImages.Jpeg, Wolves, [new("coyote", .99f)]);
        File.WriteAllBytes(path, before);
        var tagger = new CoyoteTagger(); var vm = Create(http, scope.Path, tagger);
        await vm.ShowCollectionCommand.ExecuteAsync(null);
        var item = Assert.Single(vm.Wallpapers);
        await vm.OpenPreviewCommand.ExecuteAsync(item);
        Assert.True(vm.CanSaveKeywords); Assert.Equal(0, tagger.Calls);
        vm.RemovePreviewKeywordCommand.Execute(item.SuggestedKeywordChips.Single(x => x.Label == "Coyote"));
        vm.KeywordInput = "My wolves"; vm.AddPreviewKeywordCommand.Execute(null);
        await vm.RecognizePreviewKeywordsCommand.ExecuteAsync(null);
        Assert.Equal(1, tagger.Calls); Assert.Equal(before, File.ReadAllBytes(path));
        Assert.DoesNotContain(item.SuggestedKeywordChips, x => x.Label == "Coyote");
        Assert.Contains(item.KeywordChips, x => x.Label == "My wolves");
        await vm.SavePreviewKeywordsCommand.ExecuteAsync(null);
        Assert.False(item.HasKeywordChanges); Assert.Equal("Keywords saved.", vm.KeywordStatus);
        vm.ClosePreviewCommand.Execute(null); await vm.StartStopCommand.ExecuteAsync(null);
        var reopened = Assert.Single(vm.Wallpapers);
        Assert.Contains(reopened.KeywordChips, x => x.Label == "My wolves");
        Assert.DoesNotContain(reopened.SuggestedKeywordChips, x => x.Label == "Coyote");
        Assert.Equal(ScanData(before), ScanData(File.ReadAllBytes(path)));
        vm.Shutdown();
    }

    [AvaloniaFact]
    public async Task ClosingPreviewCancelsExplicitRecognitionWithoutSaving()
    {
        using var scope = new TestScope(); using var http = Offline();
        var path = Path.Combine(scope.Path, "photo.jpg"); File.WriteAllBytes(path, TestImages.Jpeg);
        var tagger = new WaitingTagger(); var vm = Create(http, scope.Path, tagger);
        await vm.ShowCollectionCommand.ExecuteAsync(null); await vm.OpenPreviewCommand.ExecuteAsync(vm.Wallpapers[0]);
        var pending = vm.RecognizePreviewKeywordsCommand.ExecuteAsync(null);
        await tagger.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        vm.ClosePreviewCommand.Execute(null);
        await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(tagger.Canceled); Assert.False(vm.IsKeywordOperationRunning); Assert.False(vm.IsPreviewOpen);
        Assert.Equal(TestImages.Jpeg, File.ReadAllBytes(path)); vm.Shutdown();
    }

    [AvaloniaTheory]
    [InlineData("de", 640, 700)]
    [InlineData("en", 980, 850)]
    public async Task ChipRemovalAndLongCustomKeywordFitPreview(string language, int width, int height)
    {
        using var scope = new TestScope(); using var http = Offline();
        var path = Path.Combine(scope.Path, "photo.jpg"); File.WriteAllBytes(path, PhotoMetadataWriter.Write(TestImages.Jpeg, Wolves, [new("coyote", .99f)]));
        var oldTheme = Avalonia.Application.Current!.RequestedThemeVariant;
        Avalonia.Application.Current.RequestedThemeVariant = language == "en" ? ThemeVariant.Dark : ThemeVariant.Light;
        var settings = new TestSettings { Value = new() { Language = language, WallpaperPath = scope.Path, PreviewWindow = new(width, height) } };
        var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http), settings, new TestDialogs(), new CoyoteTagger());
        await vm.ShowCollectionCommand.ExecuteAsync(null); await vm.OpenPreviewCommand.ExecuteAsync(vm.Wallpapers[0]);
        var window = new PreviewWindow { DataContext = vm }; window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            var remove = window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "×") && b.DataContext is KeywordChip { Label: "Coyote" });
            remove.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            vm.KeywordInput = new string('W', 100); vm.AddPreviewKeywordCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.DoesNotContain(vm.PreviewItem!.SuggestedKeywordChips, x => x.Label == "Coyote");
            var text = window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == new string('W', 100));
            var scroll = window.FindControl<ScrollViewer>("PreviewScroll")!;
            var right = text.TranslatePoint(new Avalonia.Point(text.Bounds.Width, 0), scroll)!.Value.X;
            Assert.True(right <= scroll.Bounds.Width - 16);
            if (Environment.GetEnvironmentVariable("BINGWALLE_UI_SNAPSHOTS") is { Length: > 0 } topDirectory)
            {
                Directory.CreateDirectory(topDirectory); AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);
                using var topFrame = window.CaptureRenderedFrame();
                topFrame!.Save(Path.Combine(topDirectory, $"keyword-chips-{language}.png"), PngBitmapEncoderOptions.Default);
            }
            scroll.Offset = new Avalonia.Vector(0, scroll.Extent.Height); Dispatcher.UIThread.RunJobs();
            if (Environment.GetEnvironmentVariable("BINGWALLE_UI_SNAPSHOTS") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory); AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);
                using var frame = window.CaptureRenderedFrame();
                frame!.Save(Path.Combine(directory, $"keyword-editor-{language}.png"), PngBitmapEncoderOptions.Default);
            }
        }
        finally { window.Close(); vm.Shutdown(); Avalonia.Application.Current.RequestedThemeVariant = oldTheme; }
    }

    private static HttpClient Offline() => new(new Handler((_, _) => throw new Exception("Unexpected network")));
    private static MainWindowViewModel Create(HttpClient http, string folder, IImageTagger tagger) => new(new BingWallpaperClient(http), new WallpaperStore(http),
        new TestSettings { Value = new() { Language = "en", WallpaperPath = folder } }, new TestDialogs(), tagger);
    private sealed class CoyoteTagger : IImageTagger
    {
        public int Calls { get; private set; }
        public Task<IReadOnlyList<ImageTag>> RecognizeAsync(byte[] image, CancellationToken ct, Action<string>? progress = null)
        { Calls++; return Task.FromResult<IReadOnlyList<ImageTag>>([new("coyote", .99f)]); }
    }
    private sealed class WaitingTagger : IImageTagger
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Canceled { get; private set; }
        public async Task<IReadOnlyList<ImageTag>> RecognizeAsync(byte[] image, CancellationToken ct, Action<string>? progress = null)
        {
            Started.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, ct); }
            catch (OperationCanceledException) { Canceled = true; throw; }
            return [];
        }
    }
    private static byte[] ScanData(byte[] jpeg)
    {
        for (var i = 2; i < jpeg.Length - 1; i++)
            if (jpeg[i] == 255 && jpeg[i + 1] == 0xDA) return jpeg[i..];
        throw new Exception("JPEG scan missing");
    }
}
