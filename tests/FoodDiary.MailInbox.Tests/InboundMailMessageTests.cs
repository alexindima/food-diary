using FoodDiary.MailInbox.Domain.Events;
using FoodDiary.MailInbox.Domain.Messages;

namespace FoodDiary.MailInbox.Tests;

public sealed class InboundMailMessageTests {
    [Fact]
    public void Receive_WhenValuesAreValid_CreatesReceivedAggregate() {
        var receivedAtUtc = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);

        var message = InboundMailMessage.Receive(
            " message-id ",
            " sender@example.com ",
            [" admin@fooddiary.club "],
            " subject ",
            "text",
            "<p>html</p>",
            "raw",
            receivedAtUtc);

        Assert.NotEqual(Guid.Empty, message.Id.Value);
        Assert.Equal("message-id", message.MessageId);
        Assert.Equal("sender@example.com", message.FromAddress);
        Assert.Equal(["admin@fooddiary.club"], message.ToRecipients);
        Assert.Equal("subject", message.Subject);
        Assert.Equal(InboundMailMessageStatus.Received, message.Status);
        Assert.Equal(receivedAtUtc, message.ReceivedAtUtc);
        Assert.Single(message.DomainEvents);
        Assert.IsType<InboundMailMessageReceivedDomainEvent>(message.DomainEvents[0]);
    }

    [Fact]
    public void Archive_WhenMessageIsReceived_ChangesStatus() {
        var message = InboundMailMessage.Receive(
            null,
            null,
            ["admin@fooddiary.club"],
            null,
            null,
            null,
            "raw",
            DateTimeOffset.UtcNow);

        message.Archive(DateTimeOffset.UtcNow);

        Assert.Equal(InboundMailMessageStatus.Archived, message.Status);
        Assert.NotNull(message.ModifiedOnUtc);
    }

    [Fact]
    public void Receive_WhenRecipientsAreEmpty_Throws() {
        Assert.Throws<ArgumentException>(() => InboundMailMessage.Receive(
            null,
            null,
            [],
            null,
            null,
            null,
            "raw",
            DateTimeOffset.UtcNow));
    }
}
