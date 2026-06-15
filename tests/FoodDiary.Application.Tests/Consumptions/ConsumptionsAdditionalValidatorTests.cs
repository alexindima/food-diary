using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Consumptions.Commands.RepeatMeal;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;

namespace FoodDiary.Application.Tests.Consumptions;

[ExcludeFromCodeCoverage]
public class ConsumptionsAdditionalValidatorTests {
    // â”€â”€ RepeatMealCommandValidator â”€â”€

    [Fact]
    public async Task RepeatMeal_WithNullUserId_HasError() {
        TestValidationResult<RepeatMealCommand> result = await new RepeatMealCommandValidator().TestValidateAsync(
            new RepeatMealCommand(UserId: null, Guid.NewGuid(), DateTime.UtcNow, MealType: null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task RepeatMeal_WithEmptyMealId_HasError() {
        TestValidationResult<RepeatMealCommand> result = await new RepeatMealCommandValidator().TestValidateAsync(
            new RepeatMealCommand(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, MealType: null));
        result.ShouldHaveValidationErrorFor(c => c.MealId);
    }

    // â”€â”€ ConsumptionItemValidator â”€â”€

    [Fact]
    public void ConsumptionItem_WithNoProductOrRecipe_ReturnsFailure() {
        var item = new ConsumptionItemInput(ProductId: null, RecipeId: null, 100);
        Result result = ConsumptionItemValidator.Validate(item);
        ResultAssert.Failure(result);
    }

    [Fact]
    public void ConsumptionItem_WithBothProductAndRecipe_ReturnsFailure() {
        var item = new ConsumptionItemInput(Guid.NewGuid(), Guid.NewGuid(), 100);
        Result result = ConsumptionItemValidator.Validate(item);
        ResultAssert.Failure(result);
    }

    [Fact]
    public void ConsumptionItem_WithZeroAmount_ReturnsFailure() {
        var item = new ConsumptionItemInput(Guid.NewGuid(), RecipeId: null, 0);
        Result result = ConsumptionItemValidator.Validate(item);
        ResultAssert.Failure(result);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ConsumptionItem_WithNonFiniteAmount_ReturnsFailure(double amount) {
        var item = new ConsumptionItemInput(Guid.NewGuid(), RecipeId: null, amount);

        Result result = ConsumptionItemValidator.Validate(item);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public void ConsumptionItem_WithTooLargeAmount_ReturnsFailure() {
        var item = new ConsumptionItemInput(Guid.NewGuid(), RecipeId: null, 1_000_000.01d);

        Result result = ConsumptionItemValidator.Validate(item);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public void ConsumptionItem_WithValidProductAndAmount_ReturnsSuccess() {
        var item = new ConsumptionItemInput(Guid.NewGuid(), RecipeId: null, 150);
        Result result = ConsumptionItemValidator.Validate(item);
        ResultAssert.Success(result);
    }

    // â”€â”€ ManualNutritionValidator â”€â”€

    [Fact]
    public void ManualNutrition_WithNullCalories_ReturnsFailure() {
        Result<ManualNutritionInput> result = ManualNutritionValidator.Validate(calories: null, 30, 10, 50, 5, 0);
        ResultAssert.Failure(result);
    }

    [Theory]
    [InlineData("proteins")]
    [InlineData("fats")]
    [InlineData("carbs")]
    [InlineData("fiber")]
    public void ManualNutrition_WithMissingRequiredMacro_ReturnsFailure(string missingField) {
        Result<ManualNutritionInput> result = ManualNutritionValidator.Validate(
            200,
            string.Equals(missingField, "proteins", StringComparison.Ordinal) ? null : 30,
            string.Equals(missingField, "fats", StringComparison.Ordinal) ? null : 10,
            string.Equals(missingField, "carbs", StringComparison.Ordinal) ? null : 50,
            string.Equals(missingField, "fiber", StringComparison.Ordinal) ? null : 5,
            0);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Required", result.Error.Code);
    }

    [Fact]
    public void ManualNutrition_WithNegativeValue_ReturnsFailure() {
        Result<ManualNutritionInput> result = ManualNutritionValidator.Validate(200, -1, 10, 50, 5, 0);
        ResultAssert.Failure(result);
    }

    [Fact]
    public void ManualNutrition_WithNegativeAlcohol_ReturnsFailure() {
        Result<ManualNutritionInput> result = ManualNutritionValidator.Validate(200, 30, 10, 50, 5, -0.1);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public void ManualNutrition_WithValidData_ReturnsSuccess() {
        Result<ManualNutritionInput> result = ManualNutritionValidator.Validate(200, 30, 10, 50, 5, 0);
        ResultAssert.Success(result);
        Assert.Equal(200, result.Value.Calories);
        Assert.Equal(30, result.Value.Proteins);
    }

    [Fact]
    public void ManualNutrition_WithNullAlcohol_DefaultsToZero() {
        Result<ManualNutritionInput> result = ManualNutritionValidator.Validate(200, 30, 10, 50, 5, alcohol: null);
        ResultAssert.Success(result);
        Assert.Equal(0, result.Value.Alcohol);
    }

    // â”€â”€ SatietyLevelValidator â”€â”€

    [Fact]
    public void SatietyLevel_WithValidLevels_ReturnsSuccess() {
        Result result = SatietyLevelValidator.Validate(3, 5);
        ResultAssert.Success(result);
    }

    [Fact]
    public void SatietyLevel_WithOutOfRangePre_ReturnsFailure() {
        Result result = SatietyLevelValidator.Validate(6, 5);
        ResultAssert.Failure(result);
    }

    [Fact]
    public void SatietyLevel_WithOutOfRangePost_ReturnsFailure() {
        Result result = SatietyLevelValidator.Validate(5, -1);
        ResultAssert.Failure(result);
    }

    [Fact]
    public void SatietyLevel_WithNullValues_ReturnsSuccess() {
        Result result = SatietyLevelValidator.Validate(preMealSatietyLevel: null, postMealSatietyLevel: null);
        ResultAssert.Success(result);
    }
}
