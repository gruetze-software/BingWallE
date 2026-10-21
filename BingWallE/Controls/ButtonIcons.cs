using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace BingWallE.Controls;

// Vector coffee icon for the support button, independent of platform fonts.
public sealed class ButtonIcons : AvaloniaObject
{
    public static readonly AttachedProperty<Geometry?> IconProperty =
        AvaloniaProperty.RegisterAttached<ButtonIcons, Button, Geometry?>("Icon");
    public static Geometry? GetIcon(Button button) => button.GetValue(IconProperty);
    public static void SetIcon(Button button, Geometry? value) => button.SetValue(IconProperty, value);

    public static Geometry Coffee { get; } = Geometry.Parse("M3 7 H17 V14 A6 6 0 0 1 5 14 V7 M17 8 H19 A3 3 0 0 1 19 14 H17 M3 21 H19 M8 2 V4 M12 2 V4");
}
