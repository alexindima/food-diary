using FluentValidation;

namespace FoodDiary.Application.FavoriteRecipes.Commands.AddFavoriteRecipe;

public sealed class AddFavoriteRecipeCommandValidator : AbstractValidator<AddFavoriteRecipeCommand> {
    public AddFavoriteRecipeCommandValidator() {
        RuleFor(x => x.RecipeId)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Recipe id must not be empty.");
    }
}
