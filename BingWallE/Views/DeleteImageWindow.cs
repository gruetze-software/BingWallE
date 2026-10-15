using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using BingWallE.Localization;

namespace BingWallE.Views;

public sealed class DeleteImageWindow : Window
{
    public DeleteImageWindow(UiText text, string path)
    {
        Title = text.Get("Lokale Datei löschen?"); Width = 500; Height = 270;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var cancel = new Button { Content = text.Get("Abbrechen"), IsCancel = true, IsDefault = true };
        var delete = new Button { Content = text.Get("Lokal löschen") };
        cancel.Click += (_, _) => Close(false); delete.Click += (_, _) => Close(true);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(cancel); buttons.Children.Add(delete);
        var content = new StackPanel { Spacing = 16 };
        content.Children.Add(new TextBlock { Text = text.Get("Diese Datei wird dauerhaft gelöscht. Andere Auflösungen bleiben erhalten."), TextWrapping = TextWrapping.Wrap });
        content.Children.Add(new TextBlock { Text = path, TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Margin = new Thickness(24) };
        grid.Children.Add(new ScrollViewer { Content = content }); Grid.SetRow(buttons, 1); grid.Children.Add(buttons);
        Content = grid;
        Opened += (_, _) => WindowScreenFit.Apply(this, new(Width, Height));
    }
}
