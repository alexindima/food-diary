using System.Security.Cryptography;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FoodDiary.MailRelay.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayDkimSigningServiceTests {
    [Fact]
    public void Sign_WhenDisabled_DoesNotAddDkimSignature() {
        var service = new DkimSigningService(Options.Create(new MailRelayDkimOptions()));
        MimeMessage message = CreateMessage();

        service.Sign(message);

        Assert.False(message.Headers.Contains("DKIM-Signature"));
    }

    [Fact]
    public void Sign_WhenEnabled_AddsDkimSignatureAndMessageId() {
        using var rsa = RSA.Create(1024);
        var service = new DkimSigningService(Options.Create(new MailRelayDkimOptions {
            Enabled = true,
            Domain = "example.com",
            Selector = "mail",
            PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(),
        }));
        MimeMessage message = CreateMessage();

        service.Sign(message);

        Assert.True(message.Headers.Contains("DKIM-Signature"));
        Assert.False(string.IsNullOrWhiteSpace(message.MessageId));
    }

    [Fact]
    public void Sign_WhenDomainOrSelectorIsMissing_ThrowsConfigurationError() {
        var missingDomain = new DkimSigningService(Options.Create(new MailRelayDkimOptions {
            Enabled = true,
            Selector = "mail",
            PrivateKeyPem = "invalid",
        }));
        var missingSelector = new DkimSigningService(Options.Create(new MailRelayDkimOptions {
            Enabled = true,
            Domain = "example.com",
            PrivateKeyPem = "invalid",
        }));

        Assert.Throws<InvalidOperationException>(() => missingDomain.Sign(CreateMessage()));
        Assert.Throws<InvalidOperationException>(() => missingSelector.Sign(CreateMessage()));
    }

    [Fact]
    public void Sign_WhenPrivateKeyIsMissing_ThrowsConfigurationError() {
        var service = new DkimSigningService(Options.Create(new MailRelayDkimOptions {
            Enabled = true,
            Domain = "example.com",
            Selector = "mail",
        }));

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
}
