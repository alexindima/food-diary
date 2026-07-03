using FoodDiary.Application.Abstractions.Email.Common;
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
        var message = new EmailMessage(
            "from@example.com",
            "Sender",
            ["to@example.com"],
            "Subject",
            "<p>Hello</p>",
            "Hello");

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
        var message = new EmailMessage(
            FromAddress: string.Empty,
            FromName: string.Empty,
            ToAddresses: ["to@example.com"],
            Subject: "Subject",
            HtmlBody: "<p>Hello</p>",
            TextBody: null);

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
        var message = new EmailMessage(
            "from@example.com",
            string.Empty,
            ["to@example.com"],
            "Subject",
            "<p>Hello</p>",
            TextBody: null);

        await transport.SendAsync(message, CancellationToken.None);

        Assert.NotNull(request);
        Assert.Null(request.TextBody);
    }
}
