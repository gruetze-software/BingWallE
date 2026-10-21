using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using BingWallE.Models;

namespace BingWallE.Services;

// Updates the standard XMP packet without decoding or recompressing JPEG scan data.
public static class JpegXmpMetadata
{
    private static readonly byte[] Prefix = Encoding.ASCII.GetBytes("http://ns.adobe.com/xap/1.0/\0");
    public static byte[] Write(byte[] jpeg, Wallpaper image, string comments, IReadOnlyList<ImageTag> tags,
        DateTimeOffset? downloadedAt = null, DateTime? createdAt = null, bool creationFromDownload = false, bool originalFromDownload = false)
    {
        if (jpeg.Length < 4 || jpeg[0] != 255 || jpeg[1] != 216) throw new InvalidDataException("Invalid JPEG.");
        var segments = new List<(int Offset, int Length, bool Xmp)>();
        var offset = 2; XDocument? document = null;
        while (offset + 4 <= jpeg.Length)
        {
            var start = offset;
            if (jpeg[offset++] != 255) throw new InvalidDataException("Invalid JPEG marker.");
            while (offset < jpeg.Length && jpeg[offset] == 255) offset++;
            if (offset >= jpeg.Length) throw new InvalidDataException("Truncated JPEG marker.");
            var marker = jpeg[offset++];
            if (marker is 0xDA or 0xD9) { offset = start; break; }
            if (marker == 0x01 || marker is >= 0xD0 and <= 0xD7)
            { segments.Add((start, offset - start, false)); continue; }
            if (offset + 2 > jpeg.Length) throw new InvalidDataException("Truncated JPEG segment.");
            var length = BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(offset, 2));
            if (length < 2 || offset + length > jpeg.Length) throw new InvalidDataException("Invalid JPEG segment size.");
            var isXmp = marker == 0xE1 && jpeg.AsSpan(offset + 2, length - 2).StartsWith(Prefix);
            if (isXmp)
            {
                // XDocument's default reader prohibits external DTD resolution.
                document = XDocument.Parse(Encoding.UTF8.GetString(jpeg, offset + 2 + Prefix.Length, length - 2 - Prefix.Length));
            }
            segments.Add((start, offset + length - start, isXmp)); offset += length;
        }
        XNamespace x = "adobe:ns:meta/", rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        XNamespace dc = "http://purl.org/dc/elements/1.1/", xmp = "http://ns.adobe.com/xap/1.0/";
        XNamespace bw = "https://github.com/gruetze-software/BingWallE/metadata/1.0/";
        document ??= new XDocument(new XElement(x + "xmpmeta", new XAttribute(XNamespace.Xmlns + "x", x),
            new XElement(rdf + "RDF", new XAttribute(XNamespace.Xmlns + "rdf", rdf))));
        var root = document.Descendants(rdf + "RDF").FirstOrDefault() ?? throw new InvalidDataException("Invalid XMP packet.");
        var copyright = WallpaperCredits.Split(image.Copyright).Copyright;
        var timestamp = downloadedAt ?? DateTimeOffset.Now;
        var hasCreationDate = root.Elements(rdf + "Description").Any(d =>
            !string.IsNullOrWhiteSpace(d.Element(xmp + "CreateDate")?.Value) || !string.IsNullOrWhiteSpace(d.Attribute(xmp + "CreateDate")?.Value));
        var creationOrigin = root.Descendants(bw + "CreationDateSource").FirstOrDefault()?.Value;
        var originalOrigin = root.Descendants(bw + "OriginalDateSource").FirstOrDefault()?.Value;
        foreach (var description in root.Elements(rdf + "Description"))
        {
            foreach (var name in new[] { dc + "title", dc + "description", dc + "rights", dc + "subject", dc + "source", xmp + "CreatorTool", bw + "DownloadDate", bw + "CreationDateSource", bw + "OriginalDateSource", bw + "Source", bw + "SourceId", bw + "PublicationDate", bw + "SourceUrl", bw + "InformationUrl", bw + "Market", bw + "Predictions" })
            {
                if (name == dc + "rights" && string.IsNullOrEmpty(copyright)) continue;
                description.Elements(name).Remove(); description.Attribute(name)?.Remove();
            }
            if (!hasCreationDate)
            { description.Elements(xmp + "CreateDate").Remove(); description.Attribute(xmp + "CreateDate")?.Remove(); }
        }
        XElement Alt(XName name, string value) => new(name,
            new XElement(rdf + "Alt", new XElement(rdf + "li", new XAttribute(XNamespace.Xml + "lang", "x-default"), value)));
        root.Add(new XElement(rdf + "Description", new XAttribute(rdf + "about", ""),
            new XAttribute(XNamespace.Xmlns + "dc", dc), new XAttribute(XNamespace.Xmlns + "xmp", xmp),
            Alt(dc + "title", image.Title), Alt(dc + "description", comments), string.IsNullOrEmpty(copyright) ? null : Alt(dc + "rights", copyright),
            new XElement(dc + "subject", new XElement(rdf + "Bag", WallpaperKeywords.Stored(image, tags).Select(t => new XElement(rdf + "li", t)))),
            new XElement(xmp + "CreatorTool", AppInfo.DisplayName),
            hasCreationDate ? null : new XElement(xmp + "CreateDate", createdAt is { } created
                ? created.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                : timestamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture)),
            new XElement(bw + "DownloadDate", timestamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture)),
            new XElement(bw + "CreationDateSource", creationFromDownload ? "Download" : creationOrigin ?? "EmbeddedMetadata"),
            new XElement(bw + "OriginalDateSource", originalFromDownload ? "Download" : originalOrigin ?? "EmbeddedMetadata"),
            new XElement(dc + "source", image.Url), new XElement(bw + "Source", image.SourceName),
            image.SourceId == null ? null : new XElement(bw + "SourceId", image.SourceId),
            string.IsNullOrEmpty(image.Date) ? null : new XElement(bw + "PublicationDate", image.Date), new XElement(bw + "SourceUrl", image.Url),
            new XElement(bw + "InformationUrl", image.CopyrightUrl ?? ""), new XElement(bw + "Market", image.Market),
            new XElement(bw + "Predictions", new XElement(rdf + "Bag", tags.Select(t => new XElement(rdf + "li",
                new XAttribute("label", t.Label), new XAttribute("score", t.Score.ToString(System.Globalization.CultureInfo.InvariantCulture))))))));
        var packet = Encoding.UTF8.GetBytes(document.ToString(SaveOptions.DisableFormatting));
        var payloadLength = Prefix.Length + packet.Length + 2;
        if (payloadLength > ushort.MaxValue) throw new InvalidDataException("XMP metadata is too large.");
        using var output = new MemoryStream();
        output.Write(jpeg, 0, 2);
        foreach (var segment in segments.Where(s => !s.Xmp)) output.Write(jpeg, segment.Offset, segment.Length);
        output.WriteByte(255); output.WriteByte(0xE1);
        output.WriteByte((byte)(payloadLength >> 8)); output.WriteByte((byte)payloadLength);
        output.Write(Prefix); output.Write(packet);
        output.Write(jpeg, offset, jpeg.Length - offset);
        return output.ToArray();
    }
}
