using FluentValidation;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.Recipes.Common.Validators;

internal sealed class RecipeIngredientInputValidator : AbstractValidator<RecipeIngredientInput> {
    public RecipeIngredientInputValidator() {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Ingredient amount must be greater than zero")
            .LessThanOrEqualTo(RecipeIngredient.MaxAmount)
            .WithMessage(FormattableString.Invariant(
                $"Ingredient amount must not exceed {RecipeIngredient.MaxAmount}"));

        RuleFor(x => x)
            .Must(input => input.ProductId.HasValue ^ input.NestedRecipeId.HasValue)
            .WithMessage("Ingredient must reference either productId or nestedRecipeId");
    }
}
