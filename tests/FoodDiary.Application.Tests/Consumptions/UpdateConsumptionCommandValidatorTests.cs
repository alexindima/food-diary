using FluentValidation.TestHelper;
using FoodDiary.Application.Consumptions.Commands.UpdateConsumption;
using FoodDiary.Application.Consumptions.Common;

namespace FoodDiary.Application.Tests.Consumptions;

public class UpdateConsumptionCommandValidatorTests {
    private readonly UpdateConsumptionCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenConsumptionIdIsEmpty_HasError() {
        var command = CreateCommand(consumptionId: Guid.Empty);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.ConsumptionId);
    }

    [Fact]
    public async Task Validate_WhenNoItemsAndNoAiSessions_HasError() {
        var command = CreateCommand(items: [], aiSessions: []);
        var result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenManualItemAmountIsTooLarge_HasError() {
        var command = CreateCommand(items: [new ConsumptionItemInput(Guid.NewGuid(), null, 1_000_001)]);
        var result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenAiItemAmountIsZero_HasError() {
        var command = CreateCommand(
            items: [],
            aiSessions: [new ConsumptionAiSessionInput(null, "Text", DateTime.UtcNow, null, [
                new ConsumptionAiItemInput("Apple", null, 0, "g", 100, 10, 5, 20, 3, 0)
            ])]);
        var result = await _validator.TestValidateAsync(command);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenManualNutritionMissingFiber_HasError() {
        var command = CreateCommand(
            isAutoCalculated: false,
            manualCalories: 100,
            manualProteins: 10,
            manualFats: 5,
            manualCarbs: 20,
            manualFiber: null);
        var result = await _validator.TestValidateAsync(command);
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
