using System;
using Avalonia.Controls;
using Avalonia.Threading;
using BingWallE.Models;

namespace BingWallE.Views;

/// <summary>Keeps normal bounds separate from maximized, minimized and fullscreen sizes.</summary>
internal sealed class WindowPlacement
{
    private readonly Window _window;
    private readonly Func<SavedWindowState> _load;
    private readonly Action<SavedWindowState> _save;
    private SavedWindowState _normal;
    private bool _restoring;
    private bool _opened;
    private bool _maximized;

    public WindowPlacement(Window window, SavedWindowState fallback,
        Func<SavedWindowState> load, Action<SavedWindowState> save)
    {
        _window = window;
        _normal = fallback;
        _load = load;
        _save = save;
        window.DataContextChanged += (_, _) => { if (!window.IsVisible) Restore(); };
        window.Opened += (_, _) =>
        {
            // Screens and native frame dimensions are available reliably after Show().
            Restore();
            _opened = true;
        };
        window.Resized += (_, e) =>
        {
            if (_restoring || !_opened) return;
            var size = e.ClientSize;
            // Win32 reports WM_SIZE before WindowStateChanged when maximizing.
            // Wait until both notifications have arrived before accepting normal bounds.
            Dispatcher.UIThread.Post(() =>
            {
                if (window.IsVisible && window.WindowState == WindowState.Normal && window.ClientSize == size)
                    CaptureNormalSize();
            }, DispatcherPriority.Background);
        };
        window.PropertyChanged += (_, e) =>
        {
            if (_restoring || !_opened || e.Property != Window.WindowStateProperty) return;
            if (window.WindowState is WindowState.Normal or WindowState.Maximized)
                _maximized = window.WindowState == WindowState.Maximized;
        };
        window.Closing += (_, _) =>
        {
            // Also cover a resize followed immediately by Close, before the queued callback.
            if (window.WindowState == WindowState.Normal) CaptureNormalSize();
            _save(_normal with { IsMaximized = _maximized });
        };
    }

    private void CaptureNormalSize()
    {
        var size = _window.ClientSize;
        if (size.Width > 0 && size.Height > 0)
            _normal = new(size.Width, size.Height);
    }

    private void Restore()
    {
        _restoring = true;
        try
        {
            var saved = _load();
            _normal = WindowScreenFit.Apply(_window, saved);
            _maximized = saved.IsMaximized;
            _window.WindowState = _maximized ? WindowState.Maximized : WindowState.Normal;
        }
        finally { _restoring = false; }
    }
}
