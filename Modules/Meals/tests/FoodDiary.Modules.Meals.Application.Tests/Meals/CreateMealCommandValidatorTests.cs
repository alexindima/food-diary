using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Nutrition.Common;
using FoodDiary.Application.Meals.Commands.CreateMeal;
using FoodDiary.Application.Meals.Common;

namespace FoodDiary.Application.Tests.Meals;

[ExcludeFromCodeCoverage]
public class CreateMealCommandValidatorTests {
    private readonly CreateMealCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenUserIdIsNull_HasError() {
        CreateMealCommand command = CreateCommand(useNullUserId: true);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task Validate_WhenUserIdIsEmpty_HasError() {
        CreateMealCommand command = CreateCommand(userId: Guid.Empty);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task Validate_WhenMealTypeInvalid_HasError() {
        CreateMealCommand command = CreateCommand(mealType: "InvalidType");
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.MealType);
    }

    [Fact]
    public async Task Validate_WhenMealTypeIsNull_NoError() {
        CreateMealCommand command = CreateCommand(mealType: null);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(c => c.MealType);
    }

    [Fact]
    public async Task Validate_WhenMealTypeIsValid_NoError() {
        CreateMealCommand command = CreateCommand(mealType: "Lunch");
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(c => c.MealType);
    }

    [Fact]
    public async Task Validate_WhenNoItemsAndNoAiSessions_HasError() {
        CreateMealCommand command = CreateCommand(items: [], aiSessions: []);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenMealItemsContainNull_HasErrorWithoutThrowing() {
        CreateMealCommand command = CreateCommand(items: [null!]);

        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Items[0]");
    }

    [Fact]
    public async Task Validate_WhenAiSessionsContainNull_HasErrorWithoutThrowing() {
        CreateMealCommand command = CreateCommand(items: [], aiSessions: [null!]);

        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("AiSessions[0]");
    }

    [Fact]
    public async Task Validate_WhenAiSessionItemsContainNull_HasErrorWithoutThrowing() {
        CreateMealCommand command = CreateCommand(
            items: [],
            aiSessions: [new MealAiSessionInput(
                ImageAssetId: null,
                Source: "Text",
                RecognizedAtUtc: DateTime.UtcNow,
                Notes: null,
                Items: [null!])]);

        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("AiSessions[0].Items[0]");
    }

    [Fact]
    public async Task Validate_WhenPreMealSatietyOutOfRange_HasError() {
        CreateMealCommand command = CreateCommand(preMealSatiety: -1);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.PreMealSatietyLevel);
    }

    [Fact]
    public async Task Validate_WhenPostMealSatietyOutOfRange_HasError() {
        CreateMealCommand command = CreateCommand(postMealSatiety: 10);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.PostMealSatietyLevel);
    }

