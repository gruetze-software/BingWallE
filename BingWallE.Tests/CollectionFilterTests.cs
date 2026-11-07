using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using BingWallE.Views;
using Xunit;

namespace BingWallE.Tests;

public class CollectionFilterTests
{
    private static HttpClient Offline() => new(new Handler((_, _) => throw new Exception("Unexpected network request")));
    private static MainWindowViewModel Create(HttpClient http, string folder) =>
        new(new BingWallpaperClient(http), new WallpaperStore(http),
            new TestSettings { Value = new() { WallpaperPath = folder, Language = "en" } }, new TestDialogs());

    [AvaloniaFact]
    public async Task SearchUsesAllWordsAcrossMetadataAndEditableKeywordsWithoutLosingHiddenItems()
    {
        using var scope = new TestScope(); using var http = Offline();
        var vm = Create(http, scope.Path);
        var discovery = new ImageItemViewModel("https://example.test/image", "Discover", "20261107") { IsSelected = true };
        vm.Wallpapers.Add(discovery);
        await vm.ShowCollectionCommand.ExecuteAsync(null);
        var beach = new ImageItemViewModel(new Wallpaper("", "Hawaiʻi", "", Copyright: "© Photographer",
            Description: "Turquoise sea"), localPath: "my-beach.jpg");
        beach.AddKeyword("Favourite trip");
        var wolves = new ImageItemViewModel("", "Two gray wolves", "");
        vm.Wallpapers.Add(beach); vm.Wallpapers.Add(wolves);
        foreach (var query in new[] { "TURQUOISE favourite", "my-beach", "Photographer", "trip" })
        {
            vm.CollectionSearch = query;
            Assert.Same(beach, Assert.Single(vm.VisibleWallpapers));
        }
        beach.RemoveKeyword("Favourite trip");
        Assert.Empty(vm.VisibleWallpapers); Assert.True(vm.IsEmpty);
        Assert.Equal("No matching images found.", vm.EmptyHeading);
        beach.AddKeyword("trip");
        Assert.Same(beach, Assert.Single(vm.VisibleWallpapers));
        vm.ShowDiscoverCommand.Execute(null);
        Assert.Same(discovery, Assert.Single(vm.VisibleWallpapers)); Assert.True(discovery.IsSelected);
        await vm.ShowCollectionCommand.ExecuteAsync(null);
        Assert.Same(beach, Assert.Single(vm.VisibleWallpapers)); Assert.Equal(2, vm.Wallpapers.Count);
        vm.ResetCollectionFiltersCommand.Execute(null);
        Assert.Equal(2, vm.VisibleWallpapers.Count); Assert.False(vm.IsEmpty);
        vm.Shutdown();
    }

    [AvaloniaFact]
    public async Task SourceResolutionAndDownloadDateCombineAndSupportPortraitUhd()
    {
        using var scope = new TestScope(); using var http = Offline();
        var vm = Create(http, scope.Path); await vm.ShowCollectionCommand.ExecuteAsync(null);
        var portrait = new ImageItemViewModel(new Wallpaper("", "Portrait", "20000101", Source: WallpaperSource.WindowsSpotlight))
            { PixelWidth = 2160, PixelHeight = 3840, DownloadedAt = DateTimeOffset.Now };
        var old = new ImageItemViewModel(new Wallpaper("", "Old", "", Source: WallpaperSource.WindowsSpotlight))
            { PixelWidth = 3840, PixelHeight = 2160, DownloadedAt = DateTimeOffset.Now.AddDays(-31) };
        var small = new ImageItemViewModel(new Wallpaper("", "Small", "", Source: WallpaperSource.Bing))
            { PixelWidth = 1920, PixelHeight = 1080, DownloadedAt = DateTimeOffset.Now };
        var unknown = new ImageItemViewModel(new Wallpaper("", "Unknown", "", Source: WallpaperSource.Local));
        foreach (var item in new[] { portrait, old, small, unknown }) vm.Wallpapers.Add(item);
        vm.CollectionSourceIndex = 2; vm.CollectionResolutionIndex = 3; vm.CollectionDateIndex = 2;
        Assert.Same(portrait, Assert.Single(vm.VisibleWallpapers));
        Assert.Contains("1 of 4", vm.GallerySubtitle);
        vm.CollectionDateIndex = 0; Assert.Equal(2, vm.VisibleWallpapers.Count);
        vm.CollectionSourceIndex = 1; Assert.Empty(vm.VisibleWallpapers);
        vm.CollectionResolutionIndex = 2; Assert.Same(small, Assert.Single(vm.VisibleWallpapers));
        vm.ResetCollectionFiltersCommand.Execute(null); Assert.Equal(4, vm.VisibleWallpapers.Count);
        vm.CollectionSourceIndex = 3; Assert.Same(unknown, Assert.Single(vm.VisibleWallpapers));
        vm.CollectionDateIndex = 1; Assert.Empty(vm.VisibleWallpapers);
        vm.Shutdown();
    }

