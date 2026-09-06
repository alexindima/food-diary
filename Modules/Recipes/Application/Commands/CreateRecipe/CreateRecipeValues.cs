using FoodDiary.Domain.Primitives;
using FoodDiary.Application.Abstractions.Images.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.CreateRecipe;

internal sealed record CreateRecipeValues(
    UserId UserId,
    Visibility Visibility,
    ImageAssetId? ImageAssetId,
    ImageAssetReadModel? ImageAsset);
