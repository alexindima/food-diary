using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Meals.Domain.ValueObjects;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class CopiedMealNutritionTests {
    [Fact]
    public void CopiedNutritionSnapshot_PreservesManualValuesAndClearsThemWhenReturningToAutomaticTotals() {
        var meal = Meal.Create(UserId.New(), new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var original = MealNutritionState.CreateInitial();
        MealNutritionState state = original with {
            TotalCalories = 123.456,
            TotalProteins = 12.344,
            TotalFats = 3.456,
            TotalCarbs = 17.654,
            TotalFiber = 2.349,
            TotalAlcohol = 0.123,
            IsNutritionAutoCalculated = false,
            ManualCalories = 123.456,
            ManualProteins = 12.344,
            ManualFats = 3.456,
            ManualCarbs = 17.654,
            ManualFiber = 2.349,
            ManualAlcohol = 0.123,
        };
        var template = new MealNutritionUpdate(0, 0, 0, 0, 0, 0, IsAutoCalculated: true);
        MealNutritionUpdate update = template with {
            TotalCalories = state.TotalCalories,
            TotalProteins = state.TotalProteins,
            TotalFats = state.TotalFats,
            TotalCarbs = state.TotalCarbs,
            TotalFiber = state.TotalFiber,
            TotalAlcohol = state.TotalAlcohol,
            IsAutoCalculated = state.IsNutritionAutoCalculated,
            ManualCalories = state.ManualCalories,
            ManualProteins = state.ManualProteins,
            ManualFats = state.ManualFats,
            ManualCarbs = state.ManualCarbs,
            ManualFiber = state.ManualFiber,
            ManualAlcohol = state.ManualAlcohol,
        };
        int initialEvents = meal.DomainEvents.Count;

        meal.ApplyNutrition(update);

        Assert.Multiple(
            () => Assert.Equal(123.46, meal.TotalCalories),
            () => Assert.Equal(12.34, meal.TotalProteins),
            () => Assert.Equal(3.46, meal.TotalFats),
            () => Assert.Equal(17.65, meal.TotalCarbs),
            () => Assert.Equal(2.35, meal.TotalFiber),
            () => Assert.Equal(0.12, meal.TotalAlcohol),
            () => Assert.Equal(123.46, meal.ManualCalories),
            () => Assert.Equal(12.34, meal.ManualProteins),
            () => Assert.Equal(3.46, meal.ManualFats),
            () => Assert.Equal(17.65, meal.ManualCarbs),
            () => Assert.Equal(2.35, meal.ManualFiber),
            () => Assert.Equal(0.12, meal.ManualAlcohol),
            () => Assert.False(meal.IsNutritionAutoCalculated),
            () => Assert.Equal(initialEvents + 1, meal.DomainEvents.Count),
            () => Assert.Equal(MealNutritionState.CreateInitial(), original),
            () => Assert.True(template.IsAutoCalculated),
            () => Assert.Equal(0, template.TotalCalories));
        meal.ApplyNutrition(update);
        Assert.Equal(initialEvents + 1, meal.DomainEvents.Count);

        meal.ApplyNutrition(update with { IsAutoCalculated = true });

        Assert.Multiple(
            () => Assert.True(meal.IsNutritionAutoCalculated),
            () => Assert.Equal(123.46, meal.TotalCalories),
            () => Assert.Null(meal.ManualCalories),
            () => Assert.Null(meal.ManualProteins),
            () => Assert.Null(meal.ManualFats),
            () => Assert.Null(meal.ManualCarbs),
            () => Assert.Null(meal.ManualFiber),
            () => Assert.Null(meal.ManualAlcohol),
            () => Assert.Equal(initialEvents + 2, meal.DomainEvents.Count));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000001)]
    public void ProductSnapshot_RejectsInvalidBasesWithoutOverwritingTheStoredSnapshot(double basis) {
        var meal = Meal.Create(UserId.New(), new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        MealItem item = meal.AddProduct(ProductId.New(), ProductUnitQuantity.FromUnits(25));
        item.ApplyProductSnapshot("Saved", imageUrl: null, MeasurementUnit.G, 100, 200, 10, 5, 20, 3, 0);

        Assert.Throws<ArgumentOutOfRangeException>("baseAmount", () =>
            item.ApplyProductSnapshot("Rejected", imageUrl: null, MeasurementUnit.G, basis, 999, 1, 1, 1, 1, 0));

        Assert.Multiple(
            () => Assert.Equal("Saved", item.SnapshotName),
            () => Assert.Equal(100, item.SnapshotBaseAmount),
            () => Assert.Equal(200, item.SnapshotCaloriesPerBase),
            () => Assert.Equal(25, item.Amount));
    }
}
