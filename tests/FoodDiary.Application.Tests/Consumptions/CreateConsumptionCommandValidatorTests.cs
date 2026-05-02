using FluentValidation.TestHelper;
using FoodDiary.Application.Consumptions.Commands.CreateConsumption;
using FoodDiary.Application.Consumptions.Common;

namespace FoodDiary.Application.Tests.Consumptions;

public class CreateConsumptionCommandValidatorTests {
    private readonly CreateConsumptionCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenUserIdIsNull_HasError() {
        var command = CreateCommand(useNullUserId: true);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task Validate_WhenUserIdIsEmpty_HasError() {
        var command = CreateCommand(userId: Guid.Empty);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task Validate_WhenMealTypeInvalid_HasError() {
        var command = CreateCommand(mealType: "InvalidType");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.MealType);
    }

    [Fact]
    public async Task Validate_WhenMealTypeIsNull_NoError() {
        var command = CreateCommand(mealType: null);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(c => c.MealType);
    }

    [Fact]
    public async Task Validate_WhenMealTypeIsValid_NoError() {
        var command = CreateCommand(mealType: "Lunch");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(c => c.MealType);
    }

    [Fact]
    public async Task Validate_WhenNoItemsAndNoAiSessions_HasError() {
        var command = CreateCommand(items: [], aiSessions: []);
        var result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenPreMealSatietyOutOfRange_HasError() {
        var command = CreateCommand(preMealSatiety: -1);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.PreMealSatietyLevel);
    }

    [Fact]
    public async Task Validate_WhenPostMealSatietyOutOfRange_HasError() {
        var command = CreateCommand(postMealSatiety: 10);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.PostMealSatietyLevel);
    }

    [Fact]
    public async Task Validate_WhenItemHasNeitherProductNorRecipe_HasError() {
        var command = CreateCommand(items: [new ConsumptionItemInput(null, null, 100)]);
        var result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenItemHasBothProductAndRecipe_HasError() {
        var command = CreateCommand(items: [new ConsumptionItemInput(Guid.NewGuid(), Guid.NewGuid(), 100)]);
        var result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenItemAmountIsZero_HasError() {
        var command = CreateCommand(items: [new ConsumptionItemInput(Guid.NewGuid(), null, 0)]);
        var result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenManualNutritionMissingCalories_HasError() {
        var command = CreateCommand(
            isAutoCalculated: false,
            manualCalories: null,
            manualProteins: 10, manualFats: 5, manualCarbs: 20, manualFiber: 3);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.ManualCalories);
    }

    [Fact]
    public async Task Validate_WhenManualNutritionNegativeValue_HasError() {
        var command = CreateCommand(
            isAutoCalculated: false,
            manualCalories: -10,
            manualProteins: 10, manualFats: 5, manualCarbs: 20, manualFiber: 3);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.ManualCalories);
    }

    [Fact]
    public async Task Validate_WhenAutoCalculated_ManualFieldsNotRequired() {
        var command = CreateCommand(isAutoCalculated: true,
            manualCalories: null, manualProteins: null, manualFats: null,
            manualCarbs: null, manualFiber: null);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(c => c.ManualCalories);
    }

    [Fact]
    public async Task Validate_WhenValidCommand_NoErrors() {
        var command = CreateCommand();
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static readonly Guid DefaultUserId = Guid.NewGuid();

    private static CreateConsumptionCommand CreateCommand(
        Guid? userId = null,
        bool useNullUserId = false,
        string? mealType = "Lunch",
        IReadOnlyList<ConsumptionItemInput>? items = null,
        IReadOnlyList<ConsumptionAiSessionInput>? aiSessions = null,
        bool isAutoCalculated = true,
        double? manualCalories = null,
        double? manualProteins = null,
        double? manualFats = null,
        double? manualCarbs = null,
        double? manualFiber = null,
        int preMealSatiety = 3,
        int postMealSatiety = 4) {
        return new CreateConsumptionCommand(
            useNullUserId ? null : (userId ?? DefaultUserId),
            DateTime.UtcNow,
            mealType,
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            Items: items ?? [new ConsumptionItemInput(Guid.NewGuid(), null, 100)],
            AiSessions: aiSessions ?? [],
            IsNutritionAutoCalculated: isAutoCalculated,
            ManualCalories: manualCalories,
            ManualProteins: manualProteins,
            ManualFats: manualFats,
            ManualCarbs: manualCarbs,
            ManualFiber: manualFiber,
            ManualAlcohol: null,
            PreMealSatietyLevel: preMealSatiety,
            PostMealSatietyLevel: postMealSatiety);
    }
}
