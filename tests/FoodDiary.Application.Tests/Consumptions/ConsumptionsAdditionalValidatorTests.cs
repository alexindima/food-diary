using FluentValidation.TestHelper;
using FoodDiary.Application.Consumptions.Commands.RepeatMeal;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;

namespace FoodDiary.Application.Tests.Consumptions;

[ExcludeFromCodeCoverage]
public class ConsumptionsAdditionalValidatorTests {
    // â”€â”€ RepeatMealCommandValidator â”€â”€

    [Fact]
    public async Task RepeatMeal_WithNullUserId_HasError() {
        var result = await new RepeatMealCommandValidator().TestValidateAsync(
            new RepeatMealCommand(null, Guid.NewGuid(), DateTime.UtcNow, null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task RepeatMeal_WithEmptyMealId_HasError() {
        var result = await new RepeatMealCommandValidator().TestValidateAsync(
            new RepeatMealCommand(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, null));
        result.ShouldHaveValidationErrorFor(c => c.MealId);
    }

    // â”€â”€ ConsumptionItemValidator â”€â”€

    [Fact]
    public void ConsumptionItem_WithNoProductOrRecipe_ReturnsFailure() {
        var item = new ConsumptionItemInput(null, null, 100);
        var result = ConsumptionItemValidator.Validate(item);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ConsumptionItem_WithBothProductAndRecipe_ReturnsFailure() {
        var item = new ConsumptionItemInput(Guid.NewGuid(), Guid.NewGuid(), 100);
        var result = ConsumptionItemValidator.Validate(item);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ConsumptionItem_WithZeroAmount_ReturnsFailure() {
        var item = new ConsumptionItemInput(Guid.NewGuid(), null, 0);
        var result = ConsumptionItemValidator.Validate(item);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ConsumptionItem_WithValidProductAndAmount_ReturnsSuccess() {
        var item = new ConsumptionItemInput(Guid.NewGuid(), null, 150);
        var result = ConsumptionItemValidator.Validate(item);
        Assert.True(result.IsSuccess);
    }

    // â”€â”€ ManualNutritionValidator â”€â”€

    [Fact]
    public void ManualNutrition_WithNullCalories_ReturnsFailure() {
        var result = ManualNutritionValidator.Validate(null, 30, 10, 50, 5, 0);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ManualNutrition_WithNegativeValue_ReturnsFailure() {
        var result = ManualNutritionValidator.Validate(200, -1, 10, 50, 5, 0);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ManualNutrition_WithValidData_ReturnsSuccess() {
        var result = ManualNutritionValidator.Validate(200, 30, 10, 50, 5, 0);
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.Value.Calories);
        Assert.Equal(30, result.Value.Proteins);
    }

    [Fact]
    public void ManualNutrition_WithNullAlcohol_DefaultsToZero() {
        var result = ManualNutritionValidator.Validate(200, 30, 10, 50, 5, null);
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Alcohol);
    }

    // â”€â”€ SatietyLevelValidator â”€â”€

    [Fact]
    public void SatietyLevel_WithValidLevels_ReturnsSuccess() {
        var result = SatietyLevelValidator.Validate(3, 5);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void SatietyLevel_WithOutOfRangePre_ReturnsFailure() {
        var result = SatietyLevelValidator.Validate(6, 5);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void SatietyLevel_WithOutOfRangePost_ReturnsFailure() {
        var result = SatietyLevelValidator.Validate(5, -1);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void SatietyLevel_WithNullValues_ReturnsSuccess() {
        var result = SatietyLevelValidator.Validate(null, null);
        Assert.True(result.IsSuccess);
    }
}
