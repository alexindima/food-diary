using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using FoodDiary.MailInbox.Application.Messages.Models;
using MimeKit;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class DmarcReportParser {
    public DmarcReportPreview? TryParse(string rawMime) {
        try {
            foreach (var xml in ExtractXmlPayloads(rawMime)) {
                var report = TryParseXml(xml);
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
        foreach (var part in message.BodyParts.OfType<MimePart>()) {
            var fileName = part.FileName ?? string.Empty;
            var contentType = part.ContentType.MimeType;
            if (part.Content is null) {
                continue;
            }

            using var content = new MemoryStream();
            part.Content.DecodeTo(content);
            var bytes = content.ToArray();

            if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals("application/zip", StringComparison.OrdinalIgnoreCase)) {
                foreach (var xml in ExtractZipXmlPayloads(bytes)) {
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
        foreach (var entry in archive.Entries.Where(static entry => entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))) {
            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream, Encoding.UTF8);
            yield return reader.ReadToEnd();
        }
    }

    private static string DecompressGzip(byte[] bytes) {
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static DmarcReportPreview? TryParseXml(string xml) {
        XDocument document;
        try {
            document = XDocument.Parse(xml);
        } catch (Exception) {
            return null;
        }

        var root = document.Root;
        if (root is null || !IsElement(root, "feedback")) {
            return null;
        }

        var metadata = root.Elements().FirstOrDefault(static element => IsElement(element, "report_metadata"));
        var policy = root.Elements().FirstOrDefault(static element => IsElement(element, "policy_published"));
        var dateRange = metadata?.Elements().FirstOrDefault(static element => IsElement(element, "date_range"));
        var records = root.Elements()
            .Where(static element => IsElement(element, "record"))
            .Select(ParseRecord)
            .ToArray();

        return new DmarcReportPreview(
            GetChildValue(metadata, "org_name"),
            GetChildValue(metadata, "report_id"),
            GetChildValue(policy, "domain"),
            ParseUnixTime(GetChildValue(dateRange, "begin")),
            ParseUnixTime(GetChildValue(dateRange, "end")),
            records);
    }

    private static DmarcReportRecordPreview ParseRecord(XElement record) {
        var row = record.Elements().FirstOrDefault(static element => IsElement(element, "row"));
        var evaluated = row?.Elements().FirstOrDefault(static element => IsElement(element, "policy_evaluated"));
        var identifiers = record.Elements().FirstOrDefault(static element => IsElement(element, "identifiers"));
        var authResults = record.Elements().FirstOrDefault(static element => IsElement(element, "auth_results"));
        var dkim = authResults?.Elements().FirstOrDefault(static element => IsElement(element, "dkim"));
        var spf = authResults?.Elements().FirstOrDefault(static element => IsElement(element, "spf"));

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
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;

    private static DateTimeOffset? ParseUnixTime(string? value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? DateTimeOffset.FromUnixTimeSeconds(result)
            : null;
}
