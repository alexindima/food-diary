using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Integrations.Services.MailInbox;
using FoodDiary.MailInbox.Client;
using FoodDiary.MailInbox.Client.Models;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class MailInboxAdminReaderTests {
    [Fact]
    public async Task GetMessagesAsync_MapsClientSummaries() {
        var id = Guid.NewGuid();
        DateTimeOffset receivedAtUtc = DateTimeOffset.UtcNow;
        IReadOnlyList<InboundMailMessageSummaryResponse> summaries = [
            new InboundMailMessageSummaryResponse(
                id,
                "from@example.com",
                ["to@example.com"],
                "Subject",
                "general",
                "received",
                ReadAtUtc: null,
                receivedAtUtc),
        ];
        IMailInboxClient client = Substitute.For<IMailInboxClient>();
        int? lastLimit = null;
        client
            .GetMessagesAsync(Arg.Do<int?>(limit => lastLimit = limit), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(summaries));
        MailInboxClientAdminMailInboxReader reader = new(client);

        IReadOnlyList<AdminMailInboxMessageSummaryModel> result = await reader.GetMessagesAsync(25, CancellationToken.None);

        AdminMailInboxMessageSummaryModel message = Assert.Single(result);
        Assert.Equal(id, message.Id);
        Assert.Equal("from@example.com", message.FromAddress);
        Assert.Equal(["to@example.com"], message.ToRecipients);
        Assert.Equal("Subject", message.Subject);
        Assert.Equal("general", message.Category);
        Assert.Equal("received", message.Status);
        Assert.Null(message.ReadAtUtc);
        Assert.Equal(receivedAtUtc, message.ReceivedAtUtc);
        Assert.Equal(25, lastLimit);
    }

    [Fact]
    public async Task GetMessageAsync_WhenClientReturnsDetails_MapsDetails() {
        var id = Guid.NewGuid();
        DateTimeOffset receivedAtUtc = DateTimeOffset.UtcNow;
        InboundMailMessageDetailsResponse details = new(
            id,
            "message-id",
            "from@example.com",
            ["to@example.com"],
            "Subject",
            "text",
            "<p>html</p>",
            "raw",
            "general",
            "received",
            ReadAtUtc: null,
            receivedAtUtc);
        IMailInboxClient client = Substitute.For<IMailInboxClient>();
        Guid lastMessageId = Guid.Empty;
        client
            .GetMessageAsync(Arg.Do<Guid>(value => lastMessageId = value), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InboundMailMessageDetailsResponse?>(details));
        MailInboxClientAdminMailInboxReader reader = new(client);

        AdminMailInboxMessageDetailsModel? result = await reader.GetMessageAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("message-id", result.MessageId);
        Assert.Equal("text", result.TextBody);
        Assert.Equal("<p>html</p>", result.HtmlBody);
        Assert.Equal("raw", result.RawMime);
        Assert.Equal("general", result.Category);
        Assert.Equal(id, lastMessageId);
    }

    [Fact]
    public async Task GetMessageAsync_WhenClientReturnsNull_ReturnsNull() {
        IMailInboxClient client = Substitute.For<IMailInboxClient>();
        client
            .GetMessageAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InboundMailMessageDetailsResponse?>(null));
        MailInboxClientAdminMailInboxReader reader = new(client);

        AdminMailInboxMessageDetailsModel? result = await reader.GetMessageAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task MarkMessageReadAsync_DelegatesToClient() {
        var id = Guid.NewGuid();
        IMailInboxClient client = Substitute.For<IMailInboxClient>();
        Guid lastReadMessageId = Guid.Empty;
        client
            .MarkMessageReadAsync(Arg.Do<Guid>(value => lastReadMessageId = value), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        MailInboxClientAdminMailInboxReader reader = new(client);

        bool result = await reader.MarkMessageReadAsync(id, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(id, lastReadMessageId);
    }
}
