using System.Reflection;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class MealPlanningExtractedInvariantTests {
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0)]
    [InlineData(-1)]
    public void MealPlan_Create_WithInvalidTargetCalories_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealPlan.CreateCurated(
                "Plan",
                description: null,
                DietType.Balanced,
                durationDays: 7,
                targetCaloriesPerDay: value));
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
            aisle: null,
            note: null,
            isChecked: true,
            checkedOnUtc: null,
            sortOrder: 2,
            occurredOnUtc);

        Assert.Multiple(
            () => Assert.Equal(shoppingListId, domainEvent.ShoppingListId),
            () => Assert.Equal(itemId, domainEvent.ShoppingListItemId),
            () => Assert.Equal(productId, domainEvent.ProductId),
            () => Assert.Equal("Milk", domainEvent.Name),
            () => Assert.Equal(1.5, domainEvent.Amount),
            () => Assert.Equal(MeasurementUnit.Ml, domainEvent.Unit),
            () => Assert.Equal("Dairy", domainEvent.Category),
            () => Assert.True(domainEvent.IsChecked),
            () => Assert.Equal(2, domainEvent.SortOrder),
            () => Assert.Equal(occurredOnUtc, domainEvent.OccurredOnUtc));
    }

    [Fact]
    public void ShoppingItems_CoverSourceAndUpdatePaths() {
        var item = ShoppingListItem.Create(
            ShoppingListId.New(),
            " Apple ",
            ProductId.New(),
            1,
            MeasurementUnit.Pcs,
            " Fruit ",
            isChecked: true,
            sortOrder: 1,
            aisle: " A1 ",
            note: " Ripe ",
            checkedOnUtc: DateTime.UtcNow);

        item.UpdateDetails(
            " Pear ",
            ProductId.New(),
            2,
            MeasurementUnit.Pcs,
            " Fruit ",
            " A2 ",
            " Green ",
            isChecked: false,
            checkedOnUtc: null,
            sortOrder: 2);
        ShoppingListItemSource source = item.AddMealPlanSource(
            MealPlanId.New(),
            MealPlanMealId.New(),
            RecipeId.New(),
            " Dinner ",
            dayNumber: 1,
            " Lunch ",
            amount: 2,
            MeasurementUnit.Pcs);

        ReadPublicProperties(item);
        ReadPublicProperties(source);

        Assert.Multiple(
            () => Assert.Equal("Pear", item.Name),
            () => Assert.Null(item.CheckedOnUtc),
            () => Assert.Single(item.Sources),
            () => Assert.Equal("Dinner", source.Label),
            () => Assert.Equal("Lunch", source.MealType));
    }
    private static void ReadPublicProperties(object instance) {
        foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (property.GetIndexParameters().Length == 0) {
                property.GetValue(instance);
            }
        }
    }
}
