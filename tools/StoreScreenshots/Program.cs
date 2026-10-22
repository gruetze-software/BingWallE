using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using BingWallE.Views;

if (args.Length < 2 || args[0] is not ("fetch" or "render"))
    throw new ArgumentException("Usage: StoreScreenshots fetch <cache> | render <cache> <output>");
var cache = Path.GetFullPath(args[1]);
if (args[0] == "fetch")
{
    Directory.CreateDirectory(cache);
    using var download = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));
    var images = new List<Wallpaper>();
    await foreach (var item in new BingWallpaperClient(download).ScanAsync("en-US", deadline.Token))
    {
        await File.WriteAllBytesAsync(Path.Combine(cache, $"image-{images.Count}.jpg"), item.PreviewBytes, deadline.Token);
        images.Add(item.Image);
        Console.WriteLine($"Cached real Bing image: {item.Image.Title}");
    }
    if (images.Count == 0) throw new InvalidOperationException("Bing returned no images.");
    await File.WriteAllTextAsync(Path.Combine(cache, "images.json"), JsonSerializer.Serialize(images));
    return;
}
if (args.Length != 3 || !OperatingSystem.IsLinux())
    throw new ArgumentException("Render must run on Linux with cache and output paths.");

// Keep rendering on the initializing thread; never read or save the user's settings.
var output = Path.GetFullPath(args[2]);
Directory.CreateDirectory(output);
var wallpapers = JsonSerializer.Deserialize<Wallpaper[]>(File.ReadAllText(Path.Combine(cache, "images.json")))!;
AppBuilder.Configure<BingWallE.App>().UseSkia().UseHarfBuzz().WithInterFont()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
using var http = new HttpClient();
var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http), new ScreenshotSettings(), new NoDialogs());
for (var i = 0; i < wallpapers.Length; i++)
{
    using var stream = File.OpenRead(Path.Combine(cache, $"image-{i}.jpg"));
    vm.Wallpapers.Add(new ImageItemViewModel(wallpapers[i], new Bitmap(stream)) { IsSelected = true });
}
vm.GallerySubtitle = vm.Text.Get("{0} · {1} Bilder · aktuelle Bing-Auswahl", vm.SelectedMarket.Label, vm.Wallpapers.Count);
vm.StatusMessage = vm.Text.Get("Suche abgeschlossen. Bilder zum Download auswählen.");
var gallery = new MainWindow { DataContext = vm, Width = 1280, Height = 900 };
gallery.Show();
void Capture(Window window, string name, ThemeVariant theme)
{
    Application.Current!.RequestedThemeVariant = theme;
    Dispatcher.UIThread.RunJobs();
    AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);
    using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No rendered frame.");
    frame.Save(Path.Combine(output, name + ".png"), PngBitmapEncoderOptions.Default);
}
Capture(gallery, "gallery-light", ThemeVariant.Light);
Capture(gallery, "gallery-dark", ThemeVariant.Dark);
var settings = new SettingsWindow { DataContext = vm };
settings.Show();
Capture(settings, "settings", ThemeVariant.Light);
settings.Close();
var about = new AboutWindow(vm.Text);
about.Show();
Capture(about, "about", ThemeVariant.Light);
about.Close();
gallery.Close();
File.WriteAllText(Path.Combine(output, "provenance.txt"),
    $"BingWallE {AppInfo.Version}; {System.Runtime.InteropServices.RuntimeInformation.OSDescription}; " +
    "actual Avalonia UI rendered using the Linux headless platform, with cached real en-US Bing captions and previews. " +
    "No OS window decorations. Not a Mint desktop capture or a Flatpak sandbox test.\n");
Console.WriteLine("Rendered actual Linux UI: gallery light/dark, settings and about.");

sealed class ScreenshotSettings : ISettingsStore
{
    public AppSettings Load() => new()
    {
        Language = "en", Market = "en-US",
        WallpaperPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Pictures", "BingImages")
    };
    public void Save(AppSettings settings) { }
}
sealed class NoDialogs : IAppDialogs
{
    public Task OpenFolderAsync(string path) => Task.CompletedTask;
    public Task<string?> SelectFolderAsync() => Task.FromResult<string?>(null);
    public Task ShowInfoAsync(string title) => Task.CompletedTask;
    public void ApplyTheme(string value) { }
}
