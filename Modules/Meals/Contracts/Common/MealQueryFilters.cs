using FoodDiary.Modules.Meals.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Meals.Contracts.Common;

public sealed record MealQueryFilters(
    DateTime? DateFrom,
    DateTime? DateTo,
    IReadOnlyCollection<MealType>? MealTypes = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null,
    bool? HasAiSession = null);
