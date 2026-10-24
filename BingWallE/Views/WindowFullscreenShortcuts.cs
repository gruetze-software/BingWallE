using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace BingWallE.Views;

internal static class WindowFullscreenShortcuts
{
    public static void Attach(Window window, Func<bool>? handleEscape = null)
    {
        var previousState = WindowState.Normal;
        window.PropertyChanged += (_, e) =>
        {
            if (e.Property == Window.WindowStateProperty && window.WindowState == WindowState.FullScreen)
                previousState = e.OldValue is WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
        };
        window.KeyDown += (_, e) =>
        {
            if (e.Handled || e.KeyModifiers != KeyModifiers.None) return;
            if (e.Key == Key.F11)
            {
                window.WindowState = window.WindowState == WindowState.FullScreen ? previousState : WindowState.FullScreen;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (handleEscape?.Invoke() == true) e.Handled = true;
                else if (window.WindowState == WindowState.FullScreen)
                {
                    window.WindowState = previousState;
                    e.Handled = true;
                }
            }
        };
    }
}
