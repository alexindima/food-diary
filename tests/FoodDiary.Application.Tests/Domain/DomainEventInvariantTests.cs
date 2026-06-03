using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class DomainEventInvariantTests {
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

        Assert.Equal(recommendationId, domainEvent.RecommendationId);
        Assert.Equal(dietologistUserId, domainEvent.DietologistUserId);
        Assert.Equal(clientUserId, domainEvent.ClientUserId);
        Assert.Equal(occurredOnUtc, domainEvent.OccurredOnUtc);
    }

    [Fact]
    public void MealNutritionAppliedDomainEvent_WithOverride_ExposesNutritionValues() {
        var occurredOnUtc = new DateTime(2026, 3, 27, 12, 0, 0, DateTimeKind.Utc);
        var mealId = MealId.New();

        var domainEvent = new MealNutritionAppliedDomainEvent(
            mealId,
            IsAutoCalculated: true,
            TotalCalories: 500,
            TotalProteins: 30,
            TotalFats: 20,
            TotalCarbs: 50,
            TotalFiber: 5,
            TotalAlcohol: 0,
            occurredOnUtc);

        Assert.Equal(mealId, domainEvent.MealId);
        Assert.True(domainEvent.IsAutoCalculated);
        Assert.Equal(500, domainEvent.TotalCalories);
        Assert.Equal(30, domainEvent.TotalProteins);
        Assert.Equal(20, domainEvent.TotalFats);
        Assert.Equal(50, domainEvent.TotalCarbs);
        Assert.Equal(5, domainEvent.TotalFiber);
        Assert.Equal(0, domainEvent.TotalAlcohol);
        Assert.Equal(occurredOnUtc, domainEvent.OccurredOnUtc);
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

        Assert.Equal(invitationId, domainEvent.InvitationId);
        Assert.Equal(clientUserId, domainEvent.ClientUserId);
        Assert.Equal(dietologistUserId, domainEvent.DietologistUserId);
        Assert.Equal(occurredOnUtc, domainEvent.OccurredOnUtc);
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

        Assert.Equal(invitationId, domainEvent.InvitationId);
        Assert.Equal(clientUserId, domainEvent.ClientUserId);
        Assert.Equal("dietologist@example.com", domainEvent.DietologistEmail);
        Assert.Equal(occurredOnUtc, domainEvent.OccurredOnUtc);
    }

    [Fact]
    public void ShoppingListItemAddedDomainEvent_WithOverride_ExposesPayload() {
        var occurredOnUtc = new DateTime(2026, 3, 27, 12, 0, 0, DateTimeKind.Utc);
        var shoppingListId = ShoppingListId.New();
        var itemId = ShoppingListItemId.New();
        var productId = ProductId.New();

        var domainEvent = new ShoppingListItemAddedDomainEvent(
            shoppingListId,
            itemId,
            productId,
            "Milk",
            1.5,
            MeasurementUnit.Ml,
            "Dairy",
            IsChecked: true,
            SortOrder: 2,
            occurredOnUtc);

        Assert.Equal(shoppingListId, domainEvent.ShoppingListId);
        Assert.Equal(itemId, domainEvent.ShoppingListItemId);
        Assert.Equal(productId, domainEvent.ProductId);
        Assert.Equal("Milk", domainEvent.Name);
        Assert.Equal(1.5, domainEvent.Amount);
        Assert.Equal(MeasurementUnit.Ml, domainEvent.Unit);
        Assert.Equal("Dairy", domainEvent.Category);
        Assert.True(domainEvent.IsChecked);
        Assert.Equal(2, domainEvent.SortOrder);
        Assert.Equal(occurredOnUtc, domainEvent.OccurredOnUtc);
    }
}
