using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using BingWallE.Views;
using Xunit;

namespace BingWallE.Tests;

public class LocalCollectionTests
{
    [Fact]
    public async Task ReaderUsesEmbeddedMetadataFallsBackToFilenameAndSkipsBrokenFiles()
    {
        using var scope = new TestScope();
        var original = new Wallpaper("https://example.test/original.jpg", "Strand auf Hawaii", "20261022",
            Copyright: "© Photographer", Market: "en-GB", Source: WallpaperSource.WindowsSpotlight,
            SourceId: "spotlight-id", Description: "Sea and mountains");
        File.WriteAllBytes(Path.Combine(scope.Path, "download_UHD.JPG"), PhotoMetadataWriter.Write(TestImages.Jpeg, original, []));
        File.WriteAllBytes(Path.Combine(scope.Path, "without metadata.jpg"), TestImages.Jpeg);
        File.WriteAllText(Path.Combine(scope.Path, "broken.jpg"), "Not an image");
        File.WriteAllText(Path.Combine(scope.Path, "notes.txt"), "Not an image");
        Directory.CreateDirectory(Path.Combine(scope.Path, "subfolder"));
        File.WriteAllBytes(Path.Combine(scope.Path, "subfolder", "not-in-selected-folder.jpg"), TestImages.Jpeg);
        var skipped = 0;
        var found = new List<LocalWallpaper>();
        await foreach (var image in new LocalCollectionReader().ReadAsync(scope.Path, () => skipped++, TestContext.Current.CancellationToken)) found.Add(image);
        Assert.Equal(1, skipped); Assert.Equal(2, found.Count);
        var tagged = found.Single(x => x.Image.Title == original.Title);
        Assert.Equal(original.Source, tagged.Image.Source);
        Assert.Equal(original.Description, tagged.Image.Description);
        Assert.Equal(original.Date, tagged.Image.Date);
        Assert.Equal(original.Market, tagged.Image.Market);
        Assert.Equal(original.SourceId, tagged.Image.SourceId);
        Assert.Equal(original.Copyright, tagged.Image.Copyright);
        Assert.Equal(original.Url, tagged.Image.Url);
        Assert.True(tagged.Width > 0 && tagged.Height > 0);
        var fallback = found.Single(x => x.Image.Title == "without metadata");
        Assert.Equal(WallpaperSource.Local, fallback.Image.Source);
    }

    [AvaloniaFact]
    public async Task SwitchingKeepsDiscoverySelectionAndCollectionRefreshAndPreviewStayOffline()
    {
        using var scope = new TestScope(); using var http = Offline();
        var vm = Create(http, scope.Path);
        var discover = new ImageItemViewModel(new Wallpaper("https://example.test/not-downloaded.jpg", "Discover", "20261022")) { IsSelected = true };
        vm.Wallpapers.Add(discover);
        var path = Path.Combine(scope.Path, "local image_UHD.jpg");
        File.WriteAllBytes(path, TestImages.Jpeg);
        await vm.ShowCollectionCommand.ExecuteAsync(null);
        Assert.True(vm.IsCollectionView); Assert.False(vm.HasError);
        var local = Assert.Single(vm.Wallpapers);
        Assert.Equal(path, local.LocalPath); Assert.True(local.IsSaved); Assert.False(local.IsSelected);
        vm.ShowDiscoverCommand.Execute(null);
        Assert.Same(discover, Assert.Single(vm.Wallpapers)); Assert.True(discover.IsSelected);
        File.WriteAllBytes(Path.Combine(scope.Path, "another.jpg"), TestImages.Jpeg);
        await vm.ShowCollectionCommand.ExecuteAsync(null);
        Assert.Same(local, Assert.Single(vm.Wallpapers)); // Returning does not reread the folder.
        await vm.StartStopCommand.ExecuteAsync(null);
        Assert.Equal(2, vm.Wallpapers.Count); Assert.False(vm.HasError);
        await vm.OpenPreviewCommand.ExecuteAsync(vm.Wallpapers.First());
        Assert.NotNull(vm.LargePreview); Assert.False(vm.PreviewFailed);
        vm.ClosePreviewCommand.Execute(null);
        vm.ShowDiscoverCommand.Execute(null);
        Assert.Same(discover, Assert.Single(vm.Wallpapers)); Assert.True(discover.IsSelected);
        vm.Shutdown();
    }

