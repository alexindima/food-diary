using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Application.Commands.UpdateRecipe;

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
