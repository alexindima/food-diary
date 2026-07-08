using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;

namespace FoodDiary.MailRelay.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayDomainTests {
    [Theory]
    [InlineData("complaint", null, true)]
    [InlineData("Complaint", null, true)]
    [InlineData("bounce", "hard", true)]
    [InlineData("bounce", "soft", false)]
    [InlineData("bounce", null, false)]
    [InlineData("opened", null, false)]
    public void SuppressionPolicy_ReturnsExpectedDecision(
        string eventType,
        string? classification,
        bool expectedShouldSuppress) {
        bool shouldSuppress = MailRelaySuppressionPolicy.ShouldSuppress(eventType, classification);

        Assert.Equal(expectedShouldSuppress, shouldSuppress);
    }

    [Theory]
    [InlineData("bounce", true, "bounce")]
    [InlineData(" Bounce ", true, "bounce")]
    [InlineData("complaint", true, "complaint")]
    [InlineData("opened", false, "")]
    [InlineData("", false, "")]
    public void DeliveryEventType_TryNormalize_NormalizesSupportedTypes(
        string value,
        bool expectedResult,
        string expectedNormalized) {
        bool result = MailRelayDeliveryEventType.TryNormalize(value, out string? normalized);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedNormalized, normalized);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("hard", true)]
    [InlineData("soft", true)]
    [InlineData("permanent", false)]
    public void BounceClassification_IsSupportedOptional_ReturnsExpectedResult(
        string? value,
        bool expectedResult) {
        bool result = MailRelayBounceClassification.IsSupportedOptional(value);

        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(1, 3, QueuedEmailStatus.Retry, false)]
    [InlineData(3, 3, QueuedEmailStatus.Failed, true)]
    public void QueuedEmail_MarkFailedAttempt_DecidesRetryOrTerminalFailure(
        int attemptCount,
        int maxAttempts,
        string expectedStatus,
        bool expectedTerminalFailure) {
        var email = QueuedEmail.FromPersistence(new QueuedEmailMessage(
            Guid.NewGuid(),
            "relay@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Subject",
            "<p>Body</p>",
            TextBody: null,
            "correlation",
            attemptCount,
            maxAttempts));

        QueuedEmailFailureDecision decision = email.MarkFailedAttempt("SMTP failure");

        Assert.Equal(expectedStatus, email.Status);
        Assert.Equal(expectedStatus, decision.Status);
        Assert.Equal(expectedTerminalFailure, decision.IsTerminalFailure);
        Assert.Equal(attemptCount, decision.AttemptCount);
    }

    [Fact]
    public void QueuedEmail_ToSubmissionRequest_PreservesMessageFields() {
        var message = new QueuedEmailMessage(
            Guid.NewGuid(),
            "relay@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Subject",
            "<p>Body</p>",
            "Body",
            "correlation",
            1,
            3);
        var email = QueuedEmail.FromPersistence(message);

        RelayEmailMessageRequest request = email.ToSubmissionRequest();

        Assert.Equal(message.FromAddress, request.FromAddress);
        Assert.Equal(message.FromName, request.FromName);
        Assert.Equal(message.To, request.To);
        Assert.Equal(message.Subject, request.Subject);
        Assert.Equal(message.HtmlBody, request.HtmlBody);
        Assert.Equal(message.TextBody, request.TextBody);
        Assert.Equal(message.CorrelationId, request.CorrelationId);
    }

    [Fact]
    public void QueuedEmail_FromPersistence_SetsAuditTimestamp() {
        var createdAtUtc = new DateTimeOffset(2026, 6, 14, 10, 15, 0, TimeSpan.Zero);

        var email = QueuedEmail.FromPersistence(new QueuedEmailMessage(
            Guid.NewGuid(),
            "relay@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Subject",
            "<p>Body</p>",
            TextBody: null,
            "correlation",
            1,
            3,
            createdAtUtc));

        Assert.Equal(createdAtUtc.UtcDateTime, email.CreatedOnUtc);
        Assert.Equal(DateTimeKind.Utc, email.CreatedOnUtc.Kind);
    }

    [Fact]
    public void QueuedEmail_MarkSent_UpdatesModifiedAuditTimestamp() {
        var email = QueuedEmail.FromPersistence(new QueuedEmailMessage(
            Guid.NewGuid(),
            "relay@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Subject",
            "<p>Body</p>",
            TextBody: null,
            "correlation",
            1,
            3));

        email.MarkSent();

        Assert.Equal(QueuedEmailStatus.Sent, email.Status);
        Assert.NotNull(email.ModifiedOnUtc);
        Assert.Equal(DateTimeKind.Utc, email.ModifiedOnUtc?.Kind);
    }

    [Fact]
    public void QueuedEmailId_ToString_ReturnsWrappedGuid() {
        var value = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var id = new QueuedEmailId(value);

        Assert.Equal(value.ToString(), id.ToString());
    }

    [Fact]
    public void QueuedEmailId_Factories_ReturnExpectedValues() {
        var generated = QueuedEmailId.New();

        Assert.NotEqual(Guid.Empty, generated.Value);
        Assert.Equal(Guid.Empty, QueuedEmailId.Empty.Value);
    }

}
