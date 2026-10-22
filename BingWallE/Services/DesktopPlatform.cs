using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;

namespace BingWallE.Services;

public enum LinuxDisplayBackend { Auto, X11, Wayland }

public static class DesktopPlatform
{
    public const string AppId = "de.gruetze_software.BingWallE";
    public const string BackendVariable = "BINGWALLE_LINUX_BACKEND";

    public static LinuxDisplayBackend ParseBackend(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "auto" => LinuxDisplayBackend.Auto,
        "x11" => LinuxDisplayBackend.X11,
        "wayland" => LinuxDisplayBackend.Wayland,
        _ => throw new ArgumentException($"{BackendVariable} must be auto, x11 or wayland.")
    };

    public static AppBuilder Configure(AppBuilder builder)
    {
        builder.UsePlatformDetect();
        if (OperatingSystem.IsLinux())
        {
            builder.With(new X11PlatformOptions { WmClass = AppId })
                .With(new WaylandPlatformOptions { AppId = AppId });
            switch (ParseBackend(Environment.GetEnvironmentVariable(BackendVariable)))
            {
                case LinuxDisplayBackend.Auto:
                    // Avalonia cleans up failed Wayland initialization before
                    // invoking the X11 backend selected by UsePlatformDetect().
                    builder.UseWaylandWithFallback();
                    break;
                case LinuxDisplayBackend.Wayland:
                    builder.UseWayland();
                    break;
                case LinuxDisplayBackend.X11:
                    break;
            }
        }
        return builder;
    }

    public static string Describe(Window window)
        => window.PlatformImpl?.GetType().FullName ?? "unavailable";

    public static void Report(Window window)
    {
        var message = $"BingWallE display backend: {Describe(window)}";
        Trace.WriteLine(message);
        Console.WriteLine(message);
    }
}
