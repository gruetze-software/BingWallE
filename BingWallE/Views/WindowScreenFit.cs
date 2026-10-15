using System;
using Avalonia;
using Avalonia.Controls;
using BingWallE.Models;

namespace BingWallE.Views;

public static class WindowScreenFit
{
    public static SavedWindowState Apply(Window window, SavedWindowState saved)
    {
        var screen = window.Screens.ScreenFromWindow(window) ?? window.Screens.Primary;
        if (screen == null) return saved;
        var area = screen.WorkingArea;
        var scale = screen.Scaling;
        var frameWidth = Math.Max(0, (window.FrameSize?.Width ?? window.ClientSize.Width) - window.ClientSize.Width);
        var frameHeight = Math.Max(32, (window.FrameSize?.Height ?? window.ClientSize.Height) - window.ClientSize.Height);
        var fitted = WindowSizing.Fit(saved, area.Width / scale - frameWidth - 24, area.Height / scale - frameHeight - 24);
        window.MinWidth = Math.Min(window.MinWidth, fitted.Width);
        window.MinHeight = Math.Min(window.MinHeight, fitted.Height);
        window.Width = fitted.Width; window.Height = fitted.Height;
        window.Position = new PixelPoint(area.X + (int)((area.Width - (fitted.Width + frameWidth) * scale) / 2),
            area.Y + (int)((area.Height - (fitted.Height + frameHeight) * scale) / 2));
        return fitted;
    }
}
