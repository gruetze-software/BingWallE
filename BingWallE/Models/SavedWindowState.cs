using System;

namespace BingWallE.Models;

public sealed record SavedWindowState(double Width = 1040, double Height = 820, bool IsMaximized = false);

public static class WindowSizing
{
    public static SavedWindowState Fit(SavedWindowState saved, double availableWidth, double availableHeight)
    {
        double FitDimension(double requested, double fallback, double available) =>
            Math.Min(double.IsFinite(requested) && requested > 0 ? requested : fallback, Math.Max(1, available));
        return saved with { Width = FitDimension(saved.Width, 1040, availableWidth), Height = FitDimension(saved.Height, 820, availableHeight) };
    }
}
