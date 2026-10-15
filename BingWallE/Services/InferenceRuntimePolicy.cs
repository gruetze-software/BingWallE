using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BingWallE.Services;

// Must run before any ONNX Runtime API: disabling events afterwards still creates the uploader.
public static class InferenceRuntimePolicy
{
    public const string TelemetryVariable = "ORT_DISABLE_TELEMETRY";
    private static readonly Lazy<bool> Configured = new(Configure);

    public static void EnsureConfigured() => _ = Configured.Value;

    private static bool Configure()
    {
        // .NET's Unix environment is managed separately from native getenv(). Set both.
        var result = OperatingSystem.IsMacOS() ? MacSetEnvironment(TelemetryVariable, "1", 1)
            : OperatingSystem.IsLinux() ? LinuxSetEnvironment(TelemetryVariable, "1", 1) : 0;
        if (result != 0) throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not disable native inference telemetry.");
        Environment.SetEnvironmentVariable(TelemetryVariable, "1");
        Trace.WriteLine("ONNX Runtime telemetry disabled before native initialization.");
        return true;
    }

    [DllImport("libSystem.B.dylib", EntryPoint = "setenv", SetLastError = true)]
    private static extern int MacSetEnvironment(string name, string value, int overwrite);

    [DllImport("libc", EntryPoint = "setenv", SetLastError = true)]
    private static extern int LinuxSetEnvironment(string name, string value, int overwrite);
}
