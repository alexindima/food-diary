using System.Security.Cryptography;
using System.Text;
using DnsClient;
using FoodDiary.MailInbox.Application.Messages.Models;
using MimeKit;
using MimeKit.Cryptography;
using Org.BouncyCastle.Crypto;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class DmarcReportSourceVerifier(IDkimPublicKeyLocator publicKeyLocator) {
    private const int MaxSignatures = 8;
    private const int MaxSignatureBytes = 32 * 1024;
    private static readonly TimeSpan VerificationTimeout = TimeSpan.FromSeconds(2);
    private static readonly ReportSource[] Sources = [
        new("noreply-dmarc-support@google.com", "google.com", ["google.com"]),
        new("dmarc_support@corp.mail.ru", "mail.ru", ["mail.ru", "corp.mail.ru"]),
        new("dmarc-support@alerts.comcast.net", "comcast.net", ["comcast.net", "alerts.comcast.net"]),
    ];

    public async Task<bool> IsVerifiedAsync(
        byte[] rawMime,
        DmarcReportPreview report,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(NormalizeDomain(report.Domain), "fooddiary.club", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(VerificationTimeout);
        try {
            await using var stream = new MemoryStream(rawMime, writable: false);
            using MimeMessage message = await MimeMessage.LoadAsync(stream, budget.Token);
            return await VerifyMessageAsync(message, report.OrganizationName, budget.Token);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return false;
        } catch (Exception error) when (IsInvalidSignatureError(error)) {
            return false;
        }
    }

    internal static bool IsAllowedSigner(string domain) =>
        Sources.Any(source => source.SigningDomains.Contains(domain, StringComparer.OrdinalIgnoreCase));

    private async Task<bool> VerifyMessageAsync(
        MimeMessage message,
        string? organizationName,
        CancellationToken cancellationToken) {
        ReportSource? source = GetSource(message, organizationName);
        if (source is null || CountHeaders(message, HeaderId.ContentType) != 1) {
            return false;
        }

        int encodingHeaders = CountHeaders(message, HeaderId.ContentTransferEncoding);
        if (encodingHeaders > 1) {
            return false;
        }

        Header[] signatures = [.. message.Headers
            .Where(static header => header.Id == HeaderId.DkimSignature)
            .Take(MaxSignatures + 1)];
        if (signatures.Length > MaxSignatures) {
            return false;
        }

        var verifier = new DkimVerifier(publicKeyLocator);
        foreach (Header signature in signatures) {
            if (!IsAllowedSignature(signature, source, encodingHeaders == 1)) {
                continue;
            }

            try {
                if (await verifier.VerifyAsync(message, signature, cancellationToken)) {
                    return true;
                }
            } catch (Exception error) when (IsInvalidSignatureError(error)) {
                // A malformed signature does not invalidate another authorized signature.
            }
        }

        return false;
    }

    private static ReportSource? GetSource(MimeMessage message, string? organizationName) {
        if (message.Headers.Count(static header => header.Id == HeaderId.From) != 1 ||
            message.From.Count != 1 || message.From[0] is not MailboxAddress sender) {
            return null;
        }

        string? organization = NormalizeDomain(organizationName);
        return Sources.FirstOrDefault(source =>
            string.Equals(sender.Address, source.Address, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(organization, source.Organization, StringComparison.OrdinalIgnoreCase));
    }

    private static int CountHeaders(MimeMessage message, HeaderId id) =>
        message.Headers.Count(header => header.Id == id) +
        (message.Body?.Headers.Count(header => header.Id == id) ?? 0);

    private static bool IsAllowedSignature(Header header, ReportSource source, bool mustSignEncoding) {
        if (header.RawValue.Length > MaxSignatureBytes || !TryReadSignatureTags(header, out Dictionary<string, string> tags)) {
            return false;
        }

        if (tags.ContainsKey("l") ||
            !tags.TryGetValue("d", out string? domain) ||
            !source.SigningDomains.Contains(domain, StringComparer.OrdinalIgnoreCase) ||
            !tags.TryGetValue("a", out string? algorithm) ||
            !(string.Equals(algorithm, "rsa-sha256", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(algorithm, "ed25519-sha256", StringComparison.OrdinalIgnoreCase)) ||
            !tags.TryGetValue("h", out string? headerList)) {
            return false;
        }

        string[] signedHeaders = headerList.Split(':', StringSplitOptions.TrimEntries);
        return signedHeaders.Contains("from", StringComparer.OrdinalIgnoreCase) &&
            signedHeaders.Contains("content-type", StringComparer.OrdinalIgnoreCase) &&
            (!mustSignEncoding || signedHeaders.Contains("content-transfer-encoding", StringComparer.OrdinalIgnoreCase));
    }

    private static bool TryReadSignatureTags(Header header, out Dictionary<string, string> tags) {
        tags = [with(StringComparer.OrdinalIgnoreCase)];
        string value = Encoding.ASCII.GetString(header.RawValue);
        foreach (string tag in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            int separator = tag.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0 || !tags.TryAdd(tag[..separator].Trim(), tag[(separator + 1)..].Trim())) {
                return false;
            }
        }

        return true;
    }

    private static bool IsInvalidSignatureError(Exception error) =>
        error is ArgumentException or FormatException or InvalidOperationException or IOException or
            NotSupportedException or DnsResponseException or CryptographicException or CryptoException;

    private static string? NormalizeDomain(string? value) => value?.Trim().TrimEnd('.');

    private sealed record ReportSource(string Address, string Organization, string[] SigningDomains);
}
