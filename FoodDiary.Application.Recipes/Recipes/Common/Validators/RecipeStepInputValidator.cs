using FluentValidation;

namespace FoodDiary.Application.Recipes.Common.Validators;

internal sealed class RecipeStepInputValidator : AbstractValidator<RecipeStepInput> {
    public RecipeStepInputValidator() {
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
