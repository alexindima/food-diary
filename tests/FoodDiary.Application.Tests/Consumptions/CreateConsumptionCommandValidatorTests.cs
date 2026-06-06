using FluentValidation.TestHelper;
using FoodDiary.Application.Consumptions.Commands.CreateConsumption;
using FoodDiary.Application.Consumptions.Common;

namespace FoodDiary.Application.Tests.Consumptions;

[ExcludeFromCodeCoverage]
public class CreateConsumptionCommandValidatorTests {
    private readonly CreateConsumptionCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenUserIdIsNull_HasError() {
        CreateConsumptionCommand command = CreateCommand(useNullUserId: true);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task Validate_WhenUserIdIsEmpty_HasError() {
        CreateConsumptionCommand command = CreateCommand(userId: Guid.Empty);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task Validate_WhenMealTypeInvalid_HasError() {
        CreateConsumptionCommand command = CreateCommand(mealType: "InvalidType");
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.MealType);
    }

    [Fact]
    public async Task Validate_WhenMealTypeIsNull_NoError() {
        CreateConsumptionCommand command = CreateCommand(mealType: null);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(c => c.MealType);
    }

    [Fact]
    public async Task Validate_WhenMealTypeIsValid_NoError() {
        CreateConsumptionCommand command = CreateCommand(mealType: "Lunch");
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(c => c.MealType);
    }

    [Fact]
    public async Task Validate_WhenNoItemsAndNoAiSessions_HasError() {
        CreateConsumptionCommand command = CreateCommand(items: [], aiSessions: []);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenPreMealSatietyOutOfRange_HasError() {
        CreateConsumptionCommand command = CreateCommand(preMealSatiety: -1);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.PreMealSatietyLevel);
    }

    [Fact]
    public async Task Validate_WhenPostMealSatietyOutOfRange_HasError() {
        CreateConsumptionCommand command = CreateCommand(postMealSatiety: 10);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.PostMealSatietyLevel);
    }

    [Fact]
    public async Task Validate_WhenItemHasNeitherProductNorRecipe_HasError() {
        CreateConsumptionCommand command = CreateCommand(items: [new ConsumptionItemInput(ProductId: null, RecipeId: null, 100)]);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenItemHasBothProductAndRecipe_HasError() {
        CreateConsumptionCommand command = CreateCommand(items: [new ConsumptionItemInput(Guid.NewGuid(), Guid.NewGuid(), 100)]);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenItemAmountIsZero_HasError() {
        CreateConsumptionCommand command = CreateCommand(items: [new ConsumptionItemInput(Guid.NewGuid(), RecipeId: null, 0)]);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenItemAmountIsTooLarge_HasError() {
        CreateConsumptionCommand command = CreateCommand(items: [new ConsumptionItemInput(Guid.NewGuid(), RecipeId: null, 1_000_001)]);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenAiItemNameIsBlank_HasError() {
        CreateConsumptionCommand command = CreateCommand(
            items: [],
            aiSessions: [new ConsumptionAiSessionInput(ImageAssetId: null, "Text", DateTime.UtcNow, Notes: null, [
                new ConsumptionAiItemInput("", NameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0),
            ])]);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenAiItemNutritionIsNegative_HasError() {
        CreateConsumptionCommand command = CreateCommand(
            items: [],
            aiSessions: [new ConsumptionAiSessionInput(ImageAssetId: null, "Text", DateTime.UtcNow, Notes: null, [
                new ConsumptionAiItemInput("Apple", NameLocal: null, 100, "g", -1, 10, 5, 20, 3, 0),
            ])]);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenAiSessionRecognizedAtIsUnspecified_HasError() {
        CreateConsumptionCommand command = CreateCommand(
            items: [],
            aiSessions: [new ConsumptionAiSessionInput(ImageAssetId: null, "Text", new DateTime(2026, 3, 26, 12, 0, 0), Notes: null, [
                new ConsumptionAiItemInput("Apple", NameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0),
            ])]);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenManualNutritionMissingCalories_HasError() {
        CreateConsumptionCommand command = CreateCommand(
            isAutoCalculated: false,
            manualCalories: null,
            manualProteins: 10, manualFats: 5, manualCarbs: 20, manualFiber: 3);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.ManualCalories);
    }

    [Fact]
    public async Task Validate_WhenManualNutritionNegativeValue_HasError() {
        CreateConsumptionCommand command = CreateCommand(
            isAutoCalculated: false,
            manualCalories: -10,
            manualProteins: 10, manualFats: 5, manualCarbs: 20, manualFiber: 3);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.ManualCalories);
    }

    [Fact]
    public async Task Validate_WhenAutoCalculated_ManualFieldsNotRequired() {
        CreateConsumptionCommand command = CreateCommand(isAutoCalculated: true,
            manualCalories: null, manualProteins: null, manualFats: null,
            manualCarbs: null, manualFiber: null);
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(c => c.ManualCalories);
    }

    [Fact]
    public async Task Validate_WhenValidCommand_NoErrors() {
        CreateConsumptionCommand command = CreateCommand();
        TestValidationResult<CreateConsumptionCommand> result = await _validator.TestValidateAsync(command);
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
            Items: items ?? [new ConsumptionItemInput(Guid.NewGuid(), RecipeId: null, 100)],
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
