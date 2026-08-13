using FluentValidation;

namespace FoodDiary.Application.Recipes.Commands.DeleteRecipe;

public sealed class DeleteRecipeCommandValidator : AbstractValidator<DeleteRecipeCommand> {
    public DeleteRecipeCommandValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.RecipeId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("RecipeId is required");
    }
}
