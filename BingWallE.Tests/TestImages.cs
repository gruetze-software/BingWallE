using SkiaSharp;

namespace BingWallE.Tests;

internal static class TestImages
{
    public static byte[] Jpeg
    {
        get
        {
            using var bitmap = new SKBitmap(32, 24);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.CornflowerBlue);
            using var encoded = bitmap.Encode(SKEncodedImageFormat.Jpeg, 90);
            return encoded.ToArray();
        }
    }
}
