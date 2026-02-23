using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record RecipeAutoNutritionEnabledDomainEvent(RecipeId RecipeId) : IDomainEvent {
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
