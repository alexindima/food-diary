using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Domain.Events;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class TypedMealPlanningBoundaryTests {
    [Fact]
    public void UserPlanFactory_PreservesOwnershipAndTypedDurationAndRejectsAMissingDuration() {
        var user = UserId.New();
        var duration = PlanDurationDays.FromDays(2);

        var plan = MealPlan.CreateForUserWithDuration(user, "  Weekly  ", "  Description  ", DietType.Balanced, duration, 2100);

        Assert.Multiple(
            () => Assert.Equal(user, plan.UserId),
            () => Assert.Equal("Weekly", plan.Name),
            () => Assert.Equal("Description", plan.Description),
            () => Assert.Equal(2, plan.DurationDays),
            () => Assert.Equal(2100, plan.TargetCaloriesPerDay),
            () => Assert.False(plan.IsCurated));
        Assert.Throws<ArgumentNullException>("duration", () =>
            MealPlan.CreateForUserWithDuration(user, "Invalid", description: null, DietType.Balanced, null!, targetCaloriesPerDay: null));
    }

    [Fact]
    public void TypedItemFactory_PreservesQuantityMetadataAndSuppliedIdentity() {
        var list = ShoppingListId.New();
        var product = ProductId.New();
        var itemId = ShoppingListItemId.New();
        var checkedAt = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        var item = ShoppingListItem.CreateWithQuantity(list, "  Milk  ", product,
            ShoppingQuantity.FromFields(250.5, MeasurementUnit.Ml), "  Dairy  ", isChecked: true, 3,
            aisle: "  Cold  ", note: "  Keep chilled  ", checkedOnUtc: checkedAt, id: itemId);

        Assert.Multiple(
            () => Assert.Equal(itemId, item.Id),
            () => Assert.Equal(list, item.ShoppingListId),
            () => Assert.Equal(product, item.ProductId),
            () => Assert.Equal("Milk", item.Name),
            () => Assert.Equal(250.5, item.Amount),
            () => Assert.Equal(MeasurementUnit.Ml, item.Unit),
            () => Assert.Equal("Dairy", item.Category),
            () => Assert.Equal("Cold", item.Aisle),
            () => Assert.Equal("Keep chilled", item.Note),
            () => Assert.Equal(checkedAt, item.CheckedOnUtc),
            () => Assert.Equal(3, item.SortOrder),
            () => Assert.True(item.IsChecked));
        Assert.Throws<ArgumentNullException>("quantity", () =>
            ShoppingListItem.CreateWithQuantity(list, "Invalid", productId: null, null!, category: null, isChecked: false, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void PlannedServings_RejectsNonPositiveCounts(int value) =>
        Assert.Throws<ArgumentOutOfRangeException>("value", () => PlannedServings.FromCount(value));

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ShoppingQuantity_RejectsNonFiniteAmounts(double amount) =>
        Assert.Throws<ArgumentOutOfRangeException>("amount", () => ShoppingQuantity.FromFields(amount, MeasurementUnit.G));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SourceQuantity_RejectsNonPositiveObservations(double amount) =>
        Assert.Throws<ArgumentOutOfRangeException>("amount", () => ShoppingSourceQuantity.FromFields(amount, MeasurementUnit.G));

    [Fact]
    public void ShoppingEvents_PreserveOptionalMetadataAndUtcAuditMeaning() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");
        var checkedAt = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        ShoppingListItem item = list.AddItem("Milk", productId: null, 250, MeasurementUnit.Ml, "Dairy",
            isChecked: true, 2, "Cold", "Keep chilled", checkedAt);
        ShoppingListItemAddedDomainEvent added = Assert.Single(list.DomainEvents.OfType<ShoppingListItemAddedDomainEvent>());
        Assert.Multiple(
            () => Assert.Equal(item.Id, added.ShoppingListItemId),
            () => Assert.Equal("Cold", added.Aisle),
            () => Assert.Equal("Keep chilled", added.Note),
            () => Assert.Equal(checkedAt, added.CheckedOnUtc));

        list.ClearItems();

        ShoppingListItemsClearedDomainEvent cleared = Assert.Single(list.DomainEvents.OfType<ShoppingListItemsClearedDomainEvent>());
        Assert.Multiple(
            () => Assert.Equal(list.Id, cleared.ShoppingListId),
            () => Assert.Equal(1, cleared.ClearedItemsCount),
            () => Assert.Equal(DateTimeKind.Utc, cleared.OccurredOnUtc.Kind),
            () => Assert.NotEqual(default, cleared.OccurredOnUtc));
    }
}
