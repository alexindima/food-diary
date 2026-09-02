using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.CreateRecipe;

internal sealed record CreateRecipeValues(
    UserId UserId,
    Visibility Visibility,
    ImageAssetId? ImageAssetId,
    ImageAsset? ImageAsset);
