using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;

namespace FoodDiary.Application.Recipes.Commands.DeleteRecipe;

public class DeleteRecipeCommandHandler(IRecipeRepository recipeRepository)
    : ICommandHandler<DeleteRecipeCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteRecipeCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdAsync(
            command.RecipeId,
            command.UserId!.Value,
            includePublic: false,
            includeSteps: false,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (recipe is null)
        {
            return Result.Failure<bool>(Errors.Recipe.NotAccessible(command.RecipeId.Value));
        }

        if (recipe.MealItems.Count + recipe.NestedRecipeUsages.Count > 0)
        {
            return Result.Failure<bool>(Errors.Validation.Invalid("RecipeId",
                "Recipe is already used and cannot be deleted"));
        }

        await recipeRepository.DeleteAsync(recipe);
        return Result.Success(true);
    }
}