    [Fact]
    public async Task Validate_WhenItemHasNeitherProductNorRecipe_HasError() {
        CreateMealCommand command = CreateCommand(items: [new MealItemInput(ProductId: null, RecipeId: null, 100)]);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenItemHasBothProductAndRecipe_HasError() {
        CreateMealCommand command = CreateCommand(items: [new MealItemInput(Guid.NewGuid(), Guid.NewGuid(), 100)]);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenItemAmountIsZero_HasError() {
        CreateMealCommand command = CreateCommand(items: [new MealItemInput(Guid.NewGuid(), RecipeId: null, 0)]);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenItemAmountIsTooLarge_HasError() {
        CreateMealCommand command = CreateCommand(items: [new MealItemInput(Guid.NewGuid(), RecipeId: null, 1_000_001)]);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenAiItemNameIsBlank_HasError() {
        CreateMealCommand command = CreateCommand(
            items: [],
            aiSessions: [new MealAiSessionInput(ImageAssetId: null, "Text", DateTime.UtcNow, Notes: null, [
                new MealAiItemInput("", NameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0),
            ])]);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenAiItemNutritionIsNegative_HasError() {
        CreateMealCommand command = CreateCommand(
            items: [],
            aiSessions: [new MealAiSessionInput(ImageAssetId: null, "Text", DateTime.UtcNow, Notes: null, [
                new MealAiItemInput("Apple", NameLocal: null, 100, "g", -1, 10, 5, 20, 3, 0),
            ])]);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenAiItemConfidenceIsOutOfRange_HasError() {
        CreateMealCommand command = CreateCommand(
            items: [],
            aiSessions: [new MealAiSessionInput(ImageAssetId: null, "Text", DateTime.UtcNow, Notes: null, [
                new MealAiItemInput("Apple", NameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0, Confidence: 1.1),
            ])]);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenAiItemResolutionIsUnknown_HasError() {
        CreateMealCommand command = CreateCommand(
            items: [],
            aiSessions: [new MealAiSessionInput(ImageAssetId: null, "Text", DateTime.UtcNow, Notes: null, [
                new MealAiItemInput("Apple", NameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0, Resolution: "UnknownResolution"),
            ])]);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenAiSessionRecognizedAtIsUnspecified_HasError() {
        CreateMealCommand command = CreateCommand(
            items: [],
            aiSessions: [new MealAiSessionInput(ImageAssetId: null, "Text", new DateTime(2026, 3, 26, 12, 0, 0), Notes: null, [
                new MealAiItemInput("Apple", NameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0),
            ])]);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenManualNutritionMissingCalories_HasError() {
        CreateMealCommand command = CreateCommand(
            isAutoCalculated: false,
            manualCalories: null,
            manualProteins: 10, manualFats: 5, manualCarbs: 20, manualFiber: 3);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.ManualCalories);
    }

    [Fact]
    public async Task Validate_WhenManualNutritionNegativeValue_HasError() {
        CreateMealCommand command = CreateCommand(
            isAutoCalculated: false,
            manualCalories: -10,
            manualProteins: 10, manualFats: 5, manualCarbs: 20, manualFiber: 3);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.ManualCalories);
    }

    [Fact]
    public async Task Validate_WhenManualNutritionExceedsMaximum_HasError() {
        CreateMealCommand command = CreateCommand(
            isAutoCalculated: false,
            manualCalories: ManualNutritionLimits.MaxCalories + 1,
            manualProteins: ManualNutritionLimits.MaxNutrient + 1,
            manualFats: 5,
            manualCarbs: 20,
            manualFiber: 3);

        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ManualCalories);
        result.ShouldHaveValidationErrorFor(c => c.ManualProteins);
    }

    [Fact]
    public async Task Validate_WhenAutoCalculated_ManualFieldsNotRequired() {
        CreateMealCommand command = CreateCommand(isAutoCalculated: true,
            manualCalories: null, manualProteins: null, manualFats: null,
            manualCarbs: null, manualFiber: null);
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(c => c.ManualCalories);
    }

    [Fact]
    public async Task Validate_WhenValidCommand_NoErrors() {
        CreateMealCommand command = CreateCommand();
        TestValidationResult<CreateMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static readonly Guid DefaultUserId = Guid.NewGuid();

    private static CreateMealCommand CreateCommand(
        Guid? userId = null,
        bool useNullUserId = false,
        string? mealType = "Lunch",
        IReadOnlyList<MealItemInput>? items = null,
        IReadOnlyList<MealAiSessionInput>? aiSessions = null,
        bool isAutoCalculated = true,
        double? manualCalories = null,
        double? manualProteins = null,
        double? manualFats = null,
        double? manualCarbs = null,
        double? manualFiber = null,
        int preMealSatiety = 3,
        int postMealSatiety = 4) {
        return new CreateMealCommand(
            useNullUserId ? null : (userId ?? DefaultUserId),
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
            PreMealSatietyLevel: preMealSatiety,
            PostMealSatietyLevel: postMealSatiety);
    }
}
