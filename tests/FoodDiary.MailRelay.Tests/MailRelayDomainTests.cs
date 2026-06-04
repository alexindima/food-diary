using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Common;
using FoodDiary.MailRelay.Domain.Emails;
using System.Runtime.CompilerServices;

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
        var shouldSuppress = MailRelaySuppressionPolicy.ShouldSuppress(eventType, classification);

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
        var result = MailRelayDeliveryEventType.TryNormalize(value, out var normalized);

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
        var result = MailRelayBounceClassification.IsSupportedOptional(value);

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
            null,
            "correlation",
            attemptCount,
            maxAttempts));

        var decision = email.MarkFailedAttempt("SMTP failure");

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

        var request = email.ToSubmissionRequest();

        Assert.Equal(message.FromAddress, request.FromAddress);
        Assert.Equal(message.FromName, request.FromName);
        Assert.Equal(message.To, request.To);
        Assert.Equal(message.Subject, request.Subject);
        Assert.Equal(message.HtmlBody, request.HtmlBody);
        Assert.Equal(message.TextBody, request.TextBody);
        Assert.Equal(message.CorrelationId, request.CorrelationId);
    }

    [Fact]
    public void QueuedEmailId_ToString_ReturnsWrappedGuid() {
        var value = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var id = new QueuedEmailId(value);

        Assert.Equal(value.ToString(), id.ToString());
    }

    [Fact]
    public void Entity_Equals_ReturnsExpectedResultsForCommonCases() {
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);

        Assert.True(entity.Equals((object)entity));
        Assert.False(entity.Equals(null));
        Assert.False(entity.Equals(new DifferentTestEntity(id)));
        Assert.False(new TestEntity().Equals(new TestEntity()));
        Assert.True(entity == new TestEntity(id));
        Assert.False(entity != new TestEntity(id));
    }

    [Fact]
    public void Entity_GetHashCode_CachesPersistedIdentityHashAndUsesRuntimeHashForTransientEntities() {
        var persisted = new TestEntity(Guid.NewGuid());
        var first = persisted.GetHashCode();

        var second = persisted.GetHashCode();
        var transient = new TestEntity();

        Assert.Equal(first, second);
        Assert.Equal(RuntimeHelpers.GetHashCode(transient), transient.GetHashCode());
    }

    [Fact]
    public void AggregateRoot_TracksAndClearsDomainEvents() {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var domainEvent = new TestDomainEvent();

        aggregate.Record(domainEvent);
        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestEntity : Entity<Guid> {
        public TestEntity() {
        }

        public TestEntity(Guid id) : base(id) {
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class DifferentTestEntity(Guid id) : Entity<Guid>(id);

    [ExcludeFromCodeCoverage]
    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id) {
        public void Record(IDomainEvent domainEvent) {
            RaiseDomainEvent(domainEvent);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record TestDomainEvent : IDomainEvent {
        public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
    }
}
