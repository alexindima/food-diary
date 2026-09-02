using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class DomainEventInvariantTests {
    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void RecommendationCreatedDomainEvent_WithNonUtcOverride_Throws(DateTimeKind kind) {
        var occurredOnUtc = new DateTime(2026, 3, 27, 12, 0, 0, kind);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RecommendationCreatedDomainEvent(
                RecommendationId.New(),
                UserId.New(),
                UserId.New(),
                occurredOnUtc));

        Assert.Equal("occurredOnUtcOverride", ex.ParamName);
    }

    [Fact]
    public void RecommendationCreatedDomainEvent_WithOverride_UsesOverrideTimestamp() {
        var occurredOnUtc = new DateTime(2026, 3, 27, 12, 0, 0, DateTimeKind.Utc);
        var recommendationId = RecommendationId.New();
        var dietologistUserId = UserId.New();
        var clientUserId = UserId.New();

        var domainEvent = new RecommendationCreatedDomainEvent(
            recommendationId,
            dietologistUserId,
            clientUserId,
            occurredOnUtc);

        Assert.Multiple(
            () => Assert.Equal(recommendationId, domainEvent.RecommendationId),
            () => Assert.Equal(dietologistUserId, domainEvent.DietologistUserId),
            () => Assert.Equal(clientUserId, domainEvent.ClientUserId),
            () => Assert.Equal(occurredOnUtc, domainEvent.OccurredOnUtc));
    }

    [Fact]
    public void DietologistInvitationAcceptedDomainEvent_WithOverride_ExposesPayload() {
        var occurredOnUtc = new DateTime(2026, 3, 27, 12, 0, 0, DateTimeKind.Utc);
        var invitationId = DietologistInvitationId.New();
        var clientUserId = UserId.New();
        var dietologistUserId = UserId.New();

        var domainEvent = new DietologistInvitationAcceptedDomainEvent(
            invitationId,
            clientUserId,
            dietologistUserId,
            occurredOnUtc);

        Assert.Multiple(
            () => Assert.Equal(invitationId, domainEvent.InvitationId),
            () => Assert.Equal(clientUserId, domainEvent.ClientUserId),
            () => Assert.Equal(dietologistUserId, domainEvent.DietologistUserId),
            () => Assert.Equal(occurredOnUtc, domainEvent.OccurredOnUtc));
    }

    [Fact]
    public void DietologistInvitationDeclinedDomainEvent_WithOverride_ExposesPayload() {
        var occurredOnUtc = new DateTime(2026, 3, 27, 12, 0, 0, DateTimeKind.Utc);
        var invitationId = DietologistInvitationId.New();
        var clientUserId = UserId.New();

        var domainEvent = new DietologistInvitationDeclinedDomainEvent(
            invitationId,
            clientUserId,
            "dietologist@example.com",
            occurredOnUtc);

        Assert.Multiple(
            () => Assert.Equal(invitationId, domainEvent.InvitationId),
            () => Assert.Equal(clientUserId, domainEvent.ClientUserId),
            () => Assert.Equal("dietologist@example.com", domainEvent.DietologistEmail),
            () => Assert.Equal(occurredOnUtc, domainEvent.OccurredOnUtc));
    }

}
