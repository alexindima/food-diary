using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using FoodDiary.MailInbox.Application.Messages.Models;
using MimeKit;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class DmarcReportParser {
    private const int MaxDmarcAttachmentBytes = 5 * 1024 * 1024;
    private const int MaxDmarcXmlCharacters = 2 * 1024 * 1024;
    private const int MaxZipXmlEntries = 4;

    public DmarcReportPreview? TryParse(string rawMime) {
        try {
            foreach (string xml in ExtractXmlPayloads(rawMime)) {
                DmarcReportPreview? report = TryParseXml(xml);
                if (report is not null) {
                    return report;
                }
            }
        } catch (Exception) {
            return null;
        }

        return null;
    }

    private static IEnumerable<string> ExtractXmlPayloads(string rawMime) {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawMime));
        var message = MimeMessage.Load(stream);
        foreach (MimePart part in message.BodyParts.OfType<MimePart>()) {
            string fileName = part.FileName ?? string.Empty;
            string contentType = part.ContentType.MimeType;
            if (part.Content is null) {
                continue;
            }

            using var content = new MemoryStream();
            part.Content.DecodeTo(content);
            if (content.Length > MaxDmarcAttachmentBytes) {
                continue;
            }

            byte[] bytes = content.ToArray();

            if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals("application/zip", StringComparison.OrdinalIgnoreCase)) {
                foreach (string xml in ExtractZipXmlPayloads(bytes)) {
                    yield return xml;
                }

                continue;
            }

            if (fileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals("application/gzip", StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals("application/x-gzip", StringComparison.OrdinalIgnoreCase)) {
                yield return DecompressGzip(bytes);
                continue;
            }

            if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals("text/xml", StringComparison.OrdinalIgnoreCase)) {
                yield return Encoding.UTF8.GetString(bytes);
            }
        }
    }

    private static IEnumerable<string> ExtractZipXmlPayloads(byte[] bytes) {
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        IEnumerable<ZipArchiveEntry> xmlEntries = archive.Entries
            .Where(static entry => entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .Take(MaxZipXmlEntries);

        foreach (ZipArchiveEntry entry in xmlEntries) {
            if (entry.Length > MaxDmarcXmlCharacters) {
                continue;
            }

            using Stream entryStream = entry.Open();
            yield return ReadTextWithLimit(entryStream);
        }
    }

    private static string DecompressGzip(byte[] bytes) {
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        return ReadTextWithLimit(gzip);
    }

    private static DmarcReportPreview? TryParseXml(string xml) {
        XDocument document;
        try {
            using var reader = XmlReader.Create(
                new StringReader(xml),
                new XmlReaderSettings {
                    DtdProcessing = DtdProcessing.Prohibit,
                    MaxCharactersInDocument = MaxDmarcXmlCharacters,
                    XmlResolver = null,
                });
            document = XDocument.Load(reader);
        } catch (Exception) {
            return null;
        }

        XElement? root = document.Root;
        if (root is null || !IsElement(root, "feedback")) {
            return null;
        }

        XElement? metadata = root.Elements().FirstOrDefault(static element => IsElement(element, "report_metadata"));
        XElement? policy = root.Elements().FirstOrDefault(static element => IsElement(element, "policy_published"));
        XElement? dateRange = metadata?.Elements().FirstOrDefault(static element => IsElement(element, "date_range"));
        DmarcReportRecordPreview[] records = [.. root.Elements()
            .Where(static element => IsElement(element, "record"))
            .Select(ParseRecord)];

        return new DmarcReportPreview(
            GetChildValue(metadata, "org_name"),
            GetChildValue(metadata, "report_id"),
            GetChildValue(policy, "domain"),
            ParseUnixTime(GetChildValue(dateRange, "begin")),
            ParseUnixTime(GetChildValue(dateRange, "end")),
            records);
    }

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

    private static string ReadTextWithLimit(Stream stream) {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        var builder = new StringBuilder();
        char[] buffer = new char[8192];
        int read;

        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0) {
            if (builder.Length + read > MaxDmarcXmlCharacters) {
                throw new InvalidDataException("DMARC XML payload exceeds the maximum allowed size.");
            }

            builder.Append(buffer, 0, read);
        }

        return builder.ToString();
    }
}
