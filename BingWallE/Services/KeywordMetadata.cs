using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ExifLibrary;

namespace BingWallE.Services;

public sealed record KeywordMetadataState(string[] Keywords, string[] ExcludedKeywords, ImageTag[] Predictions, string[] UserKeywords)
{
    public static KeywordMetadataState Empty => new([], [], [], []);
}

/// <summary>Edits only JPEG keyword fields; keeps the compressed image and other metadata intact.</summary>
public static class KeywordMetadata
{
    private static readonly byte[] Prefix = Encoding.ASCII.GetBytes("http://ns.adobe.com/xap/1.0/\0");
    private static readonly XNamespace Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace Bw = "https://github.com/gruetze-software/BingWallE/metadata/1.0/";

    public static KeywordMetadataState Read(byte[] data)
    {
        var keywords = Array.Empty<string>();
        try
        {
            var file = ImageFile.FromBuffer(data);
            keywords = Clean((file.Properties.Get(ExifTag.WindowsKeywords)?.Value?.ToString() ?? "").TrimEnd('\0').Split(';'));
        }
        catch (Exception ex) { Trace.WriteLine($"Keyword EXIF unavailable: {ex.Message}"); }
        XDocument? document;
        try { document = ReadXmp(data); }
        catch (Exception ex) { Trace.WriteLine($"Keyword XMP unavailable: {ex.Message}"); return new(keywords, [], [], []); }
        if (document == null) return new(keywords, [], [], []);
        string[] Bag(XName name) => Clean(document.Descendants(name).Descendants(Rdf + "li").Select(x => x.Value));
        if (document.Descendants(Dc + "subject").Any()) keywords = Bag(Dc + "subject");
        var predictions = document.Descendants(Bw + "Predictions").Descendants(Rdf + "li")
            .Select(x => float.TryParse((string?)x.Attribute("score"), NumberStyles.Float, CultureInfo.InvariantCulture, out var score)
                && float.IsFinite(score) && (string?)x.Attribute("label") is { Length: > 0 } label ? new ImageTag(label, score) : null)
            .OfType<ImageTag>().ToArray();
        return new(keywords, Bag(Bw + "ExcludedKeywords"), predictions, Bag(Bw + "UserKeywords"));
    }

    public static bool IsJpeg(byte[] bytes) => bytes.Length > 3 && bytes[0] == 255 && bytes[1] == 216;
    public static XDocument? ReadXmp(byte[] bytes) => IsJpeg(bytes) ? Parse(bytes).Document : null;

    public static byte[] Update(byte[] jpeg, KeywordMetadataState state)
    {
        if (!IsJpeg(jpeg)) throw new NotSupportedException("Keywords können derzeit nur in JPEG-Dateien gespeichert werden.");
        var parsed = Parse(jpeg);
        var document = parsed.Document;
        var keywords = Clean(state.Keywords);
        var excluded = Clean(state.ExcludedKeywords);
        var users = Clean(state.UserKeywords);
        foreach (var value in keywords.Concat(excluded).Concat(users)) XmlConvert.VerifyXmlChars(value);
        var exifSegment = parsed.Segments.FirstOrDefault(s => s.PayloadOffset >= 0
            && jpeg[s.PayloadOffset - 3] == 0xE1
            && jpeg.AsSpan(s.PayloadOffset, s.Offset + s.Length - s.PayloadOffset).StartsWith(JpegExifKeywords.Prefix));
        var oldExif = exifSegment.Length == 0 ? null : jpeg[exifSegment.PayloadOffset..(exifSegment.Offset + exifSegment.Length)];
        var exifPayload = JpegExifKeywords.Update(oldExif, keywords);
        XNamespace x = "adobe:ns:meta/";
        document ??= new XDocument(new XElement(x + "xmpmeta", new XAttribute(XNamespace.Xmlns + "x", x),
            new XElement(Rdf + "RDF", new XAttribute(XNamespace.Xmlns + "rdf", Rdf))));
        var root = document.Descendants(Rdf + "RDF").FirstOrDefault() ?? throw new InvalidDataException("Invalid XMP packet.");
        foreach (var description in root.Elements(Rdf + "Description").ToArray())
        {
            foreach (var name in new[] { Dc + "subject", Bw + "Predictions", Bw + "ExcludedKeywords", Bw + "UserKeywords" })
            { description.Elements(name).Remove(); description.Attribute(name)?.Remove(); }
            if (!description.Elements().Any() && string.IsNullOrWhiteSpace(description.Value)
                && description.Attributes().All(a => a.IsNamespaceDeclaration || a.Name == Rdf + "about")) description.Remove();
        }
        XElement Bag(XName name, IEnumerable<string> values) => new(name, new XElement(Rdf + "Bag", values.Select(v => new XElement(Rdf + "li", v))));
        root.Add(new XElement(Rdf + "Description", new XAttribute(Rdf + "about", ""),
            Bag(Dc + "subject", keywords), Bag(Bw + "ExcludedKeywords", excluded), Bag(Bw + "UserKeywords", users),
            new XElement(Bw + "Predictions", new XElement(Rdf + "Bag", state.Predictions
                .Where(p => !excluded.Contains(p.Label, StringComparer.OrdinalIgnoreCase))
                .Select(p => new XElement(Rdf + "li", new XAttribute("label", p.Label),
                    new XAttribute("score", p.Score.ToString(CultureInfo.InvariantCulture))))))));
        var packet = Encoding.UTF8.GetBytes(document.ToString(SaveOptions.DisableFormatting));
        var length = Prefix.Length + packet.Length + 2;
        if (length > ushort.MaxValue) throw new InvalidDataException("Zu viele Keywords für die Bildmetadaten.");
        using var output = new MemoryStream(); output.Write(jpeg, 0, 2);
        var wroteExif = false;
        void WriteExif()
        {
            var exifLength = exifPayload.Length + 2;
            output.WriteByte(255); output.WriteByte(0xE1); output.WriteByte((byte)(exifLength >> 8)); output.WriteByte((byte)exifLength);
            output.Write(exifPayload); wroteExif = true;
        }
        foreach (var segment in parsed.Segments.Where(s => !s.Xmp))
        {
            if (exifSegment.Length > 0 && segment.Offset == exifSegment.Offset) { WriteExif(); continue; }
            if (exifSegment.Length == 0 && !wroteExif && (segment.PayloadOffset < 0 || jpeg[segment.PayloadOffset - 3] != 0xE0)) WriteExif();
            output.Write(jpeg, segment.Offset, segment.Length);
        }
        if (!wroteExif) WriteExif();
        output.WriteByte(255); output.WriteByte(0xE1); output.WriteByte((byte)(length >> 8)); output.WriteByte((byte)length);
        output.Write(Prefix); output.Write(packet); output.Write(jpeg, parsed.ScanOffset, jpeg.Length - parsed.ScanOffset);
        return output.ToArray();
    }

