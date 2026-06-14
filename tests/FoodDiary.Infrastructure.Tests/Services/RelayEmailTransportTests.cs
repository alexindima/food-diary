using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using FoodDiary.Integrations.Services;
using FoodDiary.MailRelay.Client;
using FoodDiary.MailRelay.Client.Models;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class RelayEmailTransportTests {
    [Fact]
    public async Task SendAsync_EnqueuesRelayPayloadWithPlainTextAlternateView() {
        var client = new RecordingMailRelayClient();
        var transport = new RelayEmailTransport(client);
        using var message = new MailMessage {
            From = new MailAddress("from@example.com", "Sender"),
            Subject = "Subject",
            Body = "<p>Hello</p>",
            IsBodyHtml = true,
        };
        message.To.Add("to@example.com");
        using var plainText = AlternateView.CreateAlternateViewFromString("Hello", Encoding.UTF8, MediaTypeNames.Text.Plain);
        message.AlternateViews.Add(plainText);

        await transport.SendAsync(message, CancellationToken.None);

        Assert.NotNull(client.Request);
        Assert.Equal("from@example.com", client.Request.FromAddress);
        Assert.Equal("Sender", client.Request.FromName);
        Assert.Equal(["to@example.com"], client.Request.To);
        Assert.Equal("Subject", client.Request.Subject);
        Assert.Equal("<p>Hello</p>", client.Request.HtmlBody);
        Assert.Equal("Hello", client.Request.TextBody);
        Assert.False(string.IsNullOrWhiteSpace(client.Request.CorrelationId));
    }

    [Fact]
    public async Task SendAsync_WhenFromMissing_ThrowsInvalidOperationException() {
        var client = new RecordingMailRelayClient();
        var transport = new RelayEmailTransport(client);
        using var message = new MailMessage {
            Subject = "Subject",
            Body = "<p>Hello</p>",
        };
        message.To.Add("to@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            transport.SendAsync(message, CancellationToken.None));

        Assert.Null(client.Request);
    }

    [Fact]
    public async Task SendAsync_WhenPlainTextAlternateViewMissing_EnqueuesNullTextBody() {
        var client = new RecordingMailRelayClient();
        var transport = new RelayEmailTransport(client);
        using var message = new MailMessage {
            From = new MailAddress("from@example.com"),
            Subject = "Subject",
            Body = "<p>Hello</p>",
            IsBodyHtml = true,
        };
        message.To.Add("to@example.com");
        using var htmlView = AlternateView.CreateAlternateViewFromString("<p>Hello</p>", Encoding.UTF8, MediaTypeNames.Text.Html);
        message.AlternateViews.Add(htmlView);

        await transport.SendAsync(message, CancellationToken.None);

        Assert.NotNull(client.Request);
        Assert.Null(client.Request.TextBody);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingMailRelayClient : IMailRelayClient {
        public EnqueueMailRelayEmailRequest? Request { get; private set; }

        public Task<EnqueueMailRelayEmailResponse> EnqueueAsync(
            EnqueueMailRelayEmailRequest request,
            CancellationToken cancellationToken) {
            Request = request;
            return Task.FromResult(new EnqueueMailRelayEmailResponse(Guid.NewGuid(), "queued"));
        }
    }
}
