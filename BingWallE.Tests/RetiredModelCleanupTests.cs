using BingWallE.Services;
using Xunit;

namespace BingWallE.Tests;

public class RetiredModelCleanupTests
{
    [Fact]
    public void RemovesOnlyRetiredModelAndItsInterruptedDownloadsWithoutRecursing()
    {
        using var scope = new TestScope();
        var model = Path.Combine(scope.Path, "mobilenetv2-7.onnx");
        var temporary = model + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(model, "retired"); File.WriteAllText(temporary, "partial");
        File.WriteAllText(Path.Combine(scope.Path, "my-other-model.onnx"), "keep");
        File.WriteAllText(Path.Combine(scope.Path, "mobilenetv2-7.onnx.personal.tmp"), "keep");
        var nested = Path.Combine(scope.Path, "nested"); Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "mobilenetv2-7.onnx"), "keep");
        RetiredModelCleanup.RemoveCache(scope.Path);
        Assert.False(File.Exists(model)); Assert.False(File.Exists(temporary));
        Assert.Equal(2, Directory.GetFiles(scope.Path).Length);
        Assert.True(File.Exists(Path.Combine(nested, "mobilenetv2-7.onnx")));
    }

    [Fact]
    public void RemovesEmptyModelFolderAndCanRunAgain()
    {
        using var scope = new TestScope();
        var folder = Path.Combine(scope.Path, "models"); Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "mobilenetv2-7.onnx"), "retired");
        RetiredModelCleanup.RemoveCache(folder); RetiredModelCleanup.RemoveCache(folder);
        Assert.False(Directory.Exists(folder));
    }
}
