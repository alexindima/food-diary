using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Nutrition.Common;
using FoodDiary.Application.Recipes.Recipes.Commands.CreateRecipe;
using FoodDiary.Application.Recipes.Recipes.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Recipes;

[ExcludeFromCodeCoverage]
public class CreateRecipeCommandValidatorTests {
    [Fact]
    public async Task ValidateAsync_WithDuplicateStepOrder_ReturnsValidationError() {
        var validator = new CreateRecipeCommandValidator();
        CreateRecipeCommand command = CreateCommand(
            Guid.NewGuid(),
            [
                CreateStep(order: 1, "Step 1"),
                CreateStep(order: 1, "Step 2 duplicate"),
            ]);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "Steps"
, StringComparison.Ordinal) && string.Equals(e.ErrorCode, "Validation.Invalid"
, StringComparison.Ordinal) && string.Equals(e.ErrorMessage, "Step order values must be unique", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithDistinctEffectiveStepOrder_Passes() {
        var validator = new CreateRecipeCommandValidator();
        CreateRecipeCommand command = CreateCommand(
            Guid.NewGuid(),
            [
                CreateStep(order: 0, "Step uses index fallback to 1"),
                CreateStep(order: 2, "Explicit step 2"),
            ]);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithNullSteps_ReturnsValidationError() {
        var validator = new CreateRecipeCommandValidator();
        CreateRecipeCommand command = CreateCommand(Guid.NewGuid(), steps: null);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "Steps", StringComparison.Ordinal)
            && string.Equals(e.ErrorCode, "Validation.Required", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithNullStep_ReturnsValidationErrorWithoutThrowing() {
        var validator = new CreateRecipeCommandValidator();
        CreateRecipeCommand command = CreateCommand(Guid.NewGuid(), [null!]);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Steps[0]", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithNullIngredient_ReturnsValidationErrorWithoutThrowing() {
        RecipeStepInput step = CreateStep(order: 1, "Step") with { Ingredients = [null!] };
        CreateRecipeCommand command = CreateCommand(Guid.NewGuid(), [step]);

        ValidationResult result = await new CreateRecipeCommandValidator().ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Steps[0].Ingredients[0]", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithEmptySteps_ReturnsValidationError() {
        var validator = new CreateRecipeCommandValidator();
        CreateRecipeCommand command = CreateCommand(Guid.NewGuid(), []);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "Steps", StringComparison.Ordinal)
            && string.Equals(e.ErrorCode, "Validation.Invalid", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithManualNutritionAboveMaximum_ReturnsValidationError() {
        var validator = new CreateRecipeCommandValidator();
        CreateRecipeCommand command = CreateCommand(Guid.NewGuid(), [CreateStep(order: 1, "Step")]) with {
            CalculateNutritionAutomatically = false,
            ManualCalories = ManualNutritionLimits.MaxCalories + 1,
            ManualProteins = ManualNutritionLimits.MaxNutrient + 1,
            ManualFats = 10,
            ManualCarbs = 20,
            ManualFiber = 3,
            ManualAlcohol = 0,
        };

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, nameof(CreateRecipeCommand.ManualCalories), StringComparison.Ordinal));
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, nameof(CreateRecipeCommand.ManualProteins), StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithValuesBeyondDomainLimits_ReturnsValidationErrors() {
        RecipeStepInput step = CreateStep(
            order: 1,
            description: new string('s', RecipeStepContentState.InstructionMaxLength + 1)) with {
            Title = new string('t', RecipeStepContentState.TitleMaxLength + 1),
            ImageUrl = new string('i', RecipeStepContentState.ImageUrlMaxLength + 1),
            Ingredients = [new RecipeIngredientInput(
                Guid.NewGuid(), NestedRecipeId: null, Amount: RecipeIngredient.MaxAmount + 1)],
        };
        CreateRecipeCommand command = CreateCommand(Guid.NewGuid(), [step]) with {
            Name = new string('n', Recipe.NameMaxLength + 1),
            Description = new string('d', Recipe.DescriptionMaxLength + 1),
            Comment = new string('c', Recipe.CommentMaxLength + 1),
            Category = new string('g', Recipe.CategoryMaxLength + 1),
            ImageUrl = new string('i', Recipe.ImageUrlMaxLength + 1),
        };

        ValidationResult result = await new CreateRecipeCommandValidator().ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, nameof(CreateRecipeCommand.Name), StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, nameof(CreateRecipeCommand.Description), StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Steps[0].Description", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Steps[0].Ingredients[0].Amount", StringComparison.Ordinal));
    }

    private static CreateRecipeCommand CreateCommand(Guid userId, IReadOnlyList<RecipeStepInput>? steps) {
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
            Steps: steps!);
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