    [Fact]
    public async Task DateFilterUsesEmbeddedDownloadDateAndFallsBackToFileDate()
    {
        using var scope = new TestScope();
        var downloaded = new DateTimeOffset(2025, 1, 2, 12, 0, 0, TimeSpan.Zero);
        File.WriteAllBytes(Path.Combine(scope.Path, "tagged.jpg"), PhotoMetadataWriter.Write(TestImages.Jpeg,
            new Wallpaper("", "Old download", "20261107"), [], downloaded));
        var plain = Path.Combine(scope.Path, "plain.jpg"); File.WriteAllBytes(plain, TestImages.Jpeg);
        File.SetLastWriteTimeUtc(plain, downloaded.AddDays(1).UtcDateTime);
        var images = new List<LocalWallpaper>();
        await foreach (var image in new LocalCollectionReader().ReadAsync(scope.Path, () => { }, TestContext.Current.CancellationToken)) images.Add(image);
        Assert.Equal(downloaded, images.Single(x => x.Image.Title == "Old download").DownloadedAt);
        Assert.Equal(downloaded.AddDays(1), images.Single(x => x.Image.Title == "plain").DownloadedAt);
    }

    [AvaloniaTheory]
    [InlineData(640, "de")]
    [InlineData(1040, "en")]
    public async Task FilterControlsFitWindowAndFollowLanguage(int width, string language)
    {
        using var scope = new TestScope(); using var http = Offline();
        var vm = Create(http, scope.Path);
        vm.SelectedLanguage = vm.Languages.Single(x => x.Value == language);
        vm.SaveWindowState(new(width, 820));
        var window = new MainWindow { DataContext = vm, Width = width, Height = 820 }; window.Show();
        try
        {
            await vm.ShowCollectionCommand.ExecuteAsync(null); Dispatcher.UIThread.RunJobs();
            Assert.Equal(width, window.ClientSize.Width);
            var controls = window.GetVisualDescendants().OfType<ComboBox>().Where(x => x.IsEffectivelyVisible).ToArray();
            Assert.Equal(3, controls.Length);
            foreach (var control in controls)
            {
                var right = control.TranslatePoint(new Avalonia.Point(control.Bounds.Width, 0), window)!.Value.X;
                Assert.InRange(right, 1, window.ClientSize.Width);
            }
            Assert.Equal(language == "en" ? "All sources" : "Alle Quellen", vm.CollectionSources[0].Label);
            if (Environment.GetEnvironmentVariable("BINGWALLE_UI_SNAPSHOTS") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory); AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);
                using var frame = window.CaptureRenderedFrame();
                frame!.Save(Path.Combine(directory, $"collection-filters-{language}-{width}.png"), PngBitmapEncoderOptions.Default);
            }
        }
        finally { window.Close(); }
    }
}
