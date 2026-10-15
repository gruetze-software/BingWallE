using System.Net;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using Xunit;

namespace BingWallE.Tests;

public class CollectionActionTests
{
    private static Wallpaper Photo => new("https://bing.com/test.jpg", "Test", "20261015", "/test");
    private static HttpClient Offline() => new(new Handler((_, _) => throw new Exception("Unexpected HTTP")));

    [Fact]
    public async Task StartupRefreshIsOptionalAndRunsOnlyOnce()
    {
        var requests = 0;
        using var client = new HttpClient(new Handler((_, _) =>
        {
            requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"images\":[]}") });
        }));
        var settings = new TestSettings();
        var vm = new MainWindowViewModel(new BingWallpaperClient(client), new WallpaperStore(client), settings, new TestDialogs());
        await vm.InitializeAsync(); Assert.Equal(0, requests);
        vm.AutoRefreshOnStartup = true;
        Assert.True(settings.Value.AutoRefreshOnStartup);
        var reopened = new MainWindowViewModel(new BingWallpaperClient(client), new WallpaperStore(client), settings, new TestDialogs());
        await reopened.InitializeAsync(); await reopened.InitializeAsync();
        Assert.Equal(1, requests); Assert.False(reopened.HasError);
    }

    [Fact]
    public void IgnoredImagesSurviveReopeningAndBulkSelectionAndCanBeRestored()
    {
        using var client = Offline();
        var settings = new TestSettings();
        MainWindowViewModel Create() => new(new BingWallpaperClient(client), new WallpaperStore(client), settings, new TestDialogs());
        var vm = Create(); var item = new ImageItemViewModel(Photo) { IsSelected = true }; vm.Wallpapers.Add(item);
        vm.ToggleIgnoreCommand.Execute(item);
        Assert.True(item.IsIgnored); Assert.False(item.IsSelected);
        vm.SelectAllCommand.Execute(null); Assert.False(item.IsSelected);
        vm.SelectNewCommand.Execute(null); Assert.False(item.IsSelected);
        var reopened = Create(); var second = new ImageItemViewModel(Photo); reopened.Wallpapers.Add(second);
        Assert.True(second.IsIgnored);
        reopened.ToggleIgnoreCommand.Execute(second);
        Assert.False(second.IsIgnored); Assert.True(second.IsSelected); Assert.Empty(settings.Value.IgnoredImages);
    }

    [Fact]
    public async Task DeleteRequiresConfirmationAndOnlyRemovesChosenResolution()
    {
        using var scope = new TestScope(); using var client = Offline();
        var store = new WallpaperStore(client);
        var dialogs = new FileDialogs();
        var vm = new MainWindowViewModel(new BingWallpaperClient(client), store,
            new TestSettings { Value = new() { WallpaperPath = scope.Path } }, dialogs);
        var item = new ImageItemViewModel(Photo); vm.Wallpapers.Add(item);
        var full = store.GetPath(scope.Path, Photo);
        var uhd = store.GetPath(scope.Path, WallpaperVariants.ForResolution(Photo, "UHD"));
        File.WriteAllText(full, "full HD"); File.WriteAllText(uhd, "UHD");
        vm.SelectedResolution = vm.Resolutions.Single(x => x.Value == "UHD");
        await vm.OpenImagePathCommand.ExecuteAsync(item); Assert.Equal(uhd, dialogs.ShownPath);
        await vm.DeleteLocalCommand.ExecuteAsync(item); Assert.True(File.Exists(uhd));
        dialogs.Confirm = true;
        await vm.DeleteLocalCommand.ExecuteAsync(item);
        Assert.False(File.Exists(uhd)); Assert.True(File.Exists(full));
        Assert.False(item.IsSaved); Assert.True(item.IsSelected); Assert.False(vm.HasError);
    }

    [Fact]
    public void WindowStateAndNewPreferencesSurviveOtherSettingChangesAndJsonRoundTrip()
    {
        using var scope = new TestScope(); using var client = Offline();
        var store = new JsonSettingsStore(Path.Combine(scope.Path, "settings.json"));
        var vm = new MainWindowViewModel(new BingWallpaperClient(client), new WallpaperStore(client), store, new TestDialogs());
        vm.AutoRefreshOnStartup = true;
        vm.SaveWindowState(new(900, 600, true));
        vm.SelectedTheme = vm.Themes.Single(x => x.Value == "Dark");
        var result = store.Load(); Assert.Equal(new SavedWindowState(900, 600, true), result.Window);
        Assert.True(result.AutoRefreshOnStartup);
    }

    [Theory]
    [InlineData(1040, 820, 1200, 650, 1040, 650)]
    [InlineData(1900, 1000, 750, 500, 750, 500)]
    [InlineData(-1, 0, 1200, 900, 1040, 820)]
    [InlineData(double.NaN, double.PositiveInfinity, 800, 600, 800, 600)]
    public void WindowDimensionsFitAvailableLogicalWorkArea(double width, double height, double availableWidth,
        double availableHeight, double expectedWidth, double expectedHeight)
    {
        var actual = WindowSizing.Fit(new(width, height, true), availableWidth, availableHeight);
        Assert.Equal(expectedWidth, actual.Width); Assert.Equal(expectedHeight, actual.Height); Assert.True(actual.IsMaximized);
    }

    private sealed class FileDialogs : IAppDialogs
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
