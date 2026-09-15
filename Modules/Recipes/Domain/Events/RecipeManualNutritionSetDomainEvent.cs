using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Recipes.Domain.Events;

public sealed record RecipeManualNutritionSetDomainEvent : IDomainEvent {
    public RecipeManualNutritionSetDomainEvent(RecipeId recipeId, DateTime? occurredOnUtcOverride = null) {
        RecipeId = recipeId;
        OccurredOnUtc = DomainTime.EnsureUtc(occurredOnUtcOverride ?? DomainTime.UtcNow, nameof(occurredOnUtcOverride));
    }

    public RecipeId RecipeId { get; }
    public DateTime OccurredOnUtc { get; }
}
