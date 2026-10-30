using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using BingWallE.Views;
using Xunit;

namespace BingWallE.Tests;

public class WindowPlacementTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeMaximizeResizeArrivingBeforeStateChangeDoesNotOverwriteNormalBounds(bool preview)
    {
        using var http = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var settings = new TestSettings();
        var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http), settings, new TestDialogs());
        Window window = preview ? new PreviewWindow { DataContext = vm } : new MainWindow { DataContext = vm };
        window.Show();
        window.Width = 760; window.Height = 540;
        Dispatcher.UIThread.RunJobs();
        // Match the Win32 WM_SIZE -> WindowStateChanged notification order.
        // Avalonia exposes no public API for this native-only notification.
        typeof(Window).GetMethod("HandleResized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(window, [new Size(1920, 1009), WindowResizeReason.Unspecified]);
        window.WindowState = WindowState.Maximized;
        Dispatcher.UIThread.RunJobs();
        window.Close();
        Assert.Equal(new SavedWindowState(760, 540, true), preview ? settings.Value.PreviewWindow : settings.Value.Window);
    }

    [AvaloniaTheory]
    [InlineData(false, WindowState.Normal)]
    [InlineData(false, WindowState.Maximized)]
    [InlineData(false, WindowState.Minimized)]
    [InlineData(false, WindowState.FullScreen)]
    [InlineData(true, WindowState.Normal)]
    [InlineData(true, WindowState.Maximized)]
    [InlineData(true, WindowState.Minimized)]
    [InlineData(true, WindowState.FullScreen)]
    public void CloseAndReopenRestoresNormalBoundsAndMaximizedState(bool preview, WindowState closingState)
    {
        using var scope = new TestScope();
        using var http = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var store = new JsonSettingsStore(Path.Combine(scope.Path, "settings.json"));
        MainWindowViewModel Create() => new(new BingWallpaperClient(http), new WallpaperStore(http), store, new TestDialogs());
        Window Open(MainWindowViewModel vm)
        {
            Window w = preview ? new PreviewWindow { DataContext = vm } : new MainWindow { DataContext = vm };
            w.Show(); Dispatcher.UIThread.RunJobs(); return w;
        }
        var first = Open(Create());
        first.Width = 760; first.Height = 540;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(760, first.ClientSize.Width);
        Assert.Equal(540, first.ClientSize.Height);
        // Minimized/fullscreen windows should reopen in the state used before that transition.
        if (closingState != WindowState.Normal) first.WindowState = WindowState.Maximized;
        first.WindowState = closingState;
        first.Close();
        var second = Open(Create());
        try
        {
            var saved = preview ? store.Load().PreviewWindow : store.Load().Window;
            Assert.Equal(new SavedWindowState(760, 540, closingState != WindowState.Normal), saved);
            Assert.Equal(closingState == WindowState.Normal ? WindowState.Normal : WindowState.Maximized, second.WindowState);
            second.WindowState = WindowState.Normal;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(760, second.Width);
            Assert.Equal(540, second.Height);
        }
        finally { second.Close(); }
    }
}
