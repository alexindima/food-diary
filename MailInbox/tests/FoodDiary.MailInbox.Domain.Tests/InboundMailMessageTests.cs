using FoodDiary.MailInbox.Domain.Events;
using FoodDiary.MailInbox.Domain.Messages;
using System.Globalization;

namespace FoodDiary.MailInbox.Domain.Tests;

[ExcludeFromCodeCoverage]
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
            messageId: null,
            fromAddress: null,
            ["admin@fooddiary.club"],
            subject: null,
            textBody: null,
            htmlBody: null,
            "raw",
            DateTimeOffset.UtcNow);

        message.Archive(DateTimeOffset.UtcNow);

        Assert.Equal(InboundMailMessageStatus.Archived, message.Status);
        Assert.NotNull(message.ModifiedOnUtc);
    }

    [Fact]
    public void Archive_WhenMessageIsAlreadyArchived_DoesNotChangeModifiedTimestamp() {
        InboundMailMessage message = CreateMessage();
        var archivedAtUtc = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);

        message.Archive(archivedAtUtc);
        DateTime? modifiedOnUtc = message.ModifiedOnUtc;
        message.Archive(archivedAtUtc.AddHours(1));

        Assert.Equal(InboundMailMessageStatus.Archived, message.Status);
        Assert.Equal(modifiedOnUtc, message.ModifiedOnUtc);
    }

    [Fact]
    public void MarkAsRead_WhenMessageIsUnread_SetsReadTimestampOnce() {
        InboundMailMessage message = CreateMessage();
        var readAt = new DateTimeOffset(2026, 6, 14, 14, 0, 0, TimeSpan.FromHours(4));

        message.MarkAsRead(readAt);
        DateTime? modifiedOnUtc = message.ModifiedOnUtc;
        message.MarkAsRead(readAt.AddHours(1));

        Assert.Equal(readAt.ToUniversalTime(), message.ReadAtUtc);
        Assert.Equal(modifiedOnUtc, message.ModifiedOnUtc);
    }

    [Fact]
    public void Receive_WhenRecipientsAreEmpty_Throws() {
        Assert.Throws<ArgumentException>(() => InboundMailMessage.Receive(
            messageId: null,
            fromAddress: null,
            [],
            subject: null,
            textBody: null,
            htmlBody: null,
            "raw",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Receive_WhenRecipientIsWhiteSpace_Throws() {
        Assert.Throws<ArgumentException>(() => InboundMailMessage.Receive(
            messageId: null,
            fromAddress: null,
            [" "],
            subject: null,
            textBody: null,
            htmlBody: null,
            "raw",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Receive_WhenRawMimeIsWhiteSpace_Throws() {
        Assert.Throws<ArgumentException>(() => InboundMailMessage.Receive(
            messageId: null,
            fromAddress: null,
            ["admin@fooddiary.club"],
            subject: null,
            textBody: null,
            htmlBody: null,
            " ",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Receive_NormalizesReceivedAtToUtcAndNullsWhiteSpaceFields() {
        var receivedAt = new DateTimeOffset(2026, 4, 26, 13, 0, 0, TimeSpan.FromHours(3));

        var message = InboundMailMessage.Receive(
            " ",
            " ",
            ["admin@fooddiary.club"],
            " ",
            textBody: null,
            htmlBody: null,
            "raw",
            receivedAt);

        Assert.Null(message.MessageId);
        Assert.Null(message.FromAddress);
        Assert.Null(message.Subject);
        Assert.Equal(
            DateTimeOffset.Parse("2026-04-26T10:00:00+00:00", CultureInfo.InvariantCulture),
            message.ReceivedAtUtc);
        Assert.Equal(DateTimeKind.Utc, message.CreatedOnUtc.Kind);
    }

    [Fact]
    public void ClearDomainEvents_RemovesRaisedEvents() {
        InboundMailMessage message = CreateMessage();

        message.ClearDomainEvents();

        Assert.Empty(message.DomainEvents);
    }

    [Fact]
    public void InboundMailMessageId_ConvertsToAndFromGuid() {
        var value = Guid.NewGuid();

        var id = (InboundMailMessageId)value;
        Guid converted = id;

        Assert.Equal(value, id.Value);
        Assert.Equal(value, converted);
        Assert.Equal(Guid.Empty, InboundMailMessageId.Empty.Value);
        Assert.NotEqual(Guid.Empty, InboundMailMessageId.New().Value);
    }

    [Fact]
    public void InboundMailMessageStatus_FromKnownValues_ReturnsStatus() {
        Assert.Equal(InboundMailMessageStatus.Received, InboundMailMessageStatus.From("received"));
        Assert.Equal(InboundMailMessageStatus.Archived, InboundMailMessageStatus.From("archived"));
        Assert.Equal("received", InboundMailMessageStatus.Received.ToString());
    }

    [Fact]
    public void InboundMailMessageStatus_FromUnknownValue_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => InboundMailMessageStatus.From("unknown"));
    }

    [Fact]
    public void InboundMailMessageReceivedDomainEvent_NormalizesOccurredOnToUtc() {
        var id = InboundMailMessageId.New();
        var occurredAt = new DateTime(2026, 4, 26, 13, 0, 0, DateTimeKind.Local);

        var domainEvent = new InboundMailMessageReceivedDomainEvent(id, occurredAt);

        Assert.Equal(id, domainEvent.MessageId);
        Assert.Equal(DateTimeKind.Utc, domainEvent.OccurredOnUtc.Kind);
    }

    private static InboundMailMessage CreateMessage() =>
        InboundMailMessage.Receive(
            messageId: null,
            fromAddress: null,
            ["admin@fooddiary.club"],
            subject: null,
            textBody: null,
            htmlBody: null,
            "raw",
            DateTimeOffset.UtcNow);
}
