using FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;
using FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;
using FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;
using FoodDiary.Contracts.Ai;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Ai;

public class AiValidatorsTests {
    [Fact]
    public async Task AnalyzeFoodImageValidator_WithEmptyIds_Fails() {
        var validator = new AnalyzeFoodImageCommandValidator();
        var command = new AnalyzeFoodImageCommand(UserId.Empty, ImageAssetId.Empty, null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AnalyzeFoodImageValidator_WithTooLongDescription_Fails() {
        var validator = new AnalyzeFoodImageCommandValidator();
        var command = new AnalyzeFoodImageCommand(UserId.New(), ImageAssetId.New(), new string('x', 2049));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AnalyzeFoodImageValidator_WithValidData_Passes() {
        var validator = new AnalyzeFoodImageCommandValidator();
        var command = new AnalyzeFoodImageCommand(UserId.New(), ImageAssetId.New(), "some context");

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CalculateFoodNutritionValidator_WithEmptyItems_Fails() {
        var validator = new CalculateFoodNutritionCommandValidator();
        var command = new CalculateFoodNutritionCommand(UserId.New(), Array.Empty<FoodVisionItem>());

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CalculateFoodNutritionValidator_WithInvalidItem_Fails() {
        var validator = new CalculateFoodNutritionCommandValidator();
        var command = new CalculateFoodNutritionCommand(
            UserId.New(),
            [new FoodVisionItem("", null, 0, "", -1)]);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CalculateFoodNutritionValidator_WithValidItems_Passes() {
        var validator = new CalculateFoodNutritionCommandValidator();
        var command = new CalculateFoodNutritionCommand(
            UserId.New(),
            [new FoodVisionItem("apple", "яблоко", 120, "g", 0.95m)]);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetUserAiUsageSummaryValidator_WithEmptyUserId_Fails() {
        var validator = new GetUserAiUsageSummaryQueryValidator();
        var query = new GetUserAiUsageSummaryQuery(UserId.Empty);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetUserAiUsageSummaryValidator_WithValidUserId_Passes() {
        var validator = new GetUserAiUsageSummaryQueryValidator();
        var query = new GetUserAiUsageSummaryQuery(UserId.New());

        var result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }
}
