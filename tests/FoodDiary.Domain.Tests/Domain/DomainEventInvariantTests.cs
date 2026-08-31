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
    public void MealNutritionAppliedDomainEvent_WithOverride_ExposesNutritionValues() {
        var occurredOnUtc = new DateTime(2026, 3, 27, 12, 0, 0, DateTimeKind.Utc);
        var mealId = MealId.New();

        var domainEvent = new MealNutritionAppliedDomainEvent(
            mealId,
            isAutoCalculated: true,
            totalCalories: 500,
            totalProteins: 30,
            totalFats: 20,
            totalCarbs: 50,
            totalFiber: 5,
            totalAlcohol: 0,
            occurredOnUtc);

        Assert.Multiple(
            () => Assert.Equal(mealId, domainEvent.MealId),
            () => Assert.True(domainEvent.IsAutoCalculated),
            () => Assert.Equal(500, domainEvent.TotalCalories),
            () => Assert.Equal(30, domainEvent.TotalProteins),
            () => Assert.Equal(20, domainEvent.TotalFats),
            () => Assert.Equal(50, domainEvent.TotalCarbs),
            () => Assert.Equal(5, domainEvent.TotalFiber),
            () => Assert.Equal(0, domainEvent.TotalAlcohol),
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
