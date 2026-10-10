using System.Net;
using System.Text;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using Xunit;

namespace BingWallE.Tests;

public class ServiceTests
{
    [Fact]
    public async Task ScanMapsBingDataAndStopsAtEightDays()
    {
        var requests = new List<string>();
        using var client = new HttpClient(new Handler((request, _) =>
        {
            var url = request.RequestUri!.AbsoluteUri;
            requests.Add(url);
            return Task.FromResult(url.Contains("HPImageArchive")
                ? Json("""{"images":[{"url":"/image.jpg?id=1","title":"Blue sky","enddate":"20261009"}]}""")
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });
        }));
        var progress = new List<string>();
        var images = new List<WallpaperPreview>();
        await foreach (var image in new BingWallpaperClient(client).ScanAsync("de-DE", default, progress.Add)) images.Add(image);
        Assert.Equal(16, progress.Count);
        Assert.Contains("Tag 1 von 8", progress[0]);
        Assert.Contains("Bilddaten", progress[0]);
        Assert.Contains("Vorschau", progress[1]);
        Assert.Equal(8, images.Count);
        Assert.Equal(16, requests.Count);
        Assert.All(requests, url => Assert.Equal("bing.com", new Uri(url).Host));
        Assert.Contains("idx=7", requests[^2]);
        Assert.Contains("mkt=de-DE", requests[0]);
        Assert.Equal(new Wallpaper("https://bing.com/image.jpg?id=1", "Blue sky", "20261009"), images[0].Image);
        Assert.Equal(new byte[] { 1, 2, 3 }, images[0].PreviewBytes);
    }

    [Fact]
    public async Task EmptyArchiveStopsAndUsesFallbackMarket()
    {
        var count = 0;
        using var client = new HttpClient(new Handler((request, _) =>
        {
            count++;
            Assert.Contains("mkt=en-US", request.RequestUri!.Query);
            return Task.FromResult(Json("""{"images":[]}"""));
        }));
        await foreach (var _ in new BingWallpaperClient(client).ScanAsync("", default)) Assert.Fail("Expected no images");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task IncompleteArchiveFailsBeforePreviewRequest()
    {
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(Json("""{"images":[{"title":"Missing URL"}]}"""))));
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await foreach (var _ in new BingWallpaperClient(client).ScanAsync("de-DE", default)) { }
        });
    }

    [Fact]
    public void SettingsRoundTripCreatesDirectoryAndLeavesNoTemporaryFile()
    {
        using var scope = new TestScope();
        var file = Path.Combine(scope.Path, "config", "settings.json");
        var store = new JsonSettingsStore(file);
        Assert.Equal(new AppSettings().WallpaperPath, store.Load().WallpaperPath);
        Assert.False(Directory.Exists(Path.GetDirectoryName(file)));
        store.Save(new AppSettings { WallpaperPath = scope.Path });
        Assert.Equal(scope.Path, store.Load().WallpaperPath);
        store.Save(new AppSettings { WallpaperPath = Path.Combine(scope.Path, "other") });
        Assert.EndsWith("other", store.Load().WallpaperPath);
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(file)!));
    }

    [Theory]
    [InlineData("broken json")]
    [InlineData("null")]
    [InlineData("{\"WallpaperPath\":null}")]
    [InlineData("{\"WallpaperPath\":\" \"}")]
    public void InvalidSettingsUseDefault(string json)
    {
        using var scope = new TestScope();
        var file = Path.Combine(scope.Path, "settings.json");
        File.WriteAllText(file, json);
        Assert.Equal(new AppSettings().WallpaperPath, new JsonSettingsStore(file).Load().WallpaperPath);
    }

    [Fact]
    public async Task FolderSelectionPersistsAndRefreshesExistingImageSelection()
    {
        using var scope = new TestScope();
        using var client = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected network access")));
        var settings = new TestSettings();
        var dialogs = new TestDialogs { Folder = scope.Path };
        var vm = new MainWindowViewModel(new BingWallpaperClient(client), new WallpaperStore(client), settings, dialogs);
        var item = new ImageItemViewModel("https://example.test/a.jpg", "A", "20261009") { IsSelected = true };
        vm.Wallpapers.Add(item);
        File.WriteAllText(Path.Combine(scope.Path, WallpaperFileName.Create(item.Date, item.Title)), "existing");
        await vm.SelectFolderCommand.ExecuteAsync(null);
        Assert.Equal(scope.Path, vm.TargetFolder);
        Assert.Equal(scope.Path, settings.Value.WallpaperPath);
        Assert.False(item.IsSelected);
        dialogs.Folder = null;
        await vm.SelectFolderCommand.ExecuteAsync(null);
        Assert.Equal(scope.Path, vm.TargetFolder);
    }

    private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };
}
