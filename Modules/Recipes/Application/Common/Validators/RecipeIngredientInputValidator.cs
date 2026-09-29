using FluentValidation;
using FoodDiary.Modules.Recipes.Domain.Entities;

namespace FoodDiary.Modules.Recipes.Application.Common.Validators;

internal sealed class RecipeIngredientInputValidator : AbstractValidator<RecipeIngredientInput> {
    public RecipeIngredientInputValidator() {
        RuleFor(x => x.PublicName).MaximumLength(RecipeIngredient.TextNameMaxLength);
        RuleFor(x => x.PublicUnit).MaximumLength(RecipeIngredient.AmountTextMaxLength);
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Ingredient amount must be greater than zero")
            .LessThanOrEqualTo(RecipeIngredient.MaxAmount)
            .WithMessage(FormattableString.Invariant(
                $"Ingredient amount must not exceed {RecipeIngredient.MaxAmount}"))
            .When(x => x.TextName is null);

        RuleFor(x => x)
            .Must(input => (input.ProductId.HasValue ? 1 : 0) + (input.NestedRecipeId.HasValue ? 1 : 0) + (input.TextName is not null ? 1 : 0) == 1)
            .WithMessage("Ingredient must have exactly one product, recipe or text name");
        RuleFor(x => x.TextName).NotEmpty().MaximumLength(RecipeIngredient.TextNameMaxLength).When(x => x.TextName is not null);
        RuleFor(x => x.AmountText).MaximumLength(RecipeIngredient.AmountTextMaxLength);
        RuleFor(x => x.AmountText).Null().When(x => x.TextName is null);
        RuleFor(x => x.Amount).Equal(0).When(x => x.TextName is not null);
    }
}
