using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Nutrition.Common;
using FoodDiary.Application.Meals.Commands.UpdateMeal;
using FoodDiary.Application.Meals.Common;

namespace FoodDiary.Application.Tests.Meals;

[ExcludeFromCodeCoverage]
public class UpdateMealCommandValidatorTests {
    private readonly UpdateMealCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenMealIdIsEmpty_HasError() {
        UpdateMealCommand command = CreateCommand(mealId: Guid.Empty);
        TestValidationResult<UpdateMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.MealId);
    }

    [Fact]
    public async Task Validate_WhenNoItemsAndNoAiSessions_HasError() {
        UpdateMealCommand command = CreateCommand(items: [], aiSessions: []);
        TestValidationResult<UpdateMealCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenMealItemsContainNull_HasErrorWithoutThrowing() {
        UpdateMealCommand command = CreateCommand(items: [null!]);

        TestValidationResult<UpdateMealCommand> result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Items[0]");
    }

    [Fact]
    public async Task Validate_WhenManualItemAmountIsTooLarge_HasError() {
        UpdateMealCommand command = CreateCommand(items: [new MealItemInput(Guid.NewGuid(), RecipeId: null, 1_000_001)]);
        TestValidationResult<UpdateMealCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenAiItemAmountIsZero_HasError() {
        UpdateMealCommand command = CreateCommand(
            items: [],
            aiSessions: [new MealAiSessionInput(ImageAssetId: null, "Text", DateTime.UtcNow, Notes: null, [
                new MealAiItemInput("Apple", NameLocal: null, 0, "g", 100, 10, 5, 20, 3, 0),
            ])]);
        TestValidationResult<UpdateMealCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenManualNutritionMissingFiber_HasError() {
        UpdateMealCommand command = CreateCommand(
            isAutoCalculated: false,
            manualCalories: 100,
            manualProteins: 10,
            manualFats: 5,
            manualCarbs: 20,
            manualFiber: null);
        TestValidationResult<UpdateMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.ManualFiber);
    }

    [Fact]
    public async Task Validate_WhenManualNutritionExceedsMaximum_HasError() {
        UpdateMealCommand command = CreateCommand(
            isAutoCalculated: false,
            manualCalories: ManualNutritionLimits.MaxCalories + 1,
            manualProteins: 10,
            manualFats: ManualNutritionLimits.MaxNutrient + 1,
            manualCarbs: 20,
            manualFiber: 3);

        TestValidationResult<UpdateMealCommand> result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ManualCalories);
        result.ShouldHaveValidationErrorFor(c => c.ManualFats);
    }

    private static UpdateMealCommand CreateCommand(
        Guid? userId = null,
        Guid? mealId = null,
        string? mealType = "Lunch",
        IReadOnlyList<MealItemInput>? items = null,
        IReadOnlyList<MealAiSessionInput>? aiSessions = null,
        bool isAutoCalculated = true,
        double? manualCalories = null,
        double? manualProteins = null,
        double? manualFats = null,
        double? manualCarbs = null,
        double? manualFiber = null) {
        return new UpdateMealCommand(
            userId ?? Guid.NewGuid(),
            mealId ?? Guid.NewGuid(),
            DateTime.UtcNow,
            mealType,
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            Items: items ?? [new MealItemInput(Guid.NewGuid(), RecipeId: null, 100)],
            AiSessions: aiSessions ?? [],
            IsNutritionAutoCalculated: isAutoCalculated,
            ManualCalories: manualCalories,
            ManualProteins: manualProteins,
            ManualFats: manualFats,
            ManualCarbs: manualCarbs,
            ManualFiber: manualFiber,
            ManualAlcohol: null,
            PreMealSatietyLevel: 3,
            PostMealSatietyLevel: 4);
    }
}
