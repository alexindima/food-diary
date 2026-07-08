using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;

namespace FoodDiary.MailRelay.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayDomainRecordTests {
    [Fact]
    public void DeliveryEventEntry_StoresAllFields() {
        var id = Guid.NewGuid();
        var occurredAtUtc = new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset createdAtUtc = occurredAtUtc.AddMinutes(1);

        var entry = new MailRelayDeliveryEventEntry(
            id,
            "bounce",
            "user@example.com",
            "ses",
            "hard",
            "provider-message",
            "mailbox unavailable",
            occurredAtUtc,
            createdAtUtc);

        Assert.Multiple(
            () => Assert.Equal(id, entry.Id),
            () => Assert.Equal("bounce", entry.EventType),
            () => Assert.Equal("user@example.com", entry.Email),
            () => Assert.Equal("ses", entry.Source),
            () => Assert.Equal("hard", entry.Classification),
            () => Assert.Equal("provider-message", entry.ProviderMessageId),
            () => Assert.Equal("mailbox unavailable", entry.Reason),
            () => Assert.Equal(occurredAtUtc, entry.OccurredAtUtc),
            () => Assert.Equal(createdAtUtc, entry.CreatedAtUtc));
    }

    [Fact]
    public void IngestMailEventRequest_StoresAllFields() {
        var occurredAtUtc = new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);

        var request = new IngestMailEventRequest(
            "complaint",
            "user@example.com",
            "ses",
            "abuse",
            "provider-message",
            "reported",
            occurredAtUtc);

        Assert.Multiple(
            () => Assert.Equal("complaint", request.EventType),
            () => Assert.Equal("user@example.com", request.Email),
            () => Assert.Equal("ses", request.Source),
            () => Assert.Equal("abuse", request.Classification),
            () => Assert.Equal("provider-message", request.ProviderMessageId),
            () => Assert.Equal("reported", request.Reason),
            () => Assert.Equal(occurredAtUtc, request.OccurredAtUtc));
    }

    [Fact]
    public void SuppressionRecords_StoreAllFields() {
        var createdAtUtc = new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset updatedAtUtc = createdAtUtc.AddMinutes(1);
        DateTimeOffset expiresAtUtc = createdAtUtc.AddDays(7);

        var request = new CreateSuppressionRequest(
            "user@example.com",
            "hard-bounce",
            "ses",
            expiresAtUtc);
        var entry = new MailRelaySuppressionEntry(
            "user@example.com",
            "hard-bounce",
            "ses",
            createdAtUtc,
            updatedAtUtc,
            expiresAtUtc);

        Assert.Multiple(
            () => Assert.Equal("user@example.com", request.Email),
            () => Assert.Equal("hard-bounce", request.Reason),
            () => Assert.Equal("ses", request.Source),
            () => Assert.Equal(expiresAtUtc, request.ExpiresAtUtc),
            () => Assert.Equal("user@example.com", entry.Email),
            () => Assert.Equal("hard-bounce", entry.Reason),
            () => Assert.Equal("ses", entry.Source),
            () => Assert.Equal(createdAtUtc, entry.CreatedAtUtc),
            () => Assert.Equal(updatedAtUtc, entry.UpdatedAtUtc),
            () => Assert.Equal(expiresAtUtc, entry.ExpiresAtUtc));
    }

    [Theory]
    [InlineData("complaint", "complaint")]
    [InlineData("bounce", "hard-bounce")]
    [InlineData("opened", "hard-bounce")]
    public void SuppressionPolicy_GetDefaultReason_ReturnsExpectedReason(
        string eventType,
        string expectedReason) {
        string reason = MailRelaySuppressionPolicy.GetDefaultReason(eventType);

        Assert.Equal(expectedReason, reason);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("complaint", true)]
    [InlineData("opened", false)]
    public void DeliveryEventType_IsSupported_ReturnsExpectedResult(
        string? eventType,
        bool expectedResult) {
        bool result = MailRelayDeliveryEventType.IsSupported(eventType);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void QueuedEmail_MarkFailedAttempt_WithBlankError_Throws() {
        var email = QueuedEmail.FromPersistence(new QueuedEmailMessage(
            Guid.NewGuid(),
            "relay@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Subject",
            "<p>Body</p>",
            TextBody: null,
            CorrelationId: null,
            AttemptCount: 1,
            MaxAttempts: 3));

        Assert.Throws<ArgumentException>(() => email.MarkFailedAttempt(" "));
    }

    [Fact]
    public void QueuedEmail_MarkSuppressed_UpdatesStatusAndModifiedTimestamp() {
        var email = QueuedEmail.FromPersistence(new QueuedEmailMessage(
            Guid.NewGuid(),
            "relay@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Subject",
            "<p>Body</p>",
            TextBody: null,
            CorrelationId: null,
            AttemptCount: 1,
            MaxAttempts: 3));

        email.MarkSuppressed();

        Assert.Equal(QueuedEmailStatus.Suppressed, email.Status);
        Assert.NotNull(email.ModifiedOnUtc);
    }

    [Fact]
    public void QueuedEmailId_ImplicitConversion_ReturnsValue() {
        var value = Guid.NewGuid();

        Guid converted = new QueuedEmailId(value);

        Assert.Equal(value, converted);
    }

    [Fact]
    public void QueuedEmailFailureDecision_StoresId() {
        var id = QueuedEmailId.New();

        var decision = new QueuedEmailFailureDecision(
            id,
            AttemptCount: 2,
            QueuedEmailStatus.Retry,
            IsTerminalFailure: false,
            Error: "SMTP failure");

        Assert.Equal(id, decision.Id);
    }
}
