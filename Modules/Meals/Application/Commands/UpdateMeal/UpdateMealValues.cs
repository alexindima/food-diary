using FoodDiary.Application.Abstractions.Images.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Commands.UpdateMeal;

internal sealed record UpdateMealValues(
    UserId UserId,
    MealId MealId,
    Meal Meal,
    MealType? MealType,
    ImageAssetId? ImageAssetId,
    ImageAssetReadModel? ImageAsset,
    ImageAssetId? OldAssetId);
