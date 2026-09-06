using FoodDiary.Domain.Primitives;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Abstractions.Images.Models;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

internal sealed record UpdateRecipeValues(
    UserId UserId,
    RecipeId RecipeId,
    Recipe Recipe,
    Visibility? Visibility,
    ImageAssetId? ImageAssetId,
    ImageAssetReadModel? ImageAsset,
    ImageAssetId? OldAssetId,
    IReadOnlyList<ImageAssetId> OldStepAssetIds,
    IReadOnlyList<RecipeStepInput> Steps);
