using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record RecipeAutoNutritionEnabledDomainEvent : IDomainEvent {
    public RecipeAutoNutritionEnabledDomainEvent(RecipeId recipeId, DateTime? occurredOnUtcOverride = null) {
        RecipeId = recipeId;
        OccurredOnUtc = occurredOnUtcOverride ?? DomainTime.UtcNow;
    }

    public RecipeId RecipeId { get; }
    public DateTime OccurredOnUtc { get; }
}
