using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using BingWallE.Views;

if (args.Length != 1 || args[0] is not ("Wayland" or "X11"))
    throw new ArgumentException("Usage: DesktopSmoke Wayland|X11");
Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
Trace.AutoFlush = true;
SmokeApplication.ExpectedBackend = args[0];
return DesktopPlatform.Configure(AppBuilder.Configure<SmokeApplication>())
    .WithInterFont().LogToTrace().StartWithClassicDesktopLifetime([]);

public class SmokeApplication : BingWallE.App
{
    public static string ExpectedBackend { get; set; } = "";

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new InvalidOperationException("Desktop lifetime missing.");
        // Deliberately bypass the production lifetime setup: no personal settings,
        // network requests, model downloads or user image folders are used.
        var http = new HttpClient(new NoNetwork());
        var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http),
            new MemorySettings(), new NoDialogs());
        var window = new MainWindow { DataContext = vm, Width = 800, Height = 820 };
        desktop.MainWindow = window;
        window.Opened += (_, _) => DispatcherTimer.RunOnce(() =>
        {
            var backend = DesktopPlatform.Describe(window);
            if (!(ExpectedBackend == "Wayland" ? backend.Contains("Wayland", StringComparison.OrdinalIgnoreCase) : backend.Contains("X11", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Expected {ExpectedBackend}, got {backend}.");
            // Exercise the real dialogs on the selected native backend as well.
            var settings = new SettingsWindow { DataContext = vm };
            settings.Show(window);
            var about = new AboutWindow(vm.Text);
            about.Show(window);
            DispatcherTimer.RunOnce(() =>
            {
                settings.Close(); about.Close();
                Console.WriteLine($"SMOKE PASS: {backend}; main window, Settings and About opened.");
                desktop.Shutdown();
            }, TimeSpan.FromSeconds(1));
        }, TimeSpan.FromSeconds(1));
        desktop.Exit += (_, _) => http.Dispose();
    }
}

sealed class MemorySettings : ISettingsStore
{
    public AppSettings Load() => new() { Language = "en", AutoRefreshOnStartup = false };
    public void Save(AppSettings settings) { }
}

sealed class NoNetwork : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Desktop smoke test must not make network requests.");
}

sealed class NoDialogs : IAppDialogs
{
    public void ApplyTheme(string theme) { }
    public Task<string?> SelectFolderAsync() => throw new NotSupportedException();
    public Task ShowInfoAsync(string title) => throw new NotSupportedException();
    public Task OpenFolderAsync(string path) => throw new NotSupportedException();
}
