using BingWallE.Services;
using Xunit;

namespace BingWallE.Tests;

public class ApplicationPathTests
{
    [Fact]
    public void SnapSettingsAndModelsUsePersistentCommonDirectory()
    {
        var common = Path.Combine(Path.GetTempPath(), "bingwalle-common");
        Assert.Equal(Path.Combine(common, "config/bingwalle/settings.json"), ApplicationPaths.InSnapCommon(common, "config/bingwalle/settings.json", "ordinary"));
        Assert.Equal("ordinary", ApplicationPaths.InSnapCommon(null, "models", "ordinary"));
        Assert.Equal("ordinary", ApplicationPaths.InSnapCommon("relative", "models", "ordinary"));
    }

    [Fact]
    public void SnapPicturesUseHostHomeInsteadOfPrivateRevisionHome()
    {
        var real = Path.Combine(Path.GetTempPath(), "user");
        var revision = Path.Combine(real, "snap", "bingwalle", "42");
        Assert.Equal(Path.Combine(real, "Pictures"), ApplicationPaths.SnapPictures(Path.Combine(revision, "Pictures"), revision, real));
        var localized = Path.Combine(real, "Bilder");
        Assert.Equal(localized, ApplicationPaths.SnapPictures(localized, revision, real));
    }
}
