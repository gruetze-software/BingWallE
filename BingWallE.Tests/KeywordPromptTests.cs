using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using BingWallE.Localization;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using BingWallE.Views;
using Xunit;

namespace BingWallE.Tests;

public class KeywordPromptTests
{
    [Theory]
    [InlineData("en", "de-DE", "German", "Title:")]
    [InlineData("de", "en-US", "Englisch", "Titel:")]
    [InlineData("en", "", "English", "Title:")]
    [InlineData("de", "", "Deutsch", "Titel:")]
    public void PromptUsesOriginalImageUrlAndSourceLanguage(string ui, string market, string keywordLanguage, string titlePrefix)
    {
        var text = new UiText(); text.SetLanguage(ui);
        var image = new Wallpaper("https://example.test/photo.jpg?id=42", "Two wolves", "", Market: market);
        var prompt = KeywordPromptBuilder.Build(image, "A winter scene", text);
        Assert.Contains(image.Url, prompt);
        Assert.Contains(titlePrefix + " Two wolves", prompt);
        Assert.Contains("A winter scene", prompt);
        Assert.Contains(keywordLanguage, prompt);
        Assert.Contains(ui == "en" ? "semicolons" : "Semikolon", prompt);
        Assert.Contains(ui == "en" ? "do not guess" : "rate keine", prompt);
    }

    [Theory]
    [InlineData("file:///C:/private/secret-photo.jpg")]
    [InlineData("/home/user/private/photo.jpg")]
    [InlineData("http://localhost/photo.jpg")]
    [InlineData("")]
    public void MissingInternetUrlRequestsAttachmentWithoutSharingLocalPath(string url)
    {
        var text = new UiText(); text.SetLanguage("en");
        var prompt = KeywordPromptBuilder.Build(new Wallpaper(url, "My photo", "", Market: ""), "Landscape", text);
        Assert.StartsWith("Please attach the image", prompt);
        if (url.Length > 0) Assert.DoesNotContain(url, prompt);
        Assert.DoesNotContain("private", prompt);
    }

    [AvaloniaFact]
    public async Task PreviewButtonCopiesPromptOfflineWithoutChangingKeywordsOrFile()
    {
        using var scope = new TestScope();
        using var http = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var path = Path.Combine(scope.Path, "photo.jpg");
        var bytes = PhotoMetadataWriter.Write(TestImages.Jpeg,
            new Wallpaper("https://example.test/original.jpg", "Test image", "", Market: "en-US"), []);
        File.WriteAllBytes(path, bytes);
        var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http),
            new TestSettings { Value = new() { Language = "en", WallpaperPath = scope.Path } }, new TestDialogs());
        await vm.ShowCollectionCommand.ExecuteAsync(null);
        var item = Assert.Single(vm.Wallpapers);
        await vm.OpenPreviewCommand.ExecuteAsync(item);
        var keywords = item.ExportKeywords().Keywords;
        var window = new PreviewWindow { DataContext = vm }; window.Show();
        try
        {
            var button = window.FindControl<Button>("CopyKeywordPromptButton")!;
            Assert.True(button.IsEnabled);
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (var attempt = 0; attempt < 100 && vm.KeywordStatus.Length == 0; attempt++)
                await Task.Delay(10, TestContext.Current.CancellationToken);
            Assert.StartsWith("AI prompt copied.", vm.KeywordStatus);
            var copied = await window.Clipboard!.TryGetTextAsync();
            Assert.Contains("https://example.test/original.jpg", copied);
            Assert.Equal(keywords, item.ExportKeywords().Keywords);
            Assert.Equal(bytes, File.ReadAllBytes(path));
        }
        finally { window.Close(); vm.Shutdown(); }
    }
}
