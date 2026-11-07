using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;

namespace BingWallE.Services;

/// <summary>Owns a detached, reduced bitmap until the clipboard releases its data.</summary>
public sealed class KeywordClipboardData : IAsyncDataTransfer
{
    private readonly DataTransfer _data = new();
    private readonly Bitmap? _bitmap;
    public IReadOnlyList<DataFormat> Formats => ((IAsyncDataTransfer)_data).Formats;
    public IReadOnlyList<IAsyncDataTransferItem> Items => ((IAsyncDataTransfer)_data).Items;

    public KeywordClipboardData(Bitmap? image, string? prompt)
    {
        var item = new DataTransferItem();
        if (image != null)
        {
            var size = image.PixelSize;
            var scale = Math.Min(1, 1024.0 / Math.Max(size.Width, size.Height));
            _bitmap = image.CreateScaledBitmap(new PixelSize(Math.Max(1, (int)Math.Round(size.Width * scale)),
                Math.Max(1, (int)Math.Round(size.Height * scale))), BitmapInterpolationMode.HighQuality);
            item.SetBitmap(_bitmap);
        }
        if (prompt != null) item.SetText(prompt);
        _data.Add(item);
    }

    public void Dispose()
    {
        ((IAsyncDataTransfer)_data).Dispose();
        _bitmap?.Dispose();
    }
}
