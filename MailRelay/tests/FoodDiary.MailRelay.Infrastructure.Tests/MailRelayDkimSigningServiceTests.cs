using System.Security.Cryptography;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using MimeKit;

namespace FoodDiary.MailRelay.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayDkimSigningServiceTests {
    [Fact]
    public void Sign_WhenDisabled_DoesNotAddDkimSignature() {
        DkimSigningService service = CreateService(new MailRelayDkimOptions());
        MimeMessage message = CreateMessage();

        service.Sign(message);

        Assert.False(message.Headers.Contains("DKIM-Signature"));
    }

    [Fact]
    public void Sign_WhenEnabled_AddsDkimSignatureAndMessageId() {
        using var rsa = RSA.Create(1024);
        DkimSigningService service = CreateService(new MailRelayDkimOptions {
            Enabled = true,
            Domain = "example.com",
            Selector = "mail",
            PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(),
        });
        MimeMessage message = CreateMessage();
        message.Date = DateTimeOffset.MinValue;
        message.Headers.Remove(HeaderId.MessageId);

        service.Sign(message);

        Assert.True(message.Headers.Contains("DKIM-Signature"));
        Assert.False(string.IsNullOrWhiteSpace(message.MessageId));
        Assert.Equal(FixedNow, message.Date);
    }

    [Fact]
    public void Sign_WhenPrivateKeyPathIsConfigured_ReadsKeyFromFileAndKeepsExistingHeaders() {
        using var rsa = RSA.Create(1024);
        string privateKeyPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pem");
        File.WriteAllText(privateKeyPath, rsa.ExportPkcs8PrivateKeyPem());
        DkimSigningService service = CreateService(new MailRelayDkimOptions {
            Enabled = true,
            Domain = "example.com",
            Selector = "mail",
            PrivateKeyPath = privateKeyPath,
        });
        MimeMessage message = CreateMessage();
        DateTimeOffset date = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        message.Date = date;
        message.MessageId = "existing@example.com";

        try {
            service.Sign(message);
        } finally {
            File.Delete(privateKeyPath);
        }

        Assert.True(message.Headers.Contains("DKIM-Signature"));
        Assert.Equal(date, message.Date);
        Assert.Equal("existing@example.com", message.MessageId);
    }

    [Fact]
    public void Sign_WhenDomainOrSelectorIsMissing_ThrowsConfigurationError() {
        DkimSigningService missingDomain = CreateService(new MailRelayDkimOptions {
            Enabled = true,
            Selector = "mail",
            PrivateKeyPem = "invalid",
        });
        DkimSigningService missingSelector = CreateService(new MailRelayDkimOptions {
            Enabled = true,
            Domain = "example.com",
            PrivateKeyPem = "invalid",
        });

        Assert.Throws<InvalidOperationException>(() => missingDomain.Sign(CreateMessage()));
        Assert.Throws<InvalidOperationException>(() => missingSelector.Sign(CreateMessage()));
    }

    [Fact]
    public void Sign_WhenPrivateKeyIsMissing_ThrowsConfigurationError() {
        DkimSigningService service = CreateService(new MailRelayDkimOptions {
            Enabled = true,
            Domain = "example.com",
            Selector = "mail",
        });

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => service.Sign(CreateMessage()));

        Assert.Contains("private key", ex.Message, StringComparison.Ordinal);
    }

    private static MimeMessage CreateMessage() {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse("relay@example.com"));
        message.To.Add(MailboxAddress.Parse("user@example.com"));
        message.Subject = "Subject";
        message.Body = new TextPart("plain") { Text = "Body" };
        return message;
    }

    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    private static DkimSigningService CreateService(MailRelayDkimOptions options) =>
        new(Microsoft.Extensions.Options.Options.Create(options), FixedTime);

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => FixedNow;
    }
}
