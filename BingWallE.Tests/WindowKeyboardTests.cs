using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using BingWallE.Views;
using Xunit;

namespace BingWallE.Tests;

public class WindowKeyboardTests
{
    [AvaloniaTheory]
    [InlineData(WindowState.Normal)]
    [InlineData(WindowState.Maximized)]
    public void FullscreenRestoresPreviousStateFromKeyboardOrCaptionButton(WindowState initial)
    {
        using var http = Offline();
        var settings = new TestSettings();
        var vm = Create(http, settings);
        var window = new MainWindow { DataContext = vm };
        window.Show();
        try
        {
            window.WindowState = initial;
            Press(window, Key.F11);
            Assert.Equal(WindowState.FullScreen, window.WindowState);
            Press(window, Key.F11);
            Assert.Equal(initial, window.WindowState);
            // The native titlebar can also enter fullscreen without our shortcut.
            window.WindowState = WindowState.FullScreen;
            Press(window, Key.Escape);
            Assert.Equal(initial, window.WindowState);
            Press(window, Key.Escape);
            Assert.True(window.IsVisible);
            Assert.Equal(initial, window.WindowState);
            Press(window, Key.F11);
        }
        finally { window.Close(); }
        Assert.Equal(initial == WindowState.Maximized, settings.Value.Window.IsMaximized);
    }

    [AvaloniaFact]
    public void EscapeClosesOwnedPreviewBeforeExitingMainFullscreen()
    {
        using var http = Offline();
        var vm = Create(http);
        var main = new MainWindow { DataContext = vm };
        main.Show();
        try
        {
            main.WindowState = WindowState.Maximized;
            Press(main, Key.F11);
            vm.PreviewItem = Photo();
            Dispatcher.UIThread.RunJobs();
            var preview = Assert.Single(main.OwnedWindows.OfType<PreviewWindow>());
            Press(preview, Key.F11);
            Assert.Equal(WindowState.FullScreen, preview.WindowState);
            Press(preview, Key.Escape);
            Assert.True(preview.IsVisible);
            Assert.Equal(WindowState.Normal, preview.WindowState);
            Press(preview, Key.Escape);
            Dispatcher.UIThread.RunJobs();
            Assert.False(vm.IsPreviewOpen);
            Assert.False(preview.IsVisible);
            Assert.Equal(WindowState.FullScreen, main.WindowState);
            main.Activate();
            Press(main, Key.Escape);
            Assert.Equal(WindowState.Maximized, main.WindowState);
        }
        finally { main.Close(); }
    }

    [AvaloniaFact]
    public void PreviewReusesWindowOnRetryAndClosesWithOwnerOrCommand()
    {
        using var http = Offline();
        var vm = Create(http);
        var main = new MainWindow { DataContext = vm };
        main.Show();
        try
        {
            vm.PreviewItem = Photo();
            Dispatcher.UIThread.RunJobs();
            var first = Assert.Single(main.OwnedWindows.OfType<PreviewWindow>());
            vm.PreviewItem = null;
            vm.PreviewItem = Photo();
            Dispatcher.UIThread.RunJobs();
            Assert.Same(first, Assert.Single(main.OwnedWindows.OfType<PreviewWindow>()));
            vm.ClosePreviewCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.False(first.IsVisible);
            vm.PreviewItem = Photo();
            Dispatcher.UIThread.RunJobs();
            Assert.True(Assert.Single(main.OwnedWindows.OfType<PreviewWindow>()).IsVisible);
        }
        finally { main.Close(); }
        Assert.False(vm.IsPreviewOpen);
    }

    [AvaloniaFact]
    public async Task ClosingPreviewCancelsPendingImageLoad()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = false;
        using var http = new HttpClient(new Handler(async (_, token) =>
        {
            started.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) { canceled = true; throw; }
            throw new Exception("Expected cancellation");
        }));
        var vm = Create(http);
        var main = new MainWindow { DataContext = vm };
        main.Show();
        try
        {
            var pending = vm.OpenPreviewCommand.ExecuteAsync(Photo());
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Dispatcher.UIThread.RunJobs();
            Assert.Single(main.OwnedWindows.OfType<PreviewWindow>()).Close();
            await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(canceled);
            Assert.False(vm.IsPreviewOpen);
            Assert.False(vm.PreviewFailed);
        }
        finally { main.Close(); }
    }

    private static MainWindowViewModel Create(HttpClient http, TestSettings? settings = null) =>
        new(new BingWallpaperClient(http), new WallpaperStore(http), settings ?? new TestSettings(), new TestDialogs());
    private static HttpClient Offline() => new(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
    private static ImageItemViewModel Photo() => new(new Wallpaper("https://example.test/image.jpg", "Test preview", ""));
    private static void Press(Window window, Key key)
    {
        var physical = key == Key.F11 ? PhysicalKey.F11 : PhysicalKey.Escape;
        window.KeyPress(key, RawInputModifiers.None, physical, "");
        if (window.IsVisible) window.KeyRelease(key, RawInputModifiers.None, physical, "");
        Dispatcher.UIThread.RunJobs();
    }
}
