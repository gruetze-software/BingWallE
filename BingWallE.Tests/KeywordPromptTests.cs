using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Headless.XUnit;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using SkiaSharp;
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
    public void PromptUsesAttachedImageAndSourceLanguage(string ui, string market, string keywordLanguage, string titlePrefix)
    {
        var text = new UiText(); text.SetLanguage(ui);
        var image = new Wallpaper("https://example.test/photo.jpg?id=42", "Two wolves", "", Market: market);
        var prompt = KeywordPromptBuilder.Build(image, "A winter scene", text);
        Assert.DoesNotContain(image.Url, prompt);
        Assert.Contains(ui == "en" ? "attached image" : "angehängte Bild", prompt);
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
        Assert.StartsWith("Analyze the attached image", prompt);
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
            Assert.Null(window.FindControl<Button>("CopyKeywordPromptButton"));
            Assert.DoesNotContain(window.GetLogicalDescendants().OfType<Button>(), b => Equals(b.Content, "Recognize keywords again"));
            vm.KeywordStatus = "";
            window.FindControl<Button>("CopyKeywordImageOnlyButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (var attempt = 0; attempt < 100 && vm.KeywordStatus.Length == 0; attempt++)
                await Task.Delay(10, TestContext.Current.CancellationToken);
            Assert.StartsWith("Image preview copied.", vm.KeywordStatus);
            Assert.Null(await window.Clipboard!.TryGetTextAsync());
            using var copiedImage = await window.Clipboard!.TryGetBitmapAsync();
            Assert.NotNull(copiedImage);
            Assert.Equal(new Avalonia.PixelSize(32, 24), copiedImage.PixelSize);
            vm.KeywordStatus = "";
            window.FindControl<Button>("CopyKeywordTextOnlyButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (var attempt = 0; attempt < 100 && vm.KeywordStatus.Length == 0; attempt++)
                await Task.Delay(10, TestContext.Current.CancellationToken);
            Assert.StartsWith("AI prompt copied.", vm.KeywordStatus);
            Assert.Contains("Analyze the attached image", await window.Clipboard!.TryGetTextAsync());
            Assert.Null(await window.Clipboard!.TryGetBitmapAsync());
            Assert.Equal(keywords, item.ExportKeywords().Keywords);
            Assert.Equal(bytes, File.ReadAllBytes(path));
            Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text?.StartsWith("1. Copy the image") == true);
        }
        finally { window.Close(); vm.Shutdown(); }
    }

    [AvaloniaTheory]
    [InlineData(2400, 1200, 1024, 512)]
    [InlineData(1200, 2400, 512, 1024)]
    [InlineData(32, 24, 32, 24)]
    public async Task ClipboardPreviewIsReducedIndependentAndSurvivesDisposingOriginal(int width, int height, int expectedWidth, int expectedHeight)
    {
        using var pixels = new SKBitmap(width, height);
        pixels.Erase(SKColors.CornflowerBlue);
        using var png = pixels.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(png.ToArray());
        var original = new Bitmap(stream);
        var window = new PreviewWindow(); window.Show();
        try
        {
            await window.Clipboard!.SetDataAsync(new KeywordClipboardData(original, "Test prompt"));
            original.Dispose();
            using var copied = await window.Clipboard!.TryGetBitmapAsync();
            Assert.NotNull(copied);
            Assert.Equal(new Avalonia.PixelSize(expectedWidth, expectedHeight), copied.PixelSize);
            Assert.Equal("Test prompt", await window.Clipboard!.TryGetTextAsync());
            using var saved = new MemoryStream();
            copied.Save(saved, PngBitmapEncoderOptions.Default);
            Assert.True(saved.Length > 0);
            await window.Clipboard!.ClearAsync();
        }
        finally { original.Dispose(); window.Close(); }
    }
}
