using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.DeleteRecipe;

public sealed class DeleteRecipeCommandValidator : AbstractValidator<DeleteRecipeCommand> {
    private readonly IRecipeReadRepository _recipeRepository;

    public DeleteRecipeCommandValidator(IRecipeReadRepository recipeRepository) {
        _recipeRepository = recipeRepository;

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

        RuleFor(x => x)
            .CustomAsync(EnsureRecipeDeletableAsync);
    }

    private async Task EnsureRecipeDeletableAsync(
        DeleteRecipeCommand command,
        ValidationContext<DeleteRecipeCommand> context,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        Result<RecipeId> recipeIdResult = RequiredIdParser.Parse(
            command.RecipeId,
            nameof(command.RecipeId),
            "Recipe id must not be empty.",
            value => new RecipeId(value));
        if (userIdResult.IsFailure || recipeIdResult.IsFailure) {
            return;
        }

        Recipe? recipe = await _recipeRepository.GetByIdAsync(
            recipeIdResult.Value,
            userIdResult.Value,
            includePublic: false,
            includeSteps: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (recipe is null) {
            context.AddFailure(new ValidationFailure(nameof(command.RecipeId),
                "Recipe not found or you do not have permission to delete it") {
                ErrorCode = "Recipe.NotFound",
            });
        }
    }
}
