using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Application.Commands.UpdateRecipe;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Services;

internal sealed class RecipePublicationApplier(UpdateRecipeCommandHandler updateRecipe)
    : IRecipePublicationApplier {
    public async Task<Result> ApplyAsync(
        RecipeId recipeId,
        UserId ownerId,
        RecipePublicationClaim claim,
        string changeJson,
        CancellationToken cancellationToken) {
        var draft = RecipePublicationDraft.Read(changeJson);
        if (draft is null || draft.Change.RecipeId != recipeId.Value || draft.Change.UserId != ownerId.Value) {
            return Result.Failure(RecipeErrors.InvalidData("The proposed recipe does not match the publication."));
        }

        Result<RecipeModel> updated = await updateRecipe.Handle(
            draft.Change with { ApprovedPublication = claim }, cancellationToken);
        return updated.IsSuccess ? Result.Success() : Result.Failure(updated.Error);
    }
}
