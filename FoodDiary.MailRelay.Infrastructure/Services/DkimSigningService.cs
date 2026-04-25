using MimeKit;
using MimeKit.Cryptography;
using MimeKit.Utils;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class DkimSigningService(IOptions<MailRelayDkimOptions> options) {
    private static readonly string[] HeadersToSign = ["From", "To", "Subject", "Date", "Message-Id", "MIME-Version", "Content-Type"];
    private readonly MailRelayDkimOptions _options = options.Value;

    public bool IsEnabled => _options.Enabled;

    public void Sign(MimeMessage message) {
        if (!_options.Enabled) {
            return;
        }

        var domain = _options.Domain ?? throw new InvalidOperationException("MailRelayDkim domain is not configured.");
        var selector = _options.Selector ?? throw new InvalidOperationException("MailRelayDkim selector is not configured.");

        if (message.Date == DateTimeOffset.MinValue) {
            message.Date = DateTimeOffset.UtcNow;
        }

        if (string.IsNullOrWhiteSpace(message.MessageId)) {
            message.MessageId = MimeUtils.GenerateMessageId(domain);
        }

        message.Prepare(EncodingConstraint.SevenBit);

        using var privateKeyStream = OpenPrivateKeyStream();
        var signer = new DkimSigner(privateKeyStream, domain, selector) {
            HeaderCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Relaxed,
            BodyCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Relaxed,
            SignatureAlgorithm = DkimSignatureAlgorithm.RsaSha256
        };

        signer.Sign(message, HeadersToSign);
    }

    private Stream OpenPrivateKeyStream() {
        if (!string.IsNullOrWhiteSpace(_options.PrivateKeyPem)) {
            return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_options.PrivateKeyPem));
        }

        if (string.IsNullOrWhiteSpace(_options.PrivateKeyPath)) {
            throw new InvalidOperationException("MailRelayDkim private key is not configured.");
        }

        return File.OpenRead(_options.PrivateKeyPath);
    }
}
