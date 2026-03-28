using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record MealNutritionAppliedDomainEvent(
    MealId MealId,
    bool IsAutoCalculated,
    double TotalCalories,
    double TotalProteins,
    double TotalFats,
    double TotalCarbs,
    double TotalFiber,
    double TotalAlcohol,
    DateTime? OccurredOnUtcOverride = null) : IDomainEvent {
    public DateTime OccurredOnUtc { get; } = OccurredOnUtcOverride ?? Common.DomainTime.UtcNow;
}
