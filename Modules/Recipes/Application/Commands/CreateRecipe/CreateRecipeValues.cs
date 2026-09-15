using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Application.Commands.CreateRecipe;

internal sealed record CreateRecipeValues(
    UserId UserId,
    Visibility Visibility,
    ImageAssetId? ImageAssetId,
    ImageAssetReadModel? ImageAsset);
