using System.Runtime.InteropServices;
using BingWallE.Services;
using SkiaSharp;

InferenceRuntimePolicy.EnsureConfigured();
InferenceRuntimePolicy.EnsureConfigured(); // Safe for repeated callers.
var nativeValue = OperatingSystem.IsMacOS() ? Marshal.PtrToStringUTF8(Native.MacGetEnvironment(InferenceRuntimePolicy.TelemetryVariable))
    : OperatingSystem.IsLinux() ? Marshal.PtrToStringUTF8(Native.LinuxGetEnvironment(InferenceRuntimePolicy.TelemetryVariable))
    : Environment.GetEnvironmentVariable(InferenceRuntimePolicy.TelemetryVariable);
if (nativeValue != "1") throw new Exception("Native ONNX Runtime cannot see the telemetry opt-out.");
using var image = new SKBitmap(32, 24);
image.Erase(SKColors.Blue);
using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 90);
using (var tagger = new LocalImageTagger(Path.GetFullPath(args.Single())))
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
    await tagger.RecognizeAsync(encoded.ToArray(), timeout.Token);
}
Console.WriteLine("Native telemetry opt-out verified; inference and disposal completed. Exiting normally.");

static class Native
{
    [DllImport("libSystem.B.dylib", EntryPoint = "getenv")]
    internal static extern IntPtr MacGetEnvironment(string name);
    [DllImport("libc", EntryPoint = "getenv")]
    internal static extern IntPtr LinuxGetEnvironment(string name);
}
