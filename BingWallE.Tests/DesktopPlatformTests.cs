using BingWallE.Services;
using Xunit;

namespace BingWallE.Tests;

public class DesktopPlatformTests
{
    [Theory]
    [InlineData(null, LinuxDisplayBackend.Auto)]
    [InlineData("", LinuxDisplayBackend.Auto)]
    [InlineData("auto", LinuxDisplayBackend.Auto)]
    [InlineData(" X11 ", LinuxDisplayBackend.X11)]
    [InlineData("wayland", LinuxDisplayBackend.Wayland)]
    public void BackendOverrideAcceptsDocumentedValues(string? value, LinuxDisplayBackend expected)
        => Assert.Equal(expected, DesktopPlatform.ParseBackend(value));

    [Fact]
    public void InvalidOverrideHasActionableError()
        => Assert.Contains(DesktopPlatform.BackendVariable,
            Assert.Throws<ArgumentException>(() => DesktopPlatform.ParseBackend("wl")).Message);
}
