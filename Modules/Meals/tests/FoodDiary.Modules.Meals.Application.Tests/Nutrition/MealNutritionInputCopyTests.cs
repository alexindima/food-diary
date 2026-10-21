using FoodDiary.Modules.Meals.Application.Services;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Meals.Application.Tests.Nutrition;

[ExcludeFromCodeCoverage]
public sealed class MealNutritionInputCopyTests {
    [Fact]
    public async Task CopiedManualTotals_ApplyWithRoundingAndCanSwitchBackToCalculatedTotalsWithoutMutatingTheInput() {
        var user = UserId.New();
        var meal = Meal.Create(user, new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc));
        var original = new MealNutritionInput(IsNutritionAutoCalculated: true,
            ManualCalories: null, ManualProteins: null, ManualFats: null, ManualCarbs: null, ManualFiber: null, ManualAlcohol: null);
        MealNutritionInput manual = original with {
            IsNutritionAutoCalculated = false,
            ManualCalories = 123.456,
            ManualProteins = 3.456,
            ManualFats = 4.567,
            ManualCarbs = 5.678,
            ManualFiber = 1.234,
            ManualAlcohol = 0.789,
        };
        using var cancellation = new CancellationTokenSource();
        IMealNutritionService nutrition = Substitute.For<IMealNutritionService>();
        nutrition.CalculateAsync(meal, user, cancellation.Token).Returns(Result.Success(new MealNutritionSummary(210, 11, 12, 13, 14, 0)));

        ResultAssert.Success(await MealNutritionApplier.ApplyAsync(meal, user, nutrition, manual, cancellation.Token));

        Assert.Multiple(
            () => Assert.False(meal.IsNutritionAutoCalculated),
            () => Assert.Equal(123.46, meal.TotalCalories),
            () => Assert.Equal(3.46, meal.TotalProteins),
            () => Assert.Equal(4.57, meal.TotalFats),
            () => Assert.Equal(5.68, meal.TotalCarbs),
            () => Assert.Equal(1.23, meal.TotalFiber),
            () => Assert.Equal(0.79, meal.TotalAlcohol),
            () => Assert.Equal(123.46, meal.ManualCalories),
            () => Assert.Equal(3.46, meal.ManualProteins),
            () => Assert.Equal(4.57, meal.ManualFats),
            () => Assert.Equal(5.68, meal.ManualCarbs),
            () => Assert.Equal(1.23, meal.ManualFiber),
            () => Assert.Equal(0.79, meal.ManualAlcohol));

        ResultAssert.Success(await MealNutritionApplier.ApplyAsync(meal, user, nutrition,
            manual with { IsNutritionAutoCalculated = true }, cancellation.Token));

        Assert.Multiple(
            () => Assert.Equal(new MealNutritionInput(IsNutritionAutoCalculated: true,
                ManualCalories: null, ManualProteins: null, ManualFats: null, ManualCarbs: null, ManualFiber: null, ManualAlcohol: null), original),
            () => Assert.False(manual.IsNutritionAutoCalculated),
            () => Assert.Equal(123.456, manual.ManualCalories),
            () => Assert.True(meal.IsNutritionAutoCalculated),
            () => Assert.Equal(210, meal.TotalCalories),
            () => Assert.Equal(11, meal.TotalProteins),
            () => Assert.Equal(12, meal.TotalFats),
            () => Assert.Equal(13, meal.TotalCarbs),
            () => Assert.Equal(14, meal.TotalFiber),
            () => Assert.Equal(0, meal.TotalAlcohol),
            () => Assert.Null(meal.ManualCalories),
            () => Assert.Null(meal.ManualProteins),
            () => Assert.Null(meal.ManualFats),
            () => Assert.Null(meal.ManualCarbs),
            () => Assert.Null(meal.ManualFiber),
            () => Assert.Null(meal.ManualAlcohol));
        await nutrition.Received(2).CalculateAsync(meal, user, cancellation.Token);
    }
}
