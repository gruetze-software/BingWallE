using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using BingWallE.Views;
using Xunit;

namespace BingWallE.Tests;

public class GalleryToolbarTests
{
    [AvaloniaTheory]
    [InlineData(640, "de", false)]
    [InlineData(640, "en", true)]
    [InlineData(1040, "de", true)]
    [InlineData(1280, "en", false)]
    public async Task RegionAndRefreshShareNavigationRowWithoutOverlap(int width, string language, bool dark)
    {
        using var scope = new TestScope();
        using var http = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http),
            new TestSettings { Value = new() { Language = language, WallpaperPath = scope.Path, Window = new(width, 820) } }, new TestDialogs());
        vm.SelectedMarket = vm.Markets.Single(x => x.Value == "en-GB");
        var oldTheme = Application.Current!.RequestedThemeVariant;
        Application.Current.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        var window = new MainWindow { DataContext = vm }; window.Show();
        try
        {
            CheckLayout(); Capture("discover");
            await vm.ShowCollectionCommand.ExecuteAsync(null);
            CheckLayout(); Capture("collection");
            vm.ShowDiscoverCommand.Execute(null);
            CheckLayout();
        }
        finally { window.Close(); vm.Shutdown(); Application.Current.RequestedThemeVariant = oldTheme; }

        void CheckLayout()
        {
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(width, window.ClientSize.Width);
            var navigation = window.FindControl<Button>("CollectionNavigationButton")!;
            var summary = window.FindControl<Border>("SourceSummary")!;
            var toolbar = window.FindControl<Grid>("GalleryToolbar")!;
            var refresh = window.FindControl<Button>("RefreshButton")!;
            var navRight = navigation.TranslatePoint(new Point(navigation.Bounds.Width, navigation.Bounds.Height / 2), window)!.Value;
            var sourceLeft = summary.TranslatePoint(new Point(0, summary.Bounds.Height / 2), window)!.Value;
            Assert.InRange(sourceLeft.X - navRight.X, 10, 20);
            Assert.InRange(Math.Abs(sourceLeft.Y - navRight.Y), 0, 1);
            var right = summary.TranslatePoint(new Point(summary.Bounds.Width, 0), window)!.Value.X;
            Assert.InRange(right, 1, window.ClientSize.Width - 20);
            Assert.InRange(summary.Bounds.Width, 150, 340);
            Assert.InRange(toolbar.Bounds.Height, 36, 62);
            Assert.True(refresh.IsEffectivelyVisible);
            Assert.Same(vm.StartStopCommand, refresh.Command);
            if (vm.IsDiscoverView)
            {
                var top = window.FindControl<Grid>("GalleryViewport")!.TranslatePoint(default, window)!.Value.Y;
                Assert.InRange(top, 100, 230);
            }
        }

        void Capture(string view)
        {
            if (Environment.GetEnvironmentVariable("BINGWALLE_UI_SNAPSHOTS") is not { Length: > 0 } directory) return;
            Directory.CreateDirectory(directory); AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);
            using var frame = window.CaptureRenderedFrame();
            frame!.Save(Path.Combine(directory, $"toolbar-{view}-{language}-{width}.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
