using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using FoodDiary.MailInbox.Application.Messages.Models;
using MimeKit;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class DmarcReportParser : IMailInboxDmarcReportParser {
    private const int MaxDmarcAttachmentBytes = 5 * 1024 * 1024;
    private const int MaxDmarcXmlCharacters = 2 * 1024 * 1024;
    private const int MaxZipXmlEntries = 4;
    private const int MaxZipEntries = 32;
    private const int MaxZipEntryNameBytes = 1024;
    private const int MaxZipTotalEntryNameBytes = 16 * 1024;
    private const int MaxZipCentralDirectoryBytes = 64 * 1024;
    private const int MaxDmarcXmlDocuments = 4;
    private const int MaxDmarcTotalAttachmentBytes = 10 * 1024 * 1024;
    private const int MaxDmarcTotalXmlCharacters = 7 * 1024 * 1024;
    private const int MaxDmarcXmlDepth = 64;
    private const int MaxDmarcXmlElements = 250_000;
    private const int MaxDmarcRecords = 10_000;

    public DmarcReportPreview? TryParse(string rawMime, CancellationToken cancellationToken = default) {
        var budget = new DmarcParseBudget(cancellationToken);
        try {
            foreach (string xml in ExtractXmlPayloads(rawMime, budget)) {
                DmarcReportPreview? report = TryParseXml(xml, budget);
                if (report is not null) {
                    return report;
                }
            }
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception) {
            return null;
        }

        return null;
    }

    private static IEnumerable<string> ExtractXmlPayloads(string rawMime, DmarcParseBudget budget) {
        budget.ThrowIfCancellationRequested();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawMime));
        var message = MimeMessage.Load(stream);
        foreach (MimePart part in message.BodyParts.OfType<MimePart>()) {
            budget.ThrowIfCancellationRequested();
            string fileName = part.FileName ?? string.Empty;
            string contentType = part.ContentType.MimeType;
            if (part.Content is null) {
                continue;
            }

            using var content = new MemoryStream();
            part.Content.DecodeTo(content);
            budget.AddAttachmentBytes(content.Length);
            if (content.Length > MaxDmarcAttachmentBytes) {
                continue;
            }

            byte[] bytes = content.ToArray();

            if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals("application/zip", StringComparison.OrdinalIgnoreCase)) {
                foreach (string xml in ExtractZipXmlPayloads(bytes, budget)) {
                    yield return xml;
                }

                continue;
            }

            if (fileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals("application/gzip", StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals("application/x-gzip", StringComparison.OrdinalIgnoreCase)) {
                budget.StartXmlDocument();
                yield return budget.CompleteXmlDocument(DecompressGzip(bytes, budget));
                continue;
            }

            if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals("text/xml", StringComparison.OrdinalIgnoreCase)) {
                budget.StartXmlDocument();
                yield return budget.CompleteXmlDocument(Encoding.UTF8.GetString(bytes));
            }
        }
    }

    private static IEnumerable<string> ExtractZipXmlPayloads(byte[] bytes, DmarcParseBudget budget) {
        ValidateZipMetadata(bytes);
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        IEnumerable<ZipArchiveEntry> xmlEntries = archive.Entries
            .Where(static entry => entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .Take(MaxZipXmlEntries);

        foreach (ZipArchiveEntry entry in xmlEntries) {
            budget.ThrowIfCancellationRequested();
            if (entry.Length > MaxDmarcXmlCharacters) {
                continue;
            }

            budget.StartXmlDocument();
            using Stream entryStream = entry.Open();
            yield return budget.CompleteXmlDocument(ReadTextWithLimit(entryStream, budget));
        }
    }

    private static void ValidateZipMetadata(ReadOnlySpan<byte> bytes) {
        const uint endOfCentralDirectorySignature = 0x06054b50;
        const uint centralDirectoryEntrySignature = 0x02014b50;
        const int endOfCentralDirectoryLength = 22;
        const int centralDirectoryEntryLength = 46;
        int minimumOffset = Math.Max(0, bytes.Length - (ushort.MaxValue + endOfCentralDirectoryLength));

        for (int offset = bytes.Length - endOfCentralDirectoryLength; offset >= minimumOffset; offset--) {
            ReadOnlySpan<byte> candidate = bytes[offset..];
            if (BinaryPrimitives.ReadUInt32LittleEndian(candidate) != endOfCentralDirectorySignature) {
                continue;
            }

            ushort commentLength = BinaryPrimitives.ReadUInt16LittleEndian(candidate[20..]);
            if (offset + endOfCentralDirectoryLength + commentLength != bytes.Length) {
                continue;
            }

            ValidateCentralDirectory(bytes, candidate, offset, centralDirectoryEntrySignature, centralDirectoryEntryLength);
            return;
        }

        throw new InvalidDataException("ZIP end-of-central-directory record was not found.");
    }

    private static void ValidateCentralDirectory(
        ReadOnlySpan<byte> bytes,
        ReadOnlySpan<byte> endOfCentralDirectory,
        int endOfCentralDirectoryOffset,
        uint centralDirectoryEntrySignature,
        int centralDirectoryEntryLength) {
        ushort diskNumber = BinaryPrimitives.ReadUInt16LittleEndian(endOfCentralDirectory[4..]);
        ushort centralDirectoryDisk = BinaryPrimitives.ReadUInt16LittleEndian(endOfCentralDirectory[6..]);
        ushort entriesOnDisk = BinaryPrimitives.ReadUInt16LittleEndian(endOfCentralDirectory[8..]);
        ushort totalEntries = BinaryPrimitives.ReadUInt16LittleEndian(endOfCentralDirectory[10..]);
        uint centralDirectorySize = BinaryPrimitives.ReadUInt32LittleEndian(endOfCentralDirectory[12..]);
        uint centralDirectoryOffset = BinaryPrimitives.ReadUInt32LittleEndian(endOfCentralDirectory[16..]);

        if (diskNumber != 0 || centralDirectoryDisk != 0 || entriesOnDisk != totalEntries ||
            totalEntries == ushort.MaxValue || centralDirectorySize == uint.MaxValue ||
            centralDirectoryOffset == uint.MaxValue) {
            throw new InvalidDataException("Multi-disk and ZIP64 DMARC archives are not supported.");
        }

        if (totalEntries > MaxZipEntries || centralDirectorySize > MaxZipCentralDirectoryBytes ||
            (ulong)centralDirectoryOffset + centralDirectorySize > (ulong)endOfCentralDirectoryOffset ||
            (ulong)centralDirectoryOffset + centralDirectorySize > (ulong)bytes.Length) {
            throw new InvalidDataException("ZIP central directory exceeds the allowed metadata budget.");
        }

        int position = checked((int)centralDirectoryOffset);
        int centralDirectoryEnd = checked(position + (int)centralDirectorySize);
        int totalEntryNameBytes = 0;
        for (int entryIndex = 0; entryIndex < totalEntries; entryIndex++) {
            if (position + centralDirectoryEntryLength > centralDirectoryEnd ||
                BinaryPrimitives.ReadUInt32LittleEndian(bytes[position..]) != centralDirectoryEntrySignature) {
                throw new InvalidDataException("ZIP central directory is malformed.");
            }

            ushort entryNameBytes = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(position + 28)..]);
            ushort extraFieldBytes = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(position + 30)..]);
            ushort commentBytes = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(position + 32)..]);
            totalEntryNameBytes = checked(totalEntryNameBytes + entryNameBytes);
            if (entryNameBytes > MaxZipEntryNameBytes || totalEntryNameBytes > MaxZipTotalEntryNameBytes) {
                throw new InvalidDataException("ZIP entry names exceed the allowed metadata budget.");
            }

            position = checked(position + centralDirectoryEntryLength + entryNameBytes + extraFieldBytes + commentBytes);
        }

        if (position != centralDirectoryEnd) {
            throw new InvalidDataException("ZIP central directory contains unexpected metadata.");
        }
    }

    private static string DecompressGzip(byte[] bytes, DmarcParseBudget budget) {
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        return ReadTextWithLimit(gzip, budget);
    }

    private static DmarcReportPreview? TryParseXml(string xml, DmarcParseBudget budget) {
        budget.ThrowIfCancellationRequested();
        XDocument document;
        try {
            ValidateXmlStructure(xml, budget);
            using var reader = XmlReader.Create(new StringReader(xml), CreateXmlReaderSettings());
            document = XDocument.Load(reader);
        } catch (Exception) when (!budget.IsCancellationRequested) {
            return null;
        }

        XElement? root = document.Root;
        if (root is null || !IsElement(root, "feedback")) {
            return null;
        }

        XElement? metadata = root.Elements().FirstOrDefault(static element => IsElement(element, "report_metadata"));
        XElement? policy = root.Elements().FirstOrDefault(static element => IsElement(element, "policy_published"));
        XElement? dateRange = metadata?.Elements().FirstOrDefault(static element => IsElement(element, "date_range"));
        XElement[] recordElements = [.. root.Elements()
            .Where(static element => IsElement(element, "record"))
            .Take(MaxDmarcRecords + 1)];
        if (recordElements.Length > MaxDmarcRecords) {
            throw new InvalidDataException("DMARC record count exceeds the maximum allowed size.");
        }

        DmarcReportRecordPreview[] records = [.. recordElements.Select(ParseRecord)];

        return new DmarcReportPreview(
            GetChildValue(metadata, "org_name"),
            GetChildValue(metadata, "report_id"),
            GetChildValue(policy, "domain"),
            ParseUnixTime(GetChildValue(dateRange, "begin")),
            ParseUnixTime(GetChildValue(dateRange, "end")),
            records);
    }

    private static void ValidateXmlStructure(string xml, DmarcParseBudget budget) {
        using var reader = XmlReader.Create(new StringReader(xml), CreateXmlReaderSettings());
        int elementCount = 0;
        while (reader.Read()) {
            budget.ThrowIfCancellationRequested();
            if (reader.NodeType != XmlNodeType.Element) {
                continue;
            }

            if (reader.Depth > MaxDmarcXmlDepth) {
                throw new InvalidDataException("DMARC XML nesting exceeds the maximum allowed depth.");
            }

            if (++elementCount > MaxDmarcXmlElements) {
                throw new InvalidDataException("DMARC XML element count exceeds the maximum allowed size.");
            }
        }
    }

    private static XmlReaderSettings CreateXmlReaderSettings() => new() {
        DtdProcessing = DtdProcessing.Prohibit,
        MaxCharactersInDocument = MaxDmarcXmlCharacters,
        XmlResolver = null,
    };

    private static DmarcReportRecordPreview ParseRecord(XElement record) {
        XElement? row = record.Elements().FirstOrDefault(static element => IsElement(element, "row"));
        XElement? evaluated = row?.Elements().FirstOrDefault(static element => IsElement(element, "policy_evaluated"));
        XElement? identifiers = record.Elements().FirstOrDefault(static element => IsElement(element, "identifiers"));
        XElement? authResults = record.Elements().FirstOrDefault(static element => IsElement(element, "auth_results"));
        XElement? dkim = authResults?.Elements().FirstOrDefault(static element => IsElement(element, "dkim"));
        XElement? spf = authResults?.Elements().FirstOrDefault(static element => IsElement(element, "spf"));

        return new DmarcReportRecordPreview(
            GetChildValue(row, "source_ip"),
            ParseInt(GetChildValue(row, "count")),
            GetChildValue(evaluated, "disposition"),
            GetChildValue(evaluated, "dkim"),
            GetChildValue(evaluated, "spf"),
            GetChildValue(identifiers, "header_from"),
            GetChildValue(identifiers, "envelope_from"),
            GetChildValue(dkim, "domain"),
            GetChildValue(dkim, "result"),
            GetChildValue(spf, "domain"),
            GetChildValue(spf, "result"));
    }

    private static bool IsElement(XElement element, string name) =>
        element.Name.LocalName.Equals(name, StringComparison.Ordinal);

    private static string? GetChildValue(XElement? element, string name) =>
        element?.Elements().FirstOrDefault(child => IsElement(child, name))?.Value;

    private static int ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0;

    private static DateTimeOffset? ParseUnixTime(string? value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result)
            ? DateTimeOffset.FromUnixTimeSeconds(result)
            : null;

    private static string ReadTextWithLimit(Stream stream, DmarcParseBudget budget) {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        var builder = new StringBuilder();
        char[] buffer = new char[8192];
        int read;

        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0) {
            budget.ThrowIfCancellationRequested();
            if (builder.Length + read > MaxDmarcXmlCharacters) {
                throw new InvalidDataException("DMARC XML payload exceeds the maximum allowed size.");
            }

            builder.Append(buffer, 0, read);
        }

        return builder.ToString();
    }

    private sealed class DmarcParseBudget(CancellationToken cancellationToken) {
        private long _attachmentBytes;
        private int _xmlCharacters;
        private int _xmlDocuments;

        public bool IsCancellationRequested => cancellationToken.IsCancellationRequested;

        public void ThrowIfCancellationRequested() => cancellationToken.ThrowIfCancellationRequested();

        public void AddAttachmentBytes(long bytes) {
            _attachmentBytes = checked(_attachmentBytes + bytes);
            if (_attachmentBytes > MaxDmarcTotalAttachmentBytes) {
                throw new InvalidDataException("DMARC attachments exceed the total allowed size.");
            }
        }

        public void StartXmlDocument() {
            ThrowIfCancellationRequested();
            if (++_xmlDocuments > MaxDmarcXmlDocuments) {
                throw new InvalidDataException("DMARC XML document count exceeds the maximum allowed size.");
            }
        }

        public string CompleteXmlDocument(string xml) {
            if (xml.Length > MaxDmarcXmlCharacters) {
                throw new InvalidDataException("DMARC XML payload exceeds the maximum allowed size.");
            }

            _xmlCharacters = checked(_xmlCharacters + xml.Length);
            if (_xmlCharacters > MaxDmarcTotalXmlCharacters) {
                throw new InvalidDataException("DMARC XML payloads exceed the total allowed size.");
            }

            return xml;
        }
    }
}
