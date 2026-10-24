using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Styling;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using BingWallE.Views;
using Xunit;

namespace BingWallE.Tests;

public class PreviewLayoutTests
{
    [AvaloniaTheory]
    [InlineData(1280, 960, "en")]
    [InlineData(640, 480, "de")]
    public void LongPreviewHasScrollbarClearanceAndKeepsLiveSaveStatusVisible(int width, int height, string language)
    {
        var previousTheme = Application.Current!.RequestedThemeVariant;
        Application.Current.RequestedThemeVariant = language == "en" ? ThemeVariant.Dark : ThemeVariant.Light;
        using var http = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http),
            new TestSettings { Value = new() { Language = language } }, new TestDialogs());
        using var stream = new MemoryStream(TestImages.Jpeg);
        using var bitmap = new Bitmap(stream);
        var image = new Wallpaper("https://example.test/image.jpg", "Stokksnes Beach, Iceland", "",
            Source: WallpaperSource.WindowsSpotlight, Description: string.Concat(Enumerable.Repeat(
                "A long image description with information about the mountains, beach and surrounding landscape. ", 50)));
        var item = new ImageItemViewModel(image, bitmap) { IsSelected = true };
        item.SetLanguage(language);
        vm.PreviewItem = item;
        var window = new PreviewWindow { DataContext = vm, Width = width, Height = height };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            var card = window.FindControl<Border>("PreviewCard")!;
            var scroll = window.FindControl<ScrollViewer>("PreviewScroll")!;
            var description = window.FindControl<TextBlock>("PreviewDescription")!;
            var status = window.FindControl<TextBlock>("PreviewSaveStatus")!;
            var selection = window.FindControl<CheckBox>("PreviewDownloadSelection")!;
            Assert.True(window.CanResize);
            Assert.InRange(card.Bounds.Width, 1, window.ClientSize.Width);
            Assert.True(scroll.Extent.Height > scroll.Viewport.Height);
            var descriptionRight = description.TranslatePoint(new Point(description.Bounds.Width, 0), scroll)!.Value.X;
            Assert.True(descriptionRight <= scroll.Bounds.Width - 16, "Text overlaps the scrollbar area.");
            Assert.True(selection.IsEffectivelyVisible);
            item.IsSaved = true;
            Dispatcher.UIThread.RunJobs();
            Assert.False(selection.IsEffectivelyVisible);
            Assert.Equal(language == "en" ? "Already saved" : "Bereits gespeichert", status.Text);
            var statusBottom = status.TranslatePoint(new Point(0, status.Bounds.Height), window)!.Value.Y;
            Assert.InRange(statusBottom, 1, window.ClientSize.Height - 12);
            Capture(window, $"preview-saved-{width}-{language}");
            // Scrolling through long metadata must not move the fixed status footer.
            var statusTop = status.TranslatePoint(default, window)!.Value.Y;
            scroll.Offset = new Vector(0, scroll.Extent.Height);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(statusTop, status.TranslatePoint(default, window)!.Value.Y);
            item.IsSaved = false;
            item.DownloadError = "Test download failure";
            Dispatcher.UIThread.RunJobs();
            Assert.True(selection.IsEffectivelyVisible);
            Assert.Equal(language == "en" ? "Download failed" : "Download fehlgeschlagen", status.Text);
            Capture(window, $"preview-{width}-{language}");
        }
        finally { window.Close(); Application.Current!.RequestedThemeVariant = previousTheme; }
    }

    [AvaloniaFact]
    public void ResizingPreviewEnlargesImageAndKeepsItsAspectRatio()
    {
        using var http = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http), new TestSettings(), new TestDialogs());
        using var stream = new MemoryStream(TestImages.Jpeg);
        using var bitmap = new Bitmap(stream);
        vm.PreviewItem = new ImageItemViewModel(new Wallpaper("https://example.test/image.jpg", "Resize", ""), bitmap);
        vm.LargePreview = bitmap;
        var window = new PreviewWindow { DataContext = vm, Width = 640, Height = 480 };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            var frame = window.FindControl<Grid>("PreviewImageFrame")!;
            var image = window.FindControl<Image>("PreviewImage")!;
            var before = frame.Bounds.Size;
            window.Width = 1100;
            window.Height = 900;
            Dispatcher.UIThread.RunJobs();
            Assert.True(frame.Bounds.Width > before.Width);
            Assert.True(frame.Bounds.Height > before.Height);
            Assert.Equal(Avalonia.Media.Stretch.Uniform, image.Stretch);
            Assert.Same(bitmap, image.Source);
            Assert.True(image.Bounds.Width > 0 && image.Bounds.Height > 0);
            Capture(window, "preview-resized");
            // The image remains fixed while the independent metadata area scrolls.
            var imageTop = frame.TranslatePoint(default, window);
            window.FindControl<ScrollViewer>("PreviewScroll")!.Offset = new Vector(0, 1000);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(imageTop, frame.TranslatePoint(default, window));
        }
        finally { vm.LargePreview = null; window.Close(); }
    }

    private static void Capture(Window window, string name)
    {
        if (Environment.GetEnvironmentVariable("BINGWALLE_UI_SNAPSHOTS") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No rendered frame.");
        frame.Save(Path.Combine(directory, name + ".png"), PngBitmapEncoderOptions.Default);
    }
}
