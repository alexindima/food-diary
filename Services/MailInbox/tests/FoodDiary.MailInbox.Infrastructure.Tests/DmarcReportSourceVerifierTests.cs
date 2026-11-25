using System.Security.Cryptography;
using System.Text;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Infrastructure.Services;
using MimeKit;
using MimeKit.Cryptography;
using Org.BouncyCastle.Crypto;

namespace FoodDiary.MailInbox.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class DmarcReportSourceVerifierTests {
    private const string GoogleSender = "noreply-dmarc-support@google.com";
    private const string GoogleDomain = "google.com";
    private const string ReportDomain = "fooddiary.club";
    private const string Selector = "report";
    private static readonly DmarcReportPreview Report = new(GoogleDomain, "report-id", ReportDomain, DateRangeStartUtc: null, DateRangeEndUtc: null, []);

    [Theory]
    [InlineData(GoogleSender, GoogleDomain, GoogleDomain)]
    [InlineData("dmarc_support@corp.mail.ru", "mail.ru", "Mail.Ru.")]
    [InlineData("dmarc-support@alerts.comcast.net", "alerts.comcast.net", "comcast.net")]
    public async Task IsVerifiedAsync_WhenProviderSignsTheFullReport_ReturnsTrue(
        string source,
        string signerDomain,
        string organization) {
        (byte[] rawMime, StaticKeyLocator locator) = CreateSignedReport(
            source,
            signerDomain,
            ["From", "Content-Type", "Content-Transfer-Encoding"]);
        var verifier = new DmarcReportSourceVerifier(locator);

        bool verified = await verifier.IsVerifiedAsync(
            rawMime,
            Report with { OrganizationName = organization },
            CancellationToken.None);

        Assert.True(verified);
        Assert.Equal(1, locator.LookupCount);
    }

    [Theory]
    [InlineData("someone@google.com", GoogleDomain, GoogleDomain, ReportDomain)]
    [InlineData(GoogleSender, "other.example", GoogleDomain, ReportDomain)]
    [InlineData(GoogleSender, GoogleDomain, "other.example", ReportDomain)]
    [InlineData(GoogleSender, GoogleDomain, GoogleDomain, "other.example")]
    public async Task IsVerifiedAsync_WhenReportIdentityIsOutsideScope_DoesNotResolveAKey(
        string source,
        string signerDomain,
        string organization,
        string reportDomain) {
        (byte[] rawMime, StaticKeyLocator locator) = CreateSignedReport(
            source,
            signerDomain,
            ["From", "Content-Type", "Content-Transfer-Encoding"]);
        var verifier = new DmarcReportSourceVerifier(locator);

        bool verified = await verifier.IsVerifiedAsync(
            rawMime,
            Report with { Domain = reportDomain, OrganizationName = organization },
            CancellationToken.None);

        Assert.False(verified);
        Assert.Equal(0, locator.LookupCount);
    }

    [Theory]
    [InlineData("Content-Type")]
    [InlineData("Content-Transfer-Encoding")]
    public async Task IsVerifiedAsync_WhenTheSignatureOmitsABodyHeader_DoesNotResolveAKey(string omittedHeader) {
        string[] signedHeaders = [.. new[] { "From", "Content-Type", "Content-Transfer-Encoding" }
            .Where(header => !string.Equals(header, omittedHeader, StringComparison.Ordinal))];
        (byte[] rawMime, StaticKeyLocator locator) = CreateSignedReport(GoogleSender, GoogleDomain, signedHeaders);
        var verifier = new DmarcReportSourceVerifier(locator);

        bool verified = await verifier.IsVerifiedAsync(rawMime, Report, CancellationToken.None);

        Assert.False(verified);
        Assert.Equal(0, locator.LookupCount);
    }

    [Fact]
    public async Task IsVerifiedAsync_WhenBodyChangesAfterSigning_ReturnsFalse() {
        (byte[] rawMime, StaticKeyLocator locator) = CreateSignedReport(
            GoogleSender,
            GoogleDomain,
            ["From", "Content-Type", "Content-Transfer-Encoding"],
            static message => ((TextPart)message.Body!).Text = "Changed report.");
        var verifier = new DmarcReportSourceVerifier(locator);

        bool verified = await verifier.IsVerifiedAsync(rawMime, Report, CancellationToken.None);

        Assert.False(verified);
    }

    [Theory]
    [InlineData("From", GoogleSender)]
    [InlineData("Content-Type", "text/plain; charset=utf-8")]
    [InlineData("Content-Transfer-Encoding", "base64")]
    public async Task IsVerifiedAsync_WhenTopLevelHeaderIsAmbiguous_DoesNotResolveAKey(string name, string value) {
        (byte[] rawMime, StaticKeyLocator locator) = CreateSignedReport(
            GoogleSender,
            GoogleDomain,
            ["From", "Content-Type", "Content-Transfer-Encoding"]);
        byte[] ambiguousMime = [.. Encoding.ASCII.GetBytes($"{name}: {value}\r\n"), .. rawMime];
        var verifier = new DmarcReportSourceVerifier(locator);

        bool verified = await verifier.IsVerifiedAsync(ambiguousMime, Report, CancellationToken.None);

        Assert.False(verified);
        Assert.Equal(0, locator.LookupCount);
    }

    private static (byte[] RawMime, StaticKeyLocator Locator) CreateSignedReport(
        string from,
        string domain,
        string[] signedHeaders,
        Action<MimeMessage>? afterSigning = null) {
        using var rsa = RSA.Create();
        string publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
        using var privateKey = new MemoryStream(Encoding.ASCII.GetBytes(rsa.ExportRSAPrivateKeyPem()));
        var signer = new DkimSigner(privateKey, domain, Selector) {
            HeaderCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Relaxed,
            BodyCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Relaxed,
            SignatureAlgorithm = DkimSignatureAlgorithm.RsaSha256,
        };

        using var message = new MimeMessage {
            Body = new TextPart("plain") { Text = "Example report content.", ContentTransferEncoding = ContentEncoding.Base64 },
        };
        message.From.Add(MailboxAddress.Parse(from));
        FormatOptions format = FormatOptions.Default.Clone();
        format.NewLineFormat = NewLineFormat.Dos;
        signer.Sign(format, message, signedHeaders);
        afterSigning?.Invoke(message);

        using var output = new MemoryStream();
        message.WriteTo(format, output);
        return (output.ToArray(), new StaticKeyLocator($"v=DKIM1; k=rsa; p={publicKey}"));
    }

    [ExcludeFromCodeCoverage]
    private sealed class StaticKeyLocator(string record) : DkimPublicKeyLocatorBase {
        public int LookupCount { get; private set; }

        public override AsymmetricKeyParameter LocatePublicKey(
            string methods,
            string domain,
            string selector,
            CancellationToken cancellationToken = default) =>
            ReadKey(cancellationToken);

        private AsymmetricKeyParameter ReadKey(CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            LookupCount += 1;
            return GetPublicKey(record);
        }

        public override Task<AsymmetricKeyParameter> LocatePublicKeyAsync(
            string methods,
            string domain,
            string selector,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ReadKey(cancellationToken));
    }
}
