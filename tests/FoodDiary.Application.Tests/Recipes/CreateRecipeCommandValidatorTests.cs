using FoodDiary.Application.Recipes.Commands.CreateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Recipes;

public class CreateRecipeCommandValidatorTests {
    [Fact]
    public async Task ValidateAsync_WithDuplicateStepOrder_ReturnsValidationError() {
        var validator = new CreateRecipeCommandValidator();
        var command = CreateCommand(
            UserId.New(),
            [
                CreateStep(order: 1, "Step 1"),
                CreateStep(order: 1, "Step 2 duplicate")
            ]);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Steps"
            && e.ErrorCode == "Validation.Invalid"
            && e.ErrorMessage == "Step order values must be unique");
    }

    [Fact]
    public async Task ValidateAsync_WithDistinctEffectiveStepOrder_Passes() {
        var validator = new CreateRecipeCommandValidator();
        var command = CreateCommand(
            UserId.New(),
            [
                CreateStep(order: 0, "Step uses index fallback to 1"),
                CreateStep(order: 2, "Explicit step 2")
            ]);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    private static CreateRecipeCommand CreateCommand(UserId userId, IReadOnlyList<RecipeStepInput> steps) {
        return new CreateRecipeCommand(
            userId,
            Name: "Soup",
            Description: "Desc",
            Comment: "Comment",
            Category: "Main",
            ImageUrl: null,
            ImageAssetId: null,
            PrepTime: 10,
            CookTime: 20,
            Servings: 2,
            Visibility: Visibility.Public.ToString(),
            CalculateNutritionAutomatically: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            Steps: steps);
    }

    private static RecipeStepInput CreateStep(int order, string description) {
        return new RecipeStepInput(
            Order: order,
            Description: description,
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: Guid.NewGuid(), NestedRecipeId: null, Amount: 100)]);
    }
}
