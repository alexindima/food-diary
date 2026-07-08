using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record MealNutritionAppliedDomainEvent : IDomainEvent {
    public MealNutritionAppliedDomainEvent(
        MealId mealId,
        bool isAutoCalculated,
        double totalCalories,
        double totalProteins,
        double totalFats,
        double totalCarbs,
        double totalFiber,
        double totalAlcohol,
        DateTime? occurredOnUtcOverride = null) {
        MealId = mealId;
        IsAutoCalculated = isAutoCalculated;
        TotalCalories = totalCalories;
        TotalProteins = totalProteins;
        TotalFats = totalFats;
        TotalCarbs = totalCarbs;
        TotalFiber = totalFiber;
        TotalAlcohol = totalAlcohol;
        OccurredOnUtc = occurredOnUtcOverride ?? DomainTime.UtcNow;
    }

    public MealId MealId { get; }
    public bool IsAutoCalculated { get; }
    public double TotalCalories { get; }
    public double TotalProteins { get; }
    public double TotalFats { get; }
    public double TotalCarbs { get; }
    public double TotalFiber { get; }
    public double TotalAlcohol { get; }
    public DateTime OccurredOnUtc { get; }
}
