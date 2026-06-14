using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Presentation.Security;

public sealed class ProviderWebhookAuthorizer(
    IOptions<MailRelayOptions> relayOptions,
    HttpClient httpClient,
    Func<X509Certificate2, bool>? certificateChainValidator = null) {
    private static readonly TimeSpan MailgunTimestampTolerance = TimeSpan.FromMinutes(15);
    private readonly MailRelayOptions _options = relayOptions.Value;
    private readonly Func<X509Certificate2, bool> _certificateChainValidator = certificateChainValidator ?? HasValidCertificateChain;

    public bool IsMailgunAuthorized(MailgunWebhookHttpRequest request) {
        if (!_options.RequireMailgunWebhookSignature) {
            return true;
        }

        if (string.IsNullOrWhiteSpace(_options.MailgunWebhookSigningKey) || request.Signature is null) {
            return false;
        }

        if (!TryValidateMailgunTimestamp(request.Signature.Timestamp)) {
            return false;
        }

        string signedValue = string.Concat(request.Signature.Timestamp, request.Signature.Token);
        byte[] keyBytes = Encoding.UTF8.GetBytes(_options.MailgunWebhookSigningKey);
        byte[] valueBytes = Encoding.UTF8.GetBytes(signedValue);
        byte[] expectedHash = HMACSHA256.HashData(keyBytes, valueBytes);
        string expectedSignature = Convert.ToHexString(expectedHash).ToLowerInvariant();

        return FixedTimeEquals(expectedSignature, request.Signature.Signature);
    }

    public async Task<bool> IsAwsSesSnsAuthorizedAsync(
        AwsSesSnsWebhookHttpRequest request,
        CancellationToken cancellationToken) {
        if (!_options.RequireAwsSesSnsSignature) {
            return true;
        }

        if (!HasRequiredSnsSignatureFields(request) ||
            !TryCreateValidatedSnsCertificateUri(request.SigningCertURL, out Uri? certificateUri)) {
            return false;
        }

        string canonical = CreateSnsCanonicalString(request);
        byte[] signature;
        try {
            signature = Convert.FromBase64String(request.Signature!);
        } catch (FormatException) {
            return false;
        }

        string certificatePem;
        try {
            certificatePem = await httpClient.GetStringAsync(certificateUri, cancellationToken).ConfigureAwait(false);
        } catch (HttpRequestException) {
            return false;
        }

        X509Certificate2 certificate;
        try {
            certificate = X509Certificate2.CreateFromPem(certificatePem);
        } catch (CryptographicException) {
            return false;
        }

        using (certificate) {
            if (!_certificateChainValidator(certificate)) {
                return false;
            }

            using RSA? rsa = certificate.GetRSAPublicKey();
            if (rsa is null) {
                return false;
            }

            HashAlgorithmName hashAlgorithm = string.Equals(request.SignatureVersion, "1", StringComparison.Ordinal)
                ? HashAlgorithmName.SHA1
                : HashAlgorithmName.SHA256;
            return rsa.VerifyData(
                Encoding.UTF8.GetBytes(canonical),
                signature,
                hashAlgorithm,
                RSASignaturePadding.Pkcs1);
        }
    }

    private static bool HasRequiredSnsSignatureFields(AwsSesSnsWebhookHttpRequest request) =>
        !string.IsNullOrWhiteSpace(request.Type) &&
        !string.IsNullOrWhiteSpace(request.Message) &&
        !string.IsNullOrWhiteSpace(request.MessageId) &&
        !string.IsNullOrWhiteSpace(request.TopicArn) &&
        !string.IsNullOrWhiteSpace(request.Timestamp) &&
        request.SignatureVersion is "1" or "2" &&
        !string.IsNullOrWhiteSpace(request.Signature) &&
        !string.IsNullOrWhiteSpace(request.SigningCertURL);

    private static bool TryCreateValidatedSnsCertificateUri(string? value, out Uri? certificateUri) {
        certificateUri = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !IsTrustedSnsCertificateHost(uri.Host) ||
            !uri.AbsolutePath.StartsWith("/SimpleNotificationService-", StringComparison.Ordinal) ||
            !uri.AbsolutePath.EndsWith(".pem", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        certificateUri = uri;
        return true;
    }

    private static bool IsTrustedSnsCertificateHost(string host) {
        string normalizedHost = host.Trim().ToLower(CultureInfo.InvariantCulture);
        return IsTrustedSnsCertificateHost(normalizedHost, ".amazonaws.com") ||
               IsTrustedSnsCertificateHost(normalizedHost, ".amazonaws.com.cn");
    }

    private static bool IsTrustedSnsCertificateHost(string host, string suffix) {
        if (!host.EndsWith(suffix, StringComparison.Ordinal)) {
            return false;
        }

        string prefix = host[..^suffix.Length];
        if (string.Equals(prefix, "sns", StringComparison.Ordinal)) {
            return true;
        }

        const string regionalPrefix = "sns.";
        if (!prefix.StartsWith(regionalPrefix, StringComparison.Ordinal)) {
            return false;
        }

        string region = prefix[regionalPrefix.Length..];
        return IsAwsRegionName(region);
    }

    private static bool IsAwsRegionName(string value) {
        string[] parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 &&
               parts[^1].All(char.IsDigit) &&
               parts.Take(parts.Length - 1).All(static part =>
                   part.Length > 0 && part.All(static c => char.IsAsciiLetterLower(c) || char.IsDigit(c)));
    }

    private static bool HasValidCertificateChain(X509Certificate2 certificate) {
        using var chain = new X509Chain {
            ChainPolicy = {
                RevocationMode = X509RevocationMode.Online,
                RevocationFlag = X509RevocationFlag.ExcludeRoot,
                VerificationFlags = X509VerificationFlags.NoFlag,
            },
        };

        return chain.Build(certificate);
    }

    private static string CreateSnsCanonicalString(AwsSesSnsWebhookHttpRequest request) {
        StringBuilder builder = new StringBuilder()
            .AppendNameValue("Message", request.Message)
            .AppendNameValue("MessageId", request.MessageId);

        if (!string.IsNullOrWhiteSpace(request.Subject)) {
            builder.AppendNameValue("Subject", request.Subject);
        }

        if (string.Equals(request.Type, "SubscriptionConfirmation", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.Type, "UnsubscribeConfirmation", StringComparison.OrdinalIgnoreCase)) {
            builder
                .AppendNameValue("SubscribeURL", request.SubscribeURL)
                .AppendNameValue("Timestamp", request.Timestamp)
                .AppendNameValue("Token", request.Token)
                .AppendNameValue("TopicArn", request.TopicArn)
                .AppendNameValue("Type", request.Type);
        } else {
            builder
                .AppendNameValue("Timestamp", request.Timestamp)
                .AppendNameValue("TopicArn", request.TopicArn)
                .AppendNameValue("Type", request.Type);
        }

        return builder.ToString();
    }

    private static bool FixedTimeEquals(string expected, string actual) {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual.Trim().ToLower(CultureInfo.InvariantCulture));
        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static bool TryValidateMailgunTimestamp(string timestamp) {
        if (!long.TryParse(timestamp, CultureInfo.InvariantCulture, out long unixSeconds)) {
            return false;
        }

        DateTimeOffset receivedAt;
        try {
            receivedAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        } catch (ArgumentOutOfRangeException) {
            return false;
        }

        TimeSpan age = DateTimeOffset.UtcNow - receivedAt;
        return age.Duration() <= MailgunTimestampTolerance;
    }
}

file static class SnsCanonicalStringBuilderExtensions {
    public static StringBuilder AppendNameValue(this StringBuilder builder, string name, string? value) =>
        builder.Append(name).Append('\n').Append(value ?? string.Empty).Append('\n');
}
