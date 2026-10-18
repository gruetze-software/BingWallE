using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BingWallE.Models;
using BingWallE.Localization;

namespace BingWallE.Views;

public partial class AboutWindow : Window
{
    private readonly UiText _text;
    public AboutWindow() : this(new UiText()) { }
    public AboutWindow(UiText text)
    {
        _text = text;
        _text.ApplyResources();
        InitializeComponent();
        ProductName.Text = AppInfo.DisplayName;
    }
    private void CloseDialog(object? sender, RoutedEventArgs e) => Close();
    private void OpenLink(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string target } ||
            !Uri.TryCreate(target, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            (uri.Host != "github.com" && uri.AbsoluteUri != "https://ko-fi.com/gruetzesoftware")) return;
        try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception ex) { Trace.WriteLine(ex); LinkStatus.Text = _text.Get("Der Browser konnte nicht geöffnet werden: ") + _text.Get("Die Aktion ist fehlgeschlagen. Weitere Informationen stehen in der Diagnoseausgabe."); }
    }
}