    [AvaloniaFact]
    public async Task LocalActionsUseActualFileRegardlessOfDownloadResolutionAndRequireDeleteConfirmation()
    {
        using var scope = new TestScope(); using var http = Offline();
        var dialogs = new LocalDialogs(); var vm = Create(http, scope.Path, dialogs);
        var path = Path.Combine(scope.Path, "my downloaded_UHD.jpg");
        var other = Path.Combine(scope.Path, "keep.jpg");
        File.WriteAllBytes(path, TestImages.Jpeg); File.WriteAllBytes(other, TestImages.Jpeg);
        await vm.ShowCollectionCommand.ExecuteAsync(null);
        var local = vm.Wallpapers.Single(x => x.LocalPath == path);
        vm.SelectedResolution = vm.Resolutions.Single(x => x.Value == "1366x768");
        await vm.OpenImagePathCommand.ExecuteAsync(local);
        Assert.Equal(path, dialogs.ShownPath);
        await vm.DeleteLocalCommand.ExecuteAsync(local);
        Assert.True(File.Exists(path)); Assert.Contains(local, vm.Wallpapers);
        dialogs.Confirm = true;
        await vm.DeleteLocalCommand.ExecuteAsync(local);
        Assert.False(File.Exists(path)); Assert.True(File.Exists(other)); Assert.DoesNotContain(local, vm.Wallpapers);
        Assert.False(vm.HasError);
        vm.Shutdown();
    }

