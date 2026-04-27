using System.IO.Compression;
using System.Text;
using FoodDiary.MailInbox.Infrastructure.Services;
using MimeKit;

namespace FoodDiary.MailInbox.Tests;

public sealed class DmarcReportParserTests {
    [Fact]
    public void TryParse_WhenMessageContainsGzipDmarcReport_ReturnsPreview() {
        var rawMime = CreateRawMessage(CreateGzipReportAttachment());
        var parser = new DmarcReportParser();

        var report = parser.TryParse(rawMime);

        Assert.NotNull(report);
        Assert.Equal("google.com", report.OrganizationName);
        Assert.Equal("fooddiary.club", report.Domain);
        Assert.Equal("report-1", report.ReportId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1777161600), report.DateRangeStartUtc);
        var record = Assert.Single(report.Records);
        Assert.Equal("193.109.69.58", record.SourceIp);
        Assert.Equal(2, record.Count);
        Assert.Equal("none", record.Disposition);
        Assert.Equal("pass", record.Dkim);
        Assert.Equal("pass", record.Spf);
    }

    [Fact]
    public void TryParse_WhenMessageDoesNotContainDmarcReport_ReturnsNull() {
        var parser = new DmarcReportParser();

        var report = parser.TryParse("From: sender@example.com\r\nSubject: Hello\r\n\r\nPlain message");

        Assert.Null(report);
    }

    private static MimePart CreateGzipReportAttachment() {
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionMode.Compress, leaveOpen: true))
        using (var writer = new StreamWriter(gzip, Encoding.UTF8)) {
            writer.Write("""
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
                         """);
        }

        return new MimePart("application", "gzip") {
            FileName = "fooddiary.club.xml.gz",
            Content = new MimeContent(new MemoryStream(compressed.ToArray())),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64
        };
    }

    private static string CreateRawMessage(MimePart attachment) {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse("reports@example.com"));
        message.To.Add(MailboxAddress.Parse("dmarc@fooddiary.club"));
        message.Subject = "Report Domain: fooddiary.club";

        var builder = new BodyBuilder {
            TextBody = "DMARC aggregate report"
        };
        builder.Attachments.Add(attachment);
        message.Body = builder.ToMessageBody();

        using var stream = new MemoryStream();
        message.WriteTo(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
