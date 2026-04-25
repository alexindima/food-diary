using System.Security.Cryptography;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FoodDiary.MailRelay.Tests;

public sealed class MailRelayDkimSigningServiceTests {
    [Fact]
    public void Sign_WhenDisabled_DoesNotAddDkimSignature() {
        var service = new DkimSigningService(Options.Create(new MailRelayDkimOptions()));
        var message = CreateMessage();

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
            PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem()
        }));
        var message = CreateMessage();

        service.Sign(message);

        Assert.True(message.Headers.Contains("DKIM-Signature"));
        Assert.False(string.IsNullOrWhiteSpace(message.MessageId));
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
