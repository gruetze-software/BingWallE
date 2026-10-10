using System.Net;
using System.Runtime.CompilerServices;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using Xunit;

namespace BingWallE.Tests;

public class CollectorTests
{
    private static Wallpaper Image => new("https://bing.com/th?id=Test_1920x1080.jpg", "Test", "20261009",
        "/th?id=Test", "Testlandschaft (© Fotograf)");

    [Fact]
    public void ResolutionVariantsKeepFullHdCompatibilityAndSeparateFiles()
    {
        var uhd = WallpaperVariants.ForResolution(Image, "UHD");
        Assert.Equal("https://bing.com/th?id=Test_UHD.jpg", uhd.Url);
        Assert.Equal("20261009_Test_UHD.jpg", WallpaperVariants.FileName(uhd));
        Assert.Equal("20261009_Test.jpg", WallpaperVariants.FileName(Image));
        Assert.EndsWith("_1366x768.jpg", WallpaperVariants.ForResolution(Image, "1366x768").Url);
        Assert.Throws<ArgumentException>(() => WallpaperVariants.ForResolution(Image, "bad"));
        Assert.Throws<InvalidOperationException>(() => WallpaperVariants.ForResolution(Image with { UrlBase = null }, "UHD"));
    }

    [Fact]
    public void BulkSelectionAndResolutionSpecificExistingFiles()
    {
        using var scope = new TestScope();
        using var client = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var vm = Create(client, scope.Path);
        var first = new ImageItemViewModel(Image);
        var second = new ImageItemViewModel(Image with { Title = "Second", Date = "20261008" });
        vm.Wallpapers.Add(first); vm.Wallpapers.Add(second);
        File.WriteAllText(Path.Combine(scope.Path, WallpaperVariants.FileName(Image)), "existing");
        vm.SelectNewCommand.Execute(null);
        Assert.True(first.IsSaved); Assert.False(first.IsSelected);
        Assert.True(second.IsSelected); Assert.Equal(1, vm.SelectedCount);
        vm.SelectAllCommand.Execute(null); Assert.Equal(2, vm.SelectedCount);
        vm.SelectNoneCommand.Execute(null); Assert.Equal(0, vm.SelectedCount);
        vm.SelectedResolution = vm.Resolutions.Single(x => x.Value == "UHD");
        Assert.False(first.IsSaved); Assert.Equal(2, vm.SelectedCount);
        Assert.Equal("2 Bilder herunterladen", vm.DownloadLabel);
    }

    [Fact]
    public void PreferencesRoundTripAndUnknownOptionsFallBack()
    {
        using var scope = new TestScope();
        using var client = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var settings = new JsonSettingsStore(Path.Combine(scope.Path, "settings.json"));
        settings.Save(new AppSettings { WallpaperPath = scope.Path, Market = "invalid", Resolution = "invalid", Theme = "invalid" });
        var vm = new MainWindowViewModel(new BingWallpaperClient(client), new WallpaperStore(client), settings, new TestDialogs());
        Assert.Equal("de-DE", vm.SelectedMarket.Value); Assert.Equal("1920x1080", vm.SelectedResolution.Value);
        vm.SelectedMarket = vm.Markets.Single(x => x.Value == "ja-JP");
        vm.SelectedResolution = vm.Resolutions.Single(x => x.Value == "UHD");
        vm.SelectedTheme = vm.Themes.Single(x => x.Value == "Dark");
        var restored = settings.Load();
        Assert.Equal("ja-JP", restored.Market); Assert.Equal("UHD", restored.Resolution); Assert.Equal("Dark", restored.Theme);
        Assert.Equal(scope.Path, restored.WallpaperPath);
    }

    [Fact]
    public async Task RetryDownloadsOnlyRemainingSelection()
    {
        using var scope = new TestScope();
        var count = 0;
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(
            ++count == 2 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
        { Content = new ByteArrayContent([1, 2, 3]) })));
        var vm = Create(client, scope.Path);
        var first = new ImageItemViewModel(Image) { IsSelected = true };
        var second = new ImageItemViewModel(Image with { Title = "Second" }) { IsSelected = true };
        vm.Wallpapers.Add(first); vm.Wallpapers.Add(second);
        await vm.DownloadSelectedCommand.ExecuteAsync(null);
        Assert.True(vm.HasError); Assert.True(first.IsSaved); Assert.False(first.IsSelected);
        Assert.NotNull(second.DownloadError);
        await vm.RetryCommand.ExecuteAsync(null);
        Assert.False(vm.HasError); Assert.True(second.IsSaved); Assert.Null(second.DownloadError);
        Assert.Equal(3, count); Assert.Equal(0, vm.SelectedCount); Assert.Equal(100, vm.ProgressValue);
    }

    [Fact]
    public async Task MissingUhdIsExplainedAndDoesNotCreateFile()
    {
        using var scope = new TestScope();
        using var client = new HttpClient(new Handler((request, _) =>
        {
            Assert.Contains("_UHD.jpg", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }));
        var vm = Create(client, scope.Path);
        vm.Wallpapers.Add(new ImageItemViewModel(Image) { IsSelected = true });
        vm.SelectedResolution = vm.Resolutions.Single(x => x.Value == "UHD");
        await vm.DownloadSelectedCommand.ExecuteAsync(null);
        Assert.True(vm.HasError); Assert.Contains("nicht verfügbar", vm.StatusMessage);
        Assert.True(vm.Wallpapers[0].IsSelected); Assert.Empty(Directory.GetFiles(scope.Path));
    }

    [Fact]
    public void ChangingMarketClearsOldResultsAndDateIsReadable()
    {
        using var scope = new TestScope();
        using var client = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var vm = Create(client, scope.Path);
        var item = new ImageItemViewModel(Image);
        Assert.Equal("9. Oktober 2026", item.DisplayDate);
        Assert.Contains("Fotograf", item.Copyright);
        vm.Wallpapers.Add(item);
        vm.SelectedMarket = vm.Markets.Single(x => x.Value == "en-US");
        Assert.True(vm.IsEmpty); Assert.Contains("USA", vm.GallerySubtitle);
    }

    [Fact]
    public async Task ClosingPreviewCancelsItsRequestAndResetsState()
    {
        using var scope = new TestScope();
        using var client = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var bing = new WaitingPreviewClient();
        var vm = new MainWindowViewModel(bing, new WallpaperStore(client), new TestSettings(), new TestDialogs());
        var pending = vm.OpenPreviewCommand.ExecuteAsync(new ImageItemViewModel(Image));
        await bing.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(vm.IsPreviewOpen); Assert.True(vm.IsPreviewLoading);
        vm.ClosePreviewCommand.Execute(null);
        await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(vm.IsPreviewOpen); Assert.False(vm.IsPreviewLoading); Assert.False(vm.PreviewFailed);
    }

    private static MainWindowViewModel Create(HttpClient client, string folder) => new(
        new BingWallpaperClient(client), new WallpaperStore(client),
        new TestSettings { Value = new AppSettings { WallpaperPath = folder } }, new TestDialogs());

    private sealed class WaitingPreviewClient : IBingWallpaperClient
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<byte[]> LoadPreviewAsync(Wallpaper image, CancellationToken ct)
        {
            Started.TrySetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, ct); return [];
        }
        public async IAsyncEnumerable<WallpaperPreview> ScanAsync(string market,
            [EnumeratorCancellation] CancellationToken cancellationToken, Action<string>? reportProgress = null)
        { await Task.CompletedTask; yield break; }
    }
}
