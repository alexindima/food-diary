using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Commands.CreateMeal;

internal sealed record CreateMealValues(
    UserId UserId,
    MealType? MealType,
    ImageAssetId? ImageAssetId,
    ImageAssetReadModel? ImageAsset);
