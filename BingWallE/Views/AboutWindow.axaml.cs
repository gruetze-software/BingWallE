using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BingWallE.Models;

namespace BingWallE.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        ProductName.Text = AppInfo.DisplayName;
    }
    private void CloseDialog(object? sender, RoutedEventArgs e) => Close();
    private void OpenLink(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string target } ||
            !Uri.TryCreate(target, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "github.com") return;
        try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception ex) { Trace.WriteLine(ex); LinkStatus.Text = "Der Browser konnte nicht geöffnet werden: " + ex.Message; }
    }
}
