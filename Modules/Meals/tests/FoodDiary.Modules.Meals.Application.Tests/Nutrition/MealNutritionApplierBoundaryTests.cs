using FoodDiary.Modules.Meals.Application.Services;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Meals.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Meals.Application.Tests.Nutrition;

[ExcludeFromCodeCoverage]
public sealed class MealNutritionApplierBoundaryTests {
    [Fact]
    public async Task InvalidManualInput_ReportsItsFirstValidationErrorWithoutCallingTheService() {
        var user = UserId.New();
        var meal = Meal.Create(user, new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc));
        using var cancellation = new CancellationTokenSource();
        IMealNutritionService service = Substitute.For<IMealNutritionService>();
        service.CalculateAsync(meal, user, cancellation.Token).Returns(
            Result.Failure<MealNutritionSummary>(new Error("Meal.NutritionUnavailable", "Service failure must not replace input validation.")));
        var input = new MealNutritionInput(IsNutritionAutoCalculated: false,
            ManualCalories: null, ManualProteins: -1, ManualFats: 2, ManualCarbs: 3, ManualFiber: 4, ManualAlcohol: 0);

        Result result = await MealNutritionApplier.ApplyAsync(meal, user, service, input, cancellation.Token);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal("Validation.Required", result.Error.Code),
            () => Assert.Contains("ManualCalories", result.Error.Message, StringComparison.Ordinal));
        Assert.Empty(service.ReceivedCalls());
    }

    [Fact]
    public async Task ValidManualInput_WhenTheServiceFails_PropagatesItsErrorWithoutReplacingExistingTotals() {
        var user = UserId.New();
        var meal = Meal.Create(user, new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc));
        meal.ApplyNutrition(new MealNutritionUpdate(210, 11, 12, 13, 14, 0, IsAutoCalculated: true));
        using var cancellation = new CancellationTokenSource();
        var error = new Error("Meal.NutritionUnavailable", "The owner service could not calculate the item snapshots.");
        IMealNutritionService service = Substitute.For<IMealNutritionService>();
        service.CalculateAsync(meal, user, cancellation.Token).Returns(Result.Failure<MealNutritionSummary>(error));
        var input = new MealNutritionInput(IsNutritionAutoCalculated: false,
            ManualCalories: 123.456, ManualProteins: 3.456, ManualFats: 4.567, ManualCarbs: 5.678, ManualFiber: 1.234, ManualAlcohol: null);

        Result result = await MealNutritionApplier.ApplyAsync(meal, user, service, input, cancellation.Token);

        ResultAssert.Failure(result);
        Assert.Equal(error, result.Error);
        Assert.Multiple(
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
        await service.Received(1).CalculateAsync(meal, user, cancellation.Token);
        Assert.Single(service.ReceivedCalls());
    }
}
