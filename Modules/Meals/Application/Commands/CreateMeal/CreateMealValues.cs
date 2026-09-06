using FoodDiary.Application.Abstractions.Images.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Commands.CreateMeal;

internal sealed record CreateMealValues(
    UserId UserId,
    MealType? MealType,
    ImageAssetId? ImageAssetId,
    ImageAssetReadModel? ImageAsset);
