using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Commands.UpdateMeal;

internal sealed record UpdateMealValues(
    UserId UserId,
    MealId MealId,
    Meal Meal,
    MealType? MealType,
    ImageAssetId? ImageAssetId,
    ImageAssetReadModel? ImageAsset,
    ImageAssetId? OldAssetId);
