using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Recipes.Commands.DeleteRecipe;

public class DeleteRecipeCommandValidator : AbstractValidator<DeleteRecipeCommand>
{
    private readonly IRecipeRepository _recipeRepository;

    public DeleteRecipeCommandValidator(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;

        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.RecipeId)
            .Must(id => id != RecipeId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("RecipeId is required");

        RuleFor(x => x)
            .CustomAsync(EnsureRecipeDeletableAsync);
    }

    private async Task EnsureRecipeDeletableAsync(
        DeleteRecipeCommand command,
        ValidationContext<DeleteRecipeCommand> context,
        CancellationToken cancellationToken)
    {
        if (command.UserId is null || command.UserId.Value == UserId.Empty)
        {
            return;
        }

        var recipe = await _recipeRepository.GetByIdAsync(
            command.RecipeId,
            command.UserId.Value,
            includePublic: false,
            includeSteps: false,
            cancellationToken: cancellationToken);

        if (recipe is null)
        {
            context.AddFailure(new ValidationFailure(nameof(command.RecipeId),
                "Recipe not found or you do not have permission to delete it")
            {
                ErrorCode = "Recipe.NotFound"
            });
            return;
        }

        var usageCount = recipe.MealItems.Count + recipe.NestedRecipeUsages.Count;
        if (usageCount > 0)
        {
            context.AddFailure(new ValidationFailure(nameof(command.RecipeId),
                "Recipe is already used and cannot be deleted")
            {
                ErrorCode = "Validation.Invalid"
            });
        }
    }
}
