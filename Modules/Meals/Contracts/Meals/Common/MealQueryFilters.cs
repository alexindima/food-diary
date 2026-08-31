using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public sealed record MealQueryFilters(
    DateTime? DateFrom,
    DateTime? DateTo,
    IReadOnlyCollection<MealType>? MealTypes = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null,
    bool? HasAiSession = null);