    [AvaloniaFact]
    public async Task FolderChangeReloadsCollectionAndIgnoredStateSurvivesReopening()
    {
        using var scope = new TestScope(); using var http = Offline();
        var second = Path.Combine(scope.Path, "second"); Directory.CreateDirectory(second);
        var firstPath = Path.Combine(scope.Path, "first.jpg"); File.WriteAllBytes(firstPath, TestImages.Jpeg);
        File.WriteAllBytes(Path.Combine(second, "second.jpg"), TestImages.Jpeg);
        var settings = new TestSettings { Value = new() { WallpaperPath = scope.Path, Language = "en" } };
        var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http), settings, new TestDialogs());
        await vm.ShowCollectionCommand.ExecuteAsync(null);
        vm.ToggleIgnoreCommand.Execute(Assert.Single(vm.Wallpapers));
        Assert.True(vm.Wallpapers[0].IsIgnored);
        vm.Shutdown();
        var reopened = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http), settings, new TestDialogs());
        await reopened.ShowCollectionCommand.ExecuteAsync(null);
        Assert.True(Assert.Single(reopened.Wallpapers).IsIgnored);
        reopened.TargetFolder = second;
        // Changing folders starts a local scan. Wait for its UI-thread continuation.
        while (reopened.IsRunning) await Task.Delay(10, TestContext.Current.CancellationToken);
        Assert.Equal("second", Assert.Single(reopened.Wallpapers).Title);
        Assert.False(reopened.Wallpapers[0].IsIgnored); Assert.False(reopened.HasError);
        reopened.Shutdown();
    }

    [AvaloniaFact]
    public async Task CancelingLocalScanKeepsLoadedImagesAndCanBeRefreshed()
    {
        using var scope = new TestScope(); using var http = Offline();
        for (var i = 0; i < 4; i++) File.WriteAllBytes(Path.Combine(scope.Path, $"image{i}.jpg"), TestImages.Jpeg);
        var vm = Create(http, scope.Path);
        var canceled = false;
        vm.Wallpapers.CollectionChanged += (_, _) =>
        {
            if (vm.IsCollectionView && vm.Wallpapers.Count == 1 && !canceled)
            { canceled = true; vm.StartStopCommand.Execute(null); }
        };
        await vm.ShowCollectionCommand.ExecuteAsync(null);
        Assert.True(canceled); Assert.Single(vm.Wallpapers); Assert.False(vm.IsRunning); Assert.False(vm.HasError);
        await vm.StartStopCommand.ExecuteAsync(null);
        Assert.Equal(4, vm.Wallpapers.Count); Assert.False(vm.HasError);
        vm.Shutdown();
    }

    [AvaloniaFact]
    public async Task ChangingLanguageAndRegionInCollectionKeepsLocalItemsAndResetsDiscovery()
    {
        using var scope = new TestScope(); using var http = Offline();
        File.WriteAllBytes(Path.Combine(scope.Path, "photo.jpg"), TestImages.Jpeg);
        var vm = Create(http, scope.Path);
        vm.Wallpapers.Add(new(new Wallpaper("https://example.test/image.jpg", "Old region", "20261022")));
        await vm.ShowCollectionCommand.ExecuteAsync(null);
        var local = Assert.Single(vm.Wallpapers);
        vm.SelectedMarket = vm.Markets.Single(x => x.Value == "en-GB");
        vm.SelectedLanguage = vm.Languages.Single(x => x.Value == "de");
        Assert.Same(local, Assert.Single(vm.Wallpapers));
        Assert.Equal("Quelle: Lokale Datei", local.SourceLabel);
        Assert.Equal("Meine Sammlung · 1 Bild", vm.GallerySubtitle);
        vm.ShowDiscoverCommand.Execute(null);
        Assert.Empty(vm.Wallpapers); Assert.DoesNotContain("Sammlung", vm.StatusMessage);
        await vm.ShowCollectionCommand.ExecuteAsync(null);
        Assert.Same(local, Assert.Single(vm.Wallpapers));
        vm.Shutdown();
    }

    [AvaloniaTheory]
    [InlineData("de", false)]
    [InlineData("en", true)]
    public async Task CollectionLayoutShowsNavigationAndHidesDownloadControls(string language, bool dark)
    {
        using var scope = new TestScope(); using var http = Offline();
        var oldTheme = Avalonia.Application.Current!.RequestedThemeVariant;
        Avalonia.Application.Current.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        File.WriteAllBytes(Path.Combine(scope.Path, "My local wallpaper.jpg"), TestImages.Jpeg);
        var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http),
            new TestSettings { Value = new() { WallpaperPath = scope.Path, Language = language, Window = new(760, 700) } }, new TestDialogs());
        var window = new MainWindow { DataContext = vm }; window.Show();
        try
        {
            await vm.ShowCollectionCommand.ExecuteAsync(null);
            Dispatcher.UIThread.RunJobs();
            var checkboxes = window.GetVisualDescendants().OfType<CheckBox>().ToArray();
            Assert.All(checkboxes, box => Assert.False(box.IsEffectivelyVisible));
            var buttons = window.GetVisualDescendants().OfType<Button>().Where(b => b.IsEffectivelyVisible).ToArray();
            Assert.Contains(buttons, b => Equals(b.Content, language == "en" ? "Discover" : "Entdecken"));
            Assert.Contains(buttons, b => Equals(b.Content, language == "en" ? "My collection" : "Meine Sammlung"));
            if (Environment.GetEnvironmentVariable("BINGWALLE_UI_SNAPSHOTS") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory); AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);
                using var frame = window.CaptureRenderedFrame();
                frame!.Save(Path.Combine(directory, $"collection-{language}.png"), PngBitmapEncoderOptions.Default);
            }
        }
        finally { window.Close(); Avalonia.Application.Current.RequestedThemeVariant = oldTheme; }
    }

    private static HttpClient Offline() => new(new Handler((_, _) => throw new Exception("Unexpected network request")));
    private static MainWindowViewModel Create(HttpClient http, string folder, IAppDialogs? dialogs = null) =>
        new(new BingWallpaperClient(http), new WallpaperStore(http),
            new TestSettings { Value = new() { WallpaperPath = folder, Language = "en" } }, dialogs ?? new TestDialogs());
    private sealed class LocalDialogs : IAppDialogs
    {
        public bool Confirm { get; set; }
        public string? ShownPath { get; private set; }
        public Task<bool> ConfirmDeleteAsync(string path) => Task.FromResult(Confirm);
        public Task ShowFileAsync(string path) { ShownPath = path; return Task.CompletedTask; }
        public Task OpenFolderAsync(string folder) => Task.CompletedTask;
        public void ApplyTheme(string theme) { }
        public Task<string?> SelectFolderAsync() => Task.FromResult<string?>(null);
        public Task ShowInfoAsync(string title) => Task.CompletedTask;
    }
}
