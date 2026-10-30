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
    throw new ArgumentException("Usage: StoreScreenshots fetch <cache> | render <cache> <output> <model-directory>");
var cache = Path.GetFullPath(args[1]);
if (args[0] == "fetch")
{
    Directory.CreateDirectory(cache);
    using var download = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(5));
    var images = new List<Wallpaper>();
    await foreach (var item in new BingWallpaperClient(download).ScanAsync("en-US", deadline.Token))
    {
        await File.WriteAllBytesAsync(Path.Combine(cache, $"image-{images.Count}.jpg"), item.PreviewBytes, deadline.Token);
        var full = await download.GetByteArrayAsync(item.Image.Url, deadline.Token);
        await File.WriteAllBytesAsync(Path.Combine(cache, $"image-{images.Count}-full.jpg"), full, deadline.Token);
        images.Add(item.Image);
        Console.WriteLine($"Cached real Bing image: {item.Image.Title}");
    }
    if (images.Count == 0) throw new InvalidOperationException("Bing returned no images.");
    await File.WriteAllTextAsync(Path.Combine(cache, "images.json"), JsonSerializer.Serialize(images));
    await File.WriteAllTextAsync(Path.Combine(cache, "fetched-at.txt"), DateTimeOffset.UtcNow.ToString("O"));
    return;
}
if (args.Length != 4 || !OperatingSystem.IsLinux())
    throw new ArgumentException("Render must run on Linux with cache, output and cached-model paths.");
var output = Path.GetFullPath(args[2]);
var modelDirectory = Path.GetFullPath(args[3]);
if (!File.Exists(Path.Combine(modelDirectory, "mobilenetv2-7.onnx")))
    throw new FileNotFoundException("Provide the existing verified recognition model; rendering must stay offline.");
Directory.CreateDirectory(output);
var wallpapers = JsonSerializer.Deserialize<Wallpaper[]>(File.ReadAllText(Path.Combine(cache, "images.json")))!;

// Render real controls on the Linux platform. Never access personal preferences or pictures.
AppBuilder.Configure<BingWallE.App>().UseSkia().UseHarfBuzz().WithInterFont()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
AvaloniaSynchronizationContext.InstallIfNeeded();
using var done = new CancellationTokenSource();
Exception? failure = null;
Dispatcher.UIThread.Post(async () =>
{
    try { await Render(); }
    catch (Exception ex) { failure = ex; }
    finally { done.Cancel(); }
});
Dispatcher.UIThread.MainLoop(done.Token);
if (failure != null) throw new InvalidOperationException("Screenshot rendering failed.", failure);

async Task Render()
{
    var sampleFolder = Directory.CreateTempSubdirectory("bingwalle-store-").FullName;
    using var http = new HttpClient(new OfflineHttp());
    using var tagger = new LocalImageTagger(modelDirectory);
    var store = new WallpaperStore(http);
    var vm = new MainWindowViewModel(new BingWallpaperClient(http), store,
        new ScreenshotSettings(sampleFolder), new NoDialogs(), tagger);
    MainWindow? gallery = null;
    try
    {
        for (var i = 0; i < wallpapers.Length; i++)
        {
            // Real previously downloaded files, with the app's normal metadata writer.
            var full = await File.ReadAllBytesAsync(Path.Combine(cache, $"image-{i}-full.jpg"));
            await File.WriteAllBytesAsync(store.GetPath(sampleFolder, wallpapers[i]),
                PhotoMetadataWriter.Write(full, wallpapers[i], []));
            using var stream = File.OpenRead(Path.Combine(cache, $"image-{i}.jpg"));
            vm.Wallpapers.Add(new ImageItemViewModel(wallpapers[i], new Bitmap(stream)) { IsSaved = true });
        }
        vm.GallerySubtitle = vm.Text.Get("{0} · {1} Bilder · {2}", vm.SelectedMarket.Label, vm.Wallpapers.Count, "Bing");
        vm.StatusMessage = vm.Text.Get("Suche abgeschlossen. Bilder zum Download auswählen.");
        gallery = new MainWindow { DataContext = vm };
        gallery.Show();
        Capture(gallery, "gallery-light", ThemeVariant.Light);
        Capture(gallery, "gallery-dark", ThemeVariant.Dark);
        await vm.ShowCollectionCommand.ExecuteAsync(null);
        Capture(gallery, "collection-light", ThemeVariant.Light);

        // Prefer a caption with useful text keywords; inference uses the real cached ONNX model.
        var selected = vm.Wallpapers.OrderByDescending(x => WallpaperKeywords.Build(x.Image, []).Count).First();
        await vm.OpenPreviewCommand.ExecuteAsync(selected);
        await vm.RecognizePreviewKeywordsCommand.ExecuteAsync(null);
        vm.KeywordInput = "My favorites";
        vm.AddPreviewKeywordCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        var preview = gallery.OwnedWindows.OfType<PreviewWindow>().Single();
        // A taller, genuinely resized preview shows credits, chips and save controls together.
        preview.Width = 1280; preview.Height = 1280;
        Capture(preview, "keywords-dark", ThemeVariant.Dark);
        preview.Close();
        await File.WriteAllTextAsync(Path.Combine(output, "provenance.txt"),
            $"BingWallE {AppInfo.Version}; {System.Runtime.InteropServices.RuntimeInformation.OSDescription}; " +
            "English UI, en-US image source. Actual Avalonia controls rendered using the Linux headless platform. " +
            "Real cached Bing photographs, captions and photographer credits; no generated images or UI mockups. " +
            "Collection populated from isolated temporary JPEG downloads with real embedded metadata. " +
            "Keywords use actual text extraction and cached MobileNetV2 inference; My favorites is a manually entered sample keyword, not a model result. " +
            "No OS window decorations. Not a Mint desktop capture or a Flatpak sandbox test. " +
            $"Photographs fetched at {File.ReadAllText(Path.Combine(cache, "fetched-at.txt")).Trim()}.\n");
        Console.WriteLine("Rendered English Linux UI: gallery light/dark, My collection and keyword editing.");
    }
    finally
    {
        gallery?.Close(); vm.Shutdown();
        // This exact directory was created by CreateTempSubdirectory above for this run only.
        Directory.Delete(sampleFolder, recursive: true);
    }
}

void Capture(Window window, string name, ThemeVariant theme)
{
    Application.Current!.RequestedThemeVariant = theme;
    Dispatcher.UIThread.RunJobs();
    AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);
    using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No rendered frame.");
    frame.Save(Path.Combine(output, name + ".png"), PngBitmapEncoderOptions.Default);
    Console.WriteLine($"Captured {name}: {frame.PixelSize.Width} x {frame.PixelSize.Height}");
}

sealed class ScreenshotSettings(string folder) : ISettingsStore
{
    public AppSettings Load() => new()
    {
        Language = "en", Market = "en-US", WallpaperPath = folder,
        Window = new(1280, 960), PreviewWindow = new(1280, 960)
    };
    public void Save(AppSettings settings) { }
}
sealed class OfflineHttp : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        => throw new InvalidOperationException("Rendering must not make HTTP requests: " + request.RequestUri);
}
sealed class NoDialogs : IAppDialogs
{
    public Task OpenFolderAsync(string path) => Task.CompletedTask;
    public Task<string?> SelectFolderAsync() => Task.FromResult<string?>(null);
    public Task ShowInfoAsync(string title) => Task.CompletedTask;
    public void ApplyTheme(string value) { }
}
