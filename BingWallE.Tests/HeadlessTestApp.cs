using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;

// UI tests temporarily install global Avalonia services; do not overlap ordinary view-model tests.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

[assembly: AvaloniaTestApplication(typeof(BingWallE.Tests.HeadlessTestApp))]

namespace BingWallE.Tests;

public static class HeadlessTestApp
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia().WithInterFont().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
