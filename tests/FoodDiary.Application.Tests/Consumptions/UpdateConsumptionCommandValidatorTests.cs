using FluentValidation.TestHelper;
using FoodDiary.Application.Consumptions.Commands.UpdateConsumption;
using FoodDiary.Application.Consumptions.Common;

namespace FoodDiary.Application.Tests.Consumptions;

[ExcludeFromCodeCoverage]
public class UpdateConsumptionCommandValidatorTests {
    private readonly UpdateConsumptionCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenConsumptionIdIsEmpty_HasError() {
        UpdateConsumptionCommand command = CreateCommand(consumptionId: Guid.Empty);
        TestValidationResult<UpdateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.ConsumptionId);
    }

    [Fact]
    public async Task Validate_WhenNoItemsAndNoAiSessions_HasError() {
        UpdateConsumptionCommand command = CreateCommand(items: [], aiSessions: []);
        TestValidationResult<UpdateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenManualItemAmountIsTooLarge_HasError() {
        UpdateConsumptionCommand command = CreateCommand(items: [new ConsumptionItemInput(Guid.NewGuid(), null, 1_000_001)]);
        TestValidationResult<UpdateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenAiItemAmountIsZero_HasError() {
        UpdateConsumptionCommand command = CreateCommand(
            items: [],
            aiSessions: [new ConsumptionAiSessionInput(null, "Text", DateTime.UtcNow, null, [
                new ConsumptionAiItemInput("Apple", null, 0, "g", 100, 10, 5, 20, 3, 0),
            ])]);
        TestValidationResult<UpdateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenManualNutritionMissingFiber_HasError() {
        UpdateConsumptionCommand command = CreateCommand(
            isAutoCalculated: false,
            manualCalories: 100,
            manualProteins: 10,
            manualFats: 5,
            manualCarbs: 20,
            manualFiber: null);
        TestValidationResult<UpdateConsumptionCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.ManualFiber);
    }

    private static UpdateConsumptionCommand CreateCommand(
        Guid? userId = null,
        Guid? consumptionId = null,
        string? mealType = "Lunch",
        IReadOnlyList<ConsumptionItemInput>? items = null,
        IReadOnlyList<ConsumptionAiSessionInput>? aiSessions = null,
        bool isAutoCalculated = true,
        double? manualCalories = null,
        double? manualProteins = null,
        double? manualFats = null,
        double? manualCarbs = null,
        double? manualFiber = null) {
        return new UpdateConsumptionCommand(
            userId ?? Guid.NewGuid(),
            consumptionId ?? Guid.NewGuid(),
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
            PreMealSatietyLevel: 3,
            PostMealSatietyLevel: 4);
    }
}
