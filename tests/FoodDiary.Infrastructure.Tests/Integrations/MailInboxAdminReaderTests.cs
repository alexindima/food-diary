using FoodDiary.Integrations.Services.MailInbox;
using FoodDiary.MailInbox.Client;
using FoodDiary.MailInbox.Client.Models;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class MailInboxAdminReaderTests {
    [Fact]
    public async Task GetMessagesAsync_MapsClientSummaries() {
        var id = Guid.NewGuid();
        var receivedAtUtc = DateTimeOffset.UtcNow;
        var client = new StubMailInboxClient {
            Summaries = [
                new InboundMailMessageSummaryResponse(
                    id,
                    "from@example.com",
                    ["to@example.com"],
                    "Subject",
                    "received",
                    receivedAtUtc)
            ]
        };
        var reader = new MailInboxClientAdminMailInboxReader(client);

        var result = await reader.GetMessagesAsync(25, CancellationToken.None);

        var message = Assert.Single(result);
        Assert.Equal(id, message.Id);
        Assert.Equal("from@example.com", message.FromAddress);
        Assert.Equal(["to@example.com"], message.ToRecipients);
        Assert.Equal("Subject", message.Subject);
        Assert.Equal("received", message.Status);
        Assert.Equal(receivedAtUtc, message.ReceivedAtUtc);
        Assert.Equal(25, client.LastLimit);
    }

    [Fact]
    public async Task GetMessageAsync_WhenClientReturnsDetails_MapsDetails() {
        var id = Guid.NewGuid();
        var receivedAtUtc = DateTimeOffset.UtcNow;
        var client = new StubMailInboxClient {
            Details = new InboundMailMessageDetailsResponse(
                id,
                "message-id",
                "from@example.com",
                ["to@example.com"],
                "Subject",
                "text",
                "<p>html</p>",
                "raw",
                "received",
                receivedAtUtc)
        };
        var reader = new MailInboxClientAdminMailInboxReader(client);

        var result = await reader.GetMessageAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("message-id", result.MessageId);
        Assert.Equal("text", result.TextBody);
        Assert.Equal("<p>html</p>", result.HtmlBody);
        Assert.Equal("raw", result.RawMime);
        Assert.Equal(id, client.LastMessageId);
    }

    [Fact]
    public async Task GetMessageAsync_WhenClientReturnsNull_ReturnsNull() {
        var reader = new MailInboxClientAdminMailInboxReader(new StubMailInboxClient());

        var result = await reader.GetMessageAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubMailInboxClient : IMailInboxClient {
        public IReadOnlyList<InboundMailMessageSummaryResponse> Summaries { get; init; } = [];
        public InboundMailMessageDetailsResponse? Details { get; init; }
        public int? LastLimit { get; private set; }
        public Guid LastMessageId { get; private set; }

        public Task<IReadOnlyList<InboundMailMessageSummaryResponse>> GetMessagesAsync(
            int? limit,
            CancellationToken cancellationToken) {
            LastLimit = limit;
            return Task.FromResult(Summaries);
        }

        public Task<InboundMailMessageDetailsResponse?> GetMessageAsync(
            Guid id,
            CancellationToken cancellationToken) {
            LastMessageId = id;
            return Task.FromResult(Details);
        }
    }
}
