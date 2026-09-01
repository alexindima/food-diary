using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record RecipeAutoNutritionEnabledDomainEvent : IDomainEvent {
    public RecipeAutoNutritionEnabledDomainEvent(RecipeId recipeId, DateTime? occurredOnUtcOverride = null) {
        RecipeId = recipeId;
        OccurredOnUtc = DomainTime.EnsureUtc(occurredOnUtcOverride ?? DomainTime.UtcNow, nameof(occurredOnUtcOverride));
    }

    public RecipeId RecipeId { get; }
    public DateTime OccurredOnUtc { get; }
}
