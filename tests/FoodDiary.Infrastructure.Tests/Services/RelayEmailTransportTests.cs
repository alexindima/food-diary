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
        IMailRelayClient client = Substitute.For<IMailRelayClient>();
        EnqueueMailRelayEmailRequest? request = null;
        client
            .EnqueueAsync(Arg.Do<EnqueueMailRelayEmailRequest>(value => request = value), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EnqueueMailRelayEmailResponse(Guid.NewGuid(), "queued")));
        RelayEmailTransport transport = new(client);
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

        Assert.NotNull(request);
        Assert.Equal("from@example.com", request.FromAddress);
        Assert.Equal("Sender", request.FromName);
        Assert.Equal(["to@example.com"], request.To);
        Assert.Equal("Subject", request.Subject);
        Assert.Equal("<p>Hello</p>", request.HtmlBody);
        Assert.Equal("Hello", request.TextBody);
        Assert.False(string.IsNullOrWhiteSpace(request.CorrelationId));
    }

    [Fact]
    public async Task SendAsync_WhenFromMissing_ThrowsInvalidOperationException() {
        IMailRelayClient client = Substitute.For<IMailRelayClient>();
        RelayEmailTransport transport = new(client);
        using var message = new MailMessage {
            Subject = "Subject",
            Body = "<p>Hello</p>",
        };
        message.To.Add("to@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            transport.SendAsync(message, CancellationToken.None));

        await client.DidNotReceive().EnqueueAsync(Arg.Any<EnqueueMailRelayEmailRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenPlainTextAlternateViewMissing_EnqueuesNullTextBody() {
        IMailRelayClient client = Substitute.For<IMailRelayClient>();
        EnqueueMailRelayEmailRequest? request = null;
        client
            .EnqueueAsync(Arg.Do<EnqueueMailRelayEmailRequest>(value => request = value), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EnqueueMailRelayEmailResponse(Guid.NewGuid(), "queued")));
        RelayEmailTransport transport = new(client);
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

        Assert.NotNull(request);
        Assert.Null(request.TextBody);
    }
}
