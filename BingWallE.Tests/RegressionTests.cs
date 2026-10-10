using System.Net;
using BingWallE.Models;
using BingWallE.ViewModels;
using BingWallE.Services;
using Xunit;

namespace BingWallE.Tests;

public class RegressionTests
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    [Fact]
    public Task FileNames()
    {
        Check(WallpaperFileName.Create("20261009", "Blue Sky") == "20261009_Blue_Sky.jpg", "Ordinary names changed");
        var name = WallpaperFileName.Create("../../", "CON: a/b\\c?*<>|\"\n");
        Check(!name.Any(c => char.IsControl(c) || "<>:\"/\\|?*".Contains(c)), "Unsafe character");
        Check(name == WallpaperFileName.Create("../../", "CON: a/b\\c?*<>|\"\n"), "Unstable name");
        Check(WallpaperFileName.Create("20261009", "A:B") != WallpaperFileName.Create("20261009", "A?B"), "Sanitized names collide");
        Check(WallpaperFileName.Create("20261009", new string('x', 500)).Length < 200, "Name too long");
        Check(System.Text.Encoding.UTF8.GetByteCount(WallpaperFileName.Create("20261009", new string('界', 500))) < 255, "Unicode filename too long");
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CancelScan()
    {
        using var scope = new TestScope();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new HttpClient(new Handler(async (_, ct) =>
        {
            entered.TrySetResult();
            await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var vm = CreateVm(client, scope.Path);
        var scan = vm.StartStopCommand.ExecuteAsync(null);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Check(vm.StartStopCommand.CanExecute(null), "Stop disabled");
        Check(!vm.DownloadSelectedCommand.CanExecute(null) && !vm.SelectFolderCommand.CanExecute(null), "Conflicting commands enabled");
        await vm.StartStopCommand.ExecuteAsync(null);
        await scan.WaitAsync(TimeSpan.FromSeconds(5));
        Check(!vm.IsRunning && vm.StatusMessage.Contains("abgebrochen"), "Scan not canceled");
        Check(vm.DownloadSelectedCommand.CanExecute(null) && vm.SelectFolderCommand.CanExecute(null), "Commands not restored");
    }

    [Fact]
    public async Task CancelDownload()
    {
        using var scope = new TestScope();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new HttpClient(new Handler(async (_, ct) =>
        {
            entered.TrySetResult();
            await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var vm = MakeVm(client, scope.Path);
        var path = System.IO.Path.Combine(scope.Path, WallpaperFileName.Create("20261009", "Test: image?"));
        await File.WriteAllTextAsync(path, "original");
        var download = vm.DownloadSelectedCommand.ExecuteAsync(null);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await vm.StartStopCommand.ExecuteAsync(null);
        await download.WaitAsync(TimeSpan.FromSeconds(5));
        Check(await File.ReadAllTextAsync(path) == "original", "Existing image modified");
        Check(vm.Wallpapers[0].IsSelected && !vm.IsRunning, "Selection/state lost");
        Check(vm.StatusMessage.Contains("abgebrochen"), "Missing cancellation status");
        Check(!Directory.GetFiles(scope.Path, "*.tmp").Any(), "Temporary file left behind");
    }

    [Fact]
    public async Task HttpFailure()
    {
        using var scope = new TestScope();
        var fail = true;
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(fail
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : Response())));
        var vm = MakeVm(client, scope.Path);
        await vm.DownloadSelectedCommand.ExecuteAsync(null);
        Check(!vm.IsRunning && vm.Wallpapers[0].IsSelected && vm.StatusMessage.StartsWith("Fehler:"), "HTTP error not handled");
        Check(Directory.GetFiles(scope.Path).Length == 0, "Failed request produced file");
        fail = false;
        await vm.DownloadSelectedCommand.ExecuteAsync(null);
        Check(!vm.Wallpapers[0].IsSelected, "Retry failed");
    }

    [Fact]
    public async Task FileFailure()
    {
        using var scope = new TestScope();
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(Response())));
        var vm = MakeVm(client, scope.Path);
        var destination = System.IO.Path.Combine(scope.Path, WallpaperFileName.Create("20261009", "Test: image?"));
        Directory.CreateDirectory(destination); // Forces the final rename to fail after the temporary write.
        await vm.DownloadSelectedCommand.ExecuteAsync(null);
        Check(vm.StatusMessage.StartsWith("Fehler:") && vm.Wallpapers[0].IsSelected && !vm.IsRunning, "Write failure not handled");
        Check(!Directory.GetFiles(scope.Path, "*.tmp").Any(), "Temporary file not cleaned");
    }

    [Fact]
    public async Task PartialBatch()
    {
        using var scope = new TestScope();
        var count = 0;
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(++count == 1
            ? Response() : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));
        var vm = MakeVm(client, scope.Path);
        vm.Wallpapers.Add(new ImageItemViewModel("https://example.test/second.jpg", "Second", "20261008") { IsSelected = true });
        await vm.DownloadSelectedCommand.ExecuteAsync(null);
        Check(!vm.Wallpapers[0].IsSelected && vm.Wallpapers[1].IsSelected, "Partial success not retained");
        Check(vm.StatusMessage.Contains("1 von 2") && Directory.GetFiles(scope.Path).Length == 1, "Incorrect partial result");
    }

    [Fact]
    public async Task Success()
    {
        using var scope = new TestScope();
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(Response())));
        var target = System.IO.Path.Combine(scope.Path, "new-folder");
        var vm = MakeVm(client, target);
        await vm.DownloadSelectedCommand.ExecuteAsync(null);
        var path = System.IO.Path.Combine(target, WallpaperFileName.Create("20261009", "Test: image?"));
        Check(File.Exists(path) && (await File.ReadAllBytesAsync(path)).SequenceEqual(new byte[] { 1, 2, 3 }), "Incorrect image bytes or path");
        Check(!vm.Wallpapers[0].IsSelected && !vm.IsRunning, "State not reset");
        Check(!Directory.GetFiles(target, "*.tmp").Any(), "Temporary file left behind");
    }

    [Fact]
    public async Task Timeout()
    {
        using var scope = new TestScope();
        using var client = new HttpClient(new Handler((_, _) => throw new TaskCanceledException("timeout")));
        var vm = MakeVm(client, scope.Path);
        await vm.DownloadSelectedCommand.ExecuteAsync(null);
        Check(vm.StatusMessage.StartsWith("Zeitüberschreitung") && !vm.IsRunning, "Timeout confused with user cancellation");
    }

    static MainWindowViewModel CreateVm(HttpClient client, string path)
    {
        var settings = new TestSettings { Value = new AppSettings { WallpaperPath = path } };
        return new MainWindowViewModel(new BingWallpaperClient(client), new WallpaperStore(client), settings, new TestDialogs());
    }
    static MainWindowViewModel MakeVm(HttpClient client, string path)
    {
        var vm = CreateVm(client, path);
        vm.Wallpapers.Add(new ImageItemViewModel("https://example.test/image.jpg", "Test: image?", "20261009") { IsSelected = true });
        return vm;
    }
    static HttpResponseMessage Response() => new(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 1, 2, 3 }) };


}

sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
}
sealed class TestScope : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BingWallE-tests-" + Guid.NewGuid().ToString("N"));
    public TestScope() => Directory.CreateDirectory(Path);
    public void Dispose() => Directory.Delete(Path, true);
}

sealed class TestSettings : ISettingsStore
{
    public AppSettings Value { get; set; } = new();
    public AppSettings Load() => Value;
    public void Save(AppSettings settings) => Value = settings;
}
sealed class TestDialogs : IAppDialogs
{
    public Task OpenFolderAsync(string folder) => Task.CompletedTask;
    public void ApplyTheme(string theme) { }
    public string? Folder { get; set; }
    public Task<string?> SelectFolderAsync() => Task.FromResult(Folder);
    public Task ShowInfoAsync(string title) => Task.CompletedTask;
}
