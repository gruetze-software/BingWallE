using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace BingWallE.Services;

public static class RetiredModelCleanup
{
    public static void RemoveDefaultCache() => RemoveCache(ApplicationPaths.ModelsDirectory);

    public static void RemoveCache(string directory)
    {
        try
        {
            if (!Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) return;
            // Delete only this retired model and its own interrupted downloads, never recurse.
            foreach (var path in Directory.EnumerateFiles(directory))
            {
                var name = Path.GetFileName(path);
                if (name != "mobilenetv2-7.onnx" && !Regex.IsMatch(name, @"^mobilenetv2-7\.onnx\.[a-fA-F0-9]{32}\.tmp$")) continue;
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0) File.Delete(path);
            }
            if (Directory.GetFileSystemEntries(directory).Length == 0) Directory.Delete(directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { Trace.WriteLine($"Retired recognition cache could not be removed: {ex.Message}"); }
    }
}
