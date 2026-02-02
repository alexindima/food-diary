using FluentValidation;

namespace FoodDiary.Application.Recipes.Commands.Common.Validators;

internal class RecipeStepInputValidator : AbstractValidator<RecipeStepInput>
{
    public RecipeStepInputValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(120)
            .WithMessage("Step title must be 120 characters or less");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Step description is required");

        RuleFor(x => x.Ingredients)
            .NotNull()
            .WithMessage("Ingredients collection is required")
            .Must(ingredients => ingredients.Count > 0)
            .WithMessage("Each step must contain at least one ingredient");

        RuleForEach(x => x.Ingredients)
            .SetValidator(new RecipeIngredientInputValidator());
    }
}

internal class RecipeIngredientInputValidator : AbstractValidator<RecipeIngredientInput>
{
    public RecipeIngredientInputValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Ingredient amount must be greater than zero");

        RuleFor(x => x)
            .Must(input => (input.ProductId.HasValue ^ input.NestedRecipeId.HasValue))
            .WithMessage("Ingredient must reference either productId or nestedRecipeId");
    }
}
