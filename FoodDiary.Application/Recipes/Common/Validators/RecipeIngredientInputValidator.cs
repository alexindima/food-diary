using FluentValidation;

namespace FoodDiary.Application.Recipes.Common.Validators;

internal class RecipeIngredientInputValidator : AbstractValidator<RecipeIngredientInput> {
    public RecipeIngredientInputValidator() {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Ingredient amount must be greater than zero");

        RuleFor(x => x)
            .Must(input => input.ProductId.HasValue ^ input.NestedRecipeId.HasValue)
            .WithMessage("Ingredient must reference either productId or nestedRecipeId");
    }
}
