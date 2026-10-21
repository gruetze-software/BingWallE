using System.Net;
using BingWallE.Models;
using BingWallE.Services;
using Xunit;

namespace BingWallE.Tests;

public class WallpaperFileNameTests
{
    [Theory]
    [InlineData("Hanakāpī'ai Beach, Hawaii")]
    [InlineData("Küste & Vögel – München")]
    [InlineData("日本語の風景")]
    [InlineData("Nukuʻalofa / 你好 👋")]
    [InlineData("CON: a/b\\c?*<>|\"\n")]
    public void GeneratedNamesOnlyContainAsciiLettersDigitsAndUnderscores(string title)
    {
        var filename = WallpaperFileName.Create("20261021", title);
        Assert.Matches(@"^[A-Za-z0-9_]+\.jpg$", filename);
        Assert.DoesNotContain("__", filename);
        Assert.Equal(filename, WallpaperFileName.Create("20261021", title));
    }

    [Fact]
    public void SpotlightTitleIsReadableWithoutAnExtraHashAndIdentityPreventsCollisions()
    {
        var photo = new Wallpaper("https://res.public.onecdn.static.microsoft/image.jpg", "Hanakāpī'ai Beach, Hawaii", "",
            Source: WallpaperSource.WindowsSpotlight, SourceId: "42");
        var filename = WallpaperVariants.FileName(photo);
        Assert.Matches(@"^Spotlight_[A-F0-9]{16}_Hanakapiai_Beach_Hawaii\.jpg$", filename);
        Assert.NotEqual(filename, WallpaperVariants.FileName(photo with { SourceId = "43" }));
        Assert.NotEqual(WallpaperFileName.Create("20261021", "A:B"), WallpaperFileName.Create("20261021", "A?B"));
    }

    [Fact]
    public async Task LegacyNamesRemainRecognizedButNewDownloadsUseCleanNames()
    {
        using var scope = new TestScope();
        var photo = new Wallpaper("https://res.public.onecdn.static.microsoft/image.jpg", "Hanakāpī'ai Beach, Hawaii", "",
            Source: WallpaperSource.WindowsSpotlight, SourceId: "42");
        var cleanName = WallpaperVariants.FileName(photo);
        var prefix = cleanName[..("Spotlight_".Length + 16)];
        var legacyPath = Path.Combine(scope.Path, prefix + "_Hanakāpī'ai_Beach,_Hawaii.jpg");
        await File.WriteAllTextAsync(legacyPath, "old image");
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new ByteArrayContent(TestImages.Jpeg) })));
        var store = new WallpaperStore(http);
        Assert.True(store.Exists(scope.Path, photo)); Assert.Equal(legacyPath, store.GetPath(scope.Path, photo));
        store.Delete(scope.Path, photo); Assert.False(File.Exists(legacyPath));
        await store.DownloadAsync(scope.Path, photo, default);
        Assert.Equal(Path.Combine(scope.Path, cleanName), store.GetPath(scope.Path, photo));
        Assert.True(store.Exists(scope.Path, photo)); Assert.Single(Directory.GetFiles(scope.Path));
    }
}
