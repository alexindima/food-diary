using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record RecipeManualNutritionSetDomainEvent : IDomainEvent {
    public RecipeManualNutritionSetDomainEvent(RecipeId recipeId, DateTime? occurredOnUtcOverride = null) {
        RecipeId = recipeId;
        OccurredOnUtc = occurredOnUtcOverride ?? DomainTime.UtcNow;
    }

    public RecipeId RecipeId { get; }
    public DateTime OccurredOnUtc { get; }
}
