using System.IO.Compression;
using System.Text;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Infrastructure.Services;
using MimeKit;

namespace FoodDiary.MailInbox.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class DmarcReportParserTests {
    [Fact]
    public void TryParse_WhenMessageContainsGzipDmarcReport_ReturnsPreview() {
        string rawMime = CreateRawMessage(CreateGzipReportAttachment());
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.NotNull(report);
        Assert.Equal("google.com", report.OrganizationName);
        Assert.Equal("fooddiary.club", report.Domain);
        Assert.Equal("report-1", report.ReportId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1777161600), report.DateRangeStartUtc);
        DmarcReportRecordPreview record = Assert.Single(report.Records);
        Assert.Equal("193.109.69.58", record.SourceIp);
        Assert.Equal(2, record.Count);
        Assert.Equal("none", record.Disposition);
        Assert.Equal("pass", record.Dkim);
        Assert.Equal("pass", record.Spf);
    }

    [Fact]
    public void TryParse_WhenMessageContainsZipDmarcReport_ReturnsPreview() {
        string rawMime = CreateRawMessage(CreateZipReportAttachment());
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.NotNull(report);
        Assert.Equal("google.com", report.OrganizationName);
        Assert.Equal("report-1", report.ReportId);
        Assert.Equal("fooddiary.club", report.Domain);
    }

    [Fact]
    public void TryParse_WhenZipContentTypeHasNoFileName_ReturnsPreview() {
        MimePart attachment = CreateZipReportAttachment();
        attachment.FileName = null;
        string rawMime = CreateRawMessage(attachment);
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.NotNull(report);
        Assert.Equal("google.com", report.OrganizationName);
    }

    [Fact]
    public void TryParse_WhenMessageContainsXmlDmarcReport_ReturnsPreview() {
        string rawMime = CreateRawMessage(new MimePart("text", "xml") {
            FileName = "report.xml",
            Content = new MimeContent(new MemoryStream(Encoding.UTF8.GetBytes(CreateDmarcXml()))),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
        });
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.NotNull(report);
        Assert.Equal("google.com", report.OrganizationName);
    }

    [Theory]
    [InlineData("application", "xml")]
    [InlineData("text", "xml")]
    public void TryParse_WhenXmlContentTypeHasNoFileName_ReturnsPreview(string mediaType, string mediaSubtype) {
        string rawMime = CreateRawMessage(new MimePart(mediaType, mediaSubtype) {
            Content = new MimeContent(new MemoryStream(Encoding.UTF8.GetBytes(CreateDmarcXml()))),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
        });
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.NotNull(report);
        Assert.Equal("fooddiary.club", report.Domain);
    }

    [Theory]
    [InlineData("application", "gzip")]
    [InlineData("application", "x-gzip")]
    public void TryParse_WhenGzipContentTypeHasNoFileName_ReturnsPreview(string mediaType, string mediaSubtype) {
        MimePart attachment = CreateGzipReportAttachment();
        attachment.FileName = null;
        attachment.ContentType.MediaType = mediaType;
        attachment.ContentType.MediaSubtype = mediaSubtype;
        string rawMime = CreateRawMessage(attachment);
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.NotNull(report);
        Assert.Equal("report-1", report.ReportId);
    }

    [Fact]
    public void TryParse_WhenMessagePartHasNoContent_ReturnsNull() {
        string rawMime = CreateRawMessage(new MimePart("application", "xml") {
            FileName = "report.xml",
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
        });
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.Null(report);
    }

    [Fact]
    public void TryParse_WhenAttachmentExceedsLimit_ReturnsNull() {
        string rawMime = CreateRawMessage(new MimePart("application", "xml") {
            FileName = "report.xml",
            Content = new MimeContent(new MemoryStream(new byte[(5 * 1024 * 1024) + 1])),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
        });
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.Null(report);
    }

    [Fact]
    public void TryParse_WhenZipDoesNotContainXml_ReturnsNull() {
        string rawMime = CreateRawMessage(CreateZipAttachment("readme.txt", "not xml"));
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.Null(report);
    }

    [Fact]
    public void TryParse_WhenZipContainsInvalidXmlBeforeValidReport_ReturnsPreview() {
        string rawMime = CreateRawMessage(CreateZipAttachment(
            ("invalid.xml", "<not-feedback />"),
            ("report.xml", CreateDmarcXml())));
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.NotNull(report);
        Assert.Equal("google.com", report.OrganizationName);
    }

    [Fact]
    public void TryParse_WhenGzipAttachmentIsInvalidBeforeValidXmlReport_ReturnsPreview() {
        string rawMime = CreateRawMessage(
            CreateGzipAttachment("<not-feedback />"),
            new MimePart("application", "xml") {
                FileName = "report.xml",
                Content = new MimeContent(new MemoryStream(Encoding.UTF8.GetBytes(CreateDmarcXml()))),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
            });
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.NotNull(report);
        Assert.Equal("fooddiary.club", report.Domain);
    }

    [Fact]
    public void TryParse_WhenZipXmlEntryIsTooLarge_ReturnsNull() {
        string rawMime = CreateRawMessage(CreateZipAttachment("report.xml", new string('a', (2 * 1024 * 1024) + 1)));
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.Null(report);
    }

    [Fact]
    public void TryParse_WhenXmlRootIsNotFeedback_ReturnsNull() {
        string rawMime = CreateRawMessage(new MimePart("application", "xml") {
            FileName = "report.xml",
            Content = new MimeContent(new MemoryStream(Encoding.UTF8.GetBytes("<not-feedback />"))),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
        });
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.Null(report);
    }

    [Fact]
    public void TryParse_WhenReportContainsInvalidNumbers_ReturnsPreviewWithDefaults() {
        string rawMime = CreateRawMessage(new MimePart("application", "xml") {
            FileName = "report.xml",
            Content = new MimeContent(new MemoryStream(Encoding.UTF8.GetBytes("""
                <feedback>
                  <report_metadata>
                    <org_name>google.com</org_name>
                    <date_range>
                      <begin>not-a-timestamp</begin>
                    </date_range>
                  </report_metadata>
                  <record>
                    <row>
                      <source_ip>192.0.2.1</source_ip>
                      <count>not-a-number</count>
                    </row>
                  </record>
                </feedback>
                """))),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
        });
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.NotNull(report);
        Assert.Null(report.DateRangeStartUtc);
        Assert.Equal(0, report.Records.Single().Count);
    }

    [Fact]
    public void TryParse_WhenMessageDoesNotContainDmarcReport_ReturnsNull() {
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse("From: sender@example.com\r\nSubject: Hello\r\n\r\nPlain message");

        Assert.Null(report);
    }

    [Fact]
    public void TryParse_WhenGzipReportExpandsPastLimit_ReturnsNull() {
        string rawMime = CreateRawMessage(CreateGzipAttachment(new string('a', (2 * 1024 * 1024) + 1)));
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.Null(report);
    }

    [Fact]
    public void TryParse_WhenXmlContainsDtd_ReturnsNull() {
        string rawMime = CreateRawMessage(new MimePart("application", "xml") {
            FileName = "report.xml",
            Content = new MimeContent(new MemoryStream(Encoding.UTF8.GetBytes("""
                <!DOCTYPE feedback [
                  <!ENTITY xxe SYSTEM "file:///etc/passwd">
                ]>
                <feedback><report_metadata><org_name>&xxe;</org_name></report_metadata></feedback>
                """))),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
        });
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse(rawMime);

        Assert.Null(report);
    }

    [Fact]
    public void TryParse_WhenRawMimeIsInvalid_ReturnsNull() {
        var parser = new DmarcReportParser();

        DmarcReportPreview? report = parser.TryParse("\ud800");

        Assert.Null(report);
    }

    private static MimePart CreateGzipReportAttachment() {
        return CreateGzipAttachment(CreateDmarcXml());
    }

    private static MimePart CreateZipReportAttachment() {
        return CreateZipAttachment("report.xml", CreateDmarcXml());
    }

    private static MimePart CreateZipAttachment(string entryName, string payload) {
        return CreateZipAttachment((entryName, payload));
    }

    private static MimePart CreateZipAttachment(params (string EntryName, string Payload)[] entries) {
        using var compressed = new MemoryStream();
        using (var archive = new ZipArchive(compressed, ZipArchiveMode.Create, leaveOpen: true)) {
            foreach ((string? entryName, string? payload) in entries) {
                ZipArchiveEntry entry = archive.CreateEntry(entryName);
                using Stream entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                writer.Write(payload);
            }
        }

        return new MimePart("application", "zip") {
            FileName = "fooddiary.club.zip",
            Content = new MimeContent(new MemoryStream(compressed.ToArray())),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
        };
    }

    private static MimePart CreateGzipAttachment(string payload) {
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionMode.Compress, leaveOpen: true))
        using (var writer = new StreamWriter(gzip, Encoding.UTF8)) {
            writer.Write(payload);
        }

        return new MimePart("application", "gzip") {
            FileName = "fooddiary.club.xml.gz",
            Content = new MimeContent(new MemoryStream(compressed.ToArray())),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
        };
    }

    private static string CreateRawMessage(params MimePart[] attachments) {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse("reports@example.com"));
        message.To.Add(MailboxAddress.Parse("dmarc@fooddiary.club"));
        message.Subject = "Report Domain: fooddiary.club";

        var builder = new BodyBuilder {
            TextBody = "DMARC aggregate report",
        };
        foreach (MimePart attachment in attachments) {
            builder.Attachments.Add(attachment);
        }
        message.Body = builder.ToMessageBody();

        using var stream = new MemoryStream();
        message.WriteTo(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CreateDmarcXml() =>
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <feedback>
          <report_metadata>
            <org_name>google.com</org_name>
            <report_id>report-1</report_id>
            <date_range>
              <begin>1777161600</begin>
              <end>1777247999</end>
            </date_range>
          </report_metadata>
          <policy_published>
            <domain>fooddiary.club</domain>
          </policy_published>
          <record>
            <row>
              <source_ip>193.109.69.58</source_ip>
              <count>2</count>
              <policy_evaluated>
                <disposition>none</disposition>
                <dkim>pass</dkim>
                <spf>pass</spf>
              </policy_evaluated>
            </row>
            <identifiers>
              <header_from>fooddiary.club</header_from>
            </identifiers>
            <auth_results>
              <dkim>
                <domain>fooddiary.club</domain>
                <result>pass</result>
              </dkim>
              <spf>
                <domain>fooddiary.club</domain>
                <result>pass</result>
              </spf>
            </auth_results>
          </record>
        </feedback>
        """;
}
