using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

internal sealed record UpdateRecipeValues(
    UserId UserId,
    RecipeId RecipeId,
    Recipe Recipe,
    Visibility? Visibility,
    ImageAssetId? ImageAssetId,
    ImageAsset? ImageAsset,
    ImageAssetId? OldAssetId,
    IReadOnlyList<ImageAssetId> OldStepAssetIds,
    IReadOnlyList<RecipeStepInput> Steps);
