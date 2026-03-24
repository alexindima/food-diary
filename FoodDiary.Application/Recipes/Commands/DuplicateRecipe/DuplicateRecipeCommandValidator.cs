using FluentValidation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.DuplicateRecipe;

public class DuplicateRecipeCommandValidator : AbstractValidator<DuplicateRecipeCommand> {
    public DuplicateRecipeCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.RecipeId)
            .Must(id => id != RecipeId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("RecipeId is required");
    }
}
