using DnsClient;
using DnsClient.Protocol;
using MimeKit.Cryptography;
using Org.BouncyCastle.Crypto;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class DmarcReportDnsKeyLocator(ILookupClient dns) : DkimPublicKeyLocatorBase {
    private const int MaxSelectorLength = 192;
    private const int MaxLabelLength = 63;

    public override AsymmetricKeyParameter LocatePublicKey(
        string methods,
        string domain,
        string selector,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("DMARC signing keys are resolved asynchronously.");

    public override async Task<AsymmetricKeyParameter> LocatePublicKeyAsync(
        string methods,
        string domain,
        string selector,
        CancellationToken cancellationToken = default) {
        if (!DmarcReportSourceVerifier.IsAllowedSigner(domain) ||
            !IsValidSelector(selector) ||
            !methods.Split(':', StringSplitOptions.TrimEntries).Contains("dns/txt", StringComparer.OrdinalIgnoreCase)) {
            throw new NotSupportedException("The DMARC signing key lookup is not supported.");
        }

        string name = $"{selector}._domainkey.{domain}.";
        IDnsQueryResponse answer = await dns.QueryAsync(name, QueryType.TXT, cancellationToken: cancellationToken);
        TxtRecord[] records = [.. answer.Answers.TxtRecords().Take(2)];
        if (answer.HasError || records.Length != 1) {
            throw new FormatException("The DMARC signing key lookup did not return a single key record.");
        }

        return GetPublicKey(string.Concat(records[0].Text));
    }

    private static bool IsValidSelector(string selector) =>
        selector.Length <= MaxSelectorLength &&
        selector.Split('.', StringSplitOptions.None).All(IsValidLabel);

    private static bool IsValidLabel(string label) =>
        label.Length is > 0 and <= MaxLabelLength &&
        char.IsAsciiLetterOrDigit(label[0]) &&
        char.IsAsciiLetterOrDigit(label[^1]) &&
        label.All(static character => char.IsAsciiLetterOrDigit(character) || character == '-');
}