    public static async Task SaveAsync(string path, KeywordMetadataState state, CancellationToken token)
    {
        var before = new FileInfo(path);
        if (before.IsReadOnly) throw new UnauthorizedAccessException();
        var timestamp = before.LastWriteTimeUtc; var length = before.Length;
        var bytes = await File.ReadAllBytesAsync(path, token);
        var updated = await Task.Run(() => Update(bytes, state), token);
        var temporary = Path.Combine(before.DirectoryName!, $".bingwalle-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(temporary, updated, token);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(temporary, File.GetUnixFileMode(path));
            token.ThrowIfCancellationRequested();
            var current = new FileInfo(path);
            if (current.Length != length || current.LastWriteTimeUtc != timestamp)
                throw new IOException("Die Datei wurde inzwischen geändert. Bitte erneut öffnen.");
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static string[] Clean(IEnumerable<string> values) => values.Select(x => x.Trim()).Where(x => x.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private sealed record Packet(List<(int Offset, int Length, bool Xmp, int PayloadOffset)> Segments, int ScanOffset, XDocument? Document);
    private static Packet Parse(byte[] data)
    {
        if (!IsJpeg(data)) throw new InvalidDataException("Invalid JPEG.");
        var segments = new List<(int, int, bool, int)>(); var offset = 2; XDocument? document = null;
        while (offset + 4 <= data.Length)
        {
            var start = offset;
            if (data[offset++] != 255) throw new InvalidDataException("Invalid JPEG marker.");
            while (offset < data.Length && data[offset] == 255) offset++;
            if (offset >= data.Length) throw new InvalidDataException("Truncated JPEG marker.");
            var marker = data[offset++];
            if (marker is 0xDA or 0xD9) { offset = start; break; }
            if (marker == 1 || marker is >= 0xD0 and <= 0xD7) { segments.Add((start, offset - start, false, -1)); continue; }
            if (offset + 2 > data.Length) throw new InvalidDataException("Truncated JPEG segment.");
            var length = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2));
            if (length < 2 || offset + length > data.Length) throw new InvalidDataException("Invalid JPEG segment.");
            var xmp = marker == 0xE1 && data.AsSpan(offset + 2, length - 2).StartsWith(Prefix);
            if (xmp)
            {
                using var reader = XmlReader.Create(new StringReader(Encoding.UTF8.GetString(data, offset + 2 + Prefix.Length, length - 2 - Prefix.Length)),
                    new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
                document = XDocument.Load(reader);
            }
            segments.Add((start, offset + length - start, xmp, offset + 2)); offset += length;
        }
        return new(segments, offset, document);
    }
}
