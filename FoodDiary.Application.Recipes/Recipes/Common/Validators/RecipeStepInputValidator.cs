using FluentValidation;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Recipes.Recipes.Common.Validators;

internal sealed class RecipeStepInputValidator : AbstractValidator<RecipeStepInput> {
    public RecipeStepInputValidator() {
        RuleFor(x => x.Title)
            .MaximumLength(RecipeStepContentState.TitleMaxLength)
            .WithMessage($"Step title must be {RecipeStepContentState.TitleMaxLength} characters or less");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Step description is required")
            .MaximumLength(RecipeStepContentState.InstructionMaxLength)
            .WithMessage($"Step description must be {RecipeStepContentState.InstructionMaxLength} characters or less");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(RecipeStepContentState.ImageUrlMaxLength);

        RuleFor(x => x.Ingredients)
            .NotNull()
            .WithMessage("Ingredients collection is required")
            .Must(ingredients => ingredients.Count > 0)
            .WithMessage("Each step must contain at least one ingredient");

        RuleForEach(x => x.Ingredients)
            .SetValidator(new RecipeIngredientInputValidator());
    }
}
