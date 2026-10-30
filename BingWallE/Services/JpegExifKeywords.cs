using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;

namespace BingWallE.Services;

// Patch the TIFF keyword entry without reserializing unrelated EXIF values or MakerNotes.
internal static class JpegExifKeywords
{
    internal static readonly byte[] Prefix = "Exif\0\0"u8.ToArray();
    public static byte[] Update(byte[]? payload, string[] keywords)
    {
        var tiff = payload == null ? new byte[] { 73, 73, 42, 0, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0 } : payload[6..];
        if (tiff.Length < 8 || !(tiff[0] == tiff[1] && tiff[0] is 73 or 77)) throw new InvalidDataException("Invalid EXIF byte order.");
        var little = tiff[0] == 73;
        ushort U16(int offset) => little ? BinaryPrimitives.ReadUInt16LittleEndian(tiff.AsSpan(offset, 2)) : BinaryPrimitives.ReadUInt16BigEndian(tiff.AsSpan(offset, 2));
        uint U32(int offset) => little ? BinaryPrimitives.ReadUInt32LittleEndian(tiff.AsSpan(offset, 4)) : BinaryPrimitives.ReadUInt32BigEndian(tiff.AsSpan(offset, 4));
        void W16(byte[] data, int offset, ushort value)
        { if (little) BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset, 2), value); else BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(offset, 2), value); }
        void W32(byte[] data, int offset, uint value)
        { if (little) BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset, 4), value); else BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset, 4), value); }
        if (U16(2) != 42) throw new InvalidDataException("Invalid EXIF header.");
        var ifd = checked((int)U32(4));
        if (ifd < 8 || ifd > tiff.Length - 6) throw new InvalidDataException("Invalid EXIF directory.");
        var count = U16(ifd);
        if (ifd + 2 + count * 12 + 4 > tiff.Length) throw new InvalidDataException("Truncated EXIF directory.");
        var entry = -1;
        for (var i = 0; i < count; i++)
            if (U16(ifd + 2 + i * 12) == 0x9C9E) { entry = ifd + 2 + i * 12; break; }
        var value = Encoding.Unicode.GetBytes(string.Join(";", keywords) + "\0");
        byte[] result;
        if (entry >= 0)
        {
            // Reuse a previous trailing keyword allocation, so repeated edits do not grow the file.
            var keep = tiff.Length;
            if (U16(entry + 2) == 1 && U32(entry + 4) > 4)
            {
                var oldOffset = U32(entry + 8); var oldLength = U32(entry + 4);
                if ((ulong)oldOffset + oldLength == (ulong)tiff.Length && oldOffset >= ifd + 2 + count * 12 + 4)
                    keep = checked((int)oldOffset);
            }
            result = new byte[keep + (value.Length > 4 ? value.Length : 0)];
            tiff.AsSpan(0, keep).CopyTo(result);
            W16(result, entry + 2, 1); W32(result, entry + 4, (uint)value.Length);
            result.AsSpan(entry + 8, 4).Clear();
            if (value.Length <= 4) value.CopyTo(result, entry + 8);
            else { W32(result, entry + 8, (uint)keep); value.CopyTo(result, keep); }
        }
        else
        {
            var newIfd = (tiff.Length + 1) & ~1;
            var valueOffset = newIfd + 2 + (count + 1) * 12 + 4;
            result = new byte[valueOffset + (value.Length > 4 ? value.Length : 0)];
            tiff.CopyTo(result, 0); W32(result, 4, (uint)newIfd); W16(result, newIfd, (ushort)(count + 1));
            var entries = Enumerable.Range(0, count).Select(i => tiff.AsSpan(ifd + 2 + i * 12, 12).ToArray()).ToList();
            var added = new byte[12]; W16(added, 0, 0x9C9E); W16(added, 2, 1); W32(added, 4, (uint)value.Length);
            if (value.Length <= 4) value.CopyTo(added, 8);
            else { W32(added, 8, (uint)valueOffset); value.CopyTo(result, valueOffset); }
            entries.Add(added);
            ushort Tag(byte[] e) => little ? BinaryPrimitives.ReadUInt16LittleEndian(e) : BinaryPrimitives.ReadUInt16BigEndian(e);
            var index = newIfd + 2;
            foreach (var e in entries.OrderBy(Tag)) { e.CopyTo(result, index); index += 12; }
            W32(result, index, U32(ifd + 2 + count * 12));
        }
        if (result.Length + Prefix.Length + 2 > ushort.MaxValue) throw new InvalidDataException("Zu viele Keywords für die Bildmetadaten.");
        return Prefix.Concat(result).ToArray();
    }
}
