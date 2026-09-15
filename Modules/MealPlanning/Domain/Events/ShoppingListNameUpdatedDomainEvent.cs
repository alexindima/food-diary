using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.MealPlanning.Domain.Events;

public sealed record ShoppingListNameUpdatedDomainEvent : IDomainEvent {
    public ShoppingListNameUpdatedDomainEvent(
        ShoppingListId shoppingListId,
        string previousName,
        string currentName,
        DateTime? occurredOnUtcOverride = null) {
        ShoppingListId = shoppingListId;
        PreviousName = previousName;
        CurrentName = currentName;
        OccurredOnUtc = DomainTime.EnsureUtc(occurredOnUtcOverride ?? DomainTime.UtcNow, nameof(occurredOnUtcOverride));
    }

    public ShoppingListId ShoppingListId { get; }
    public string PreviousName { get; }
    public string CurrentName { get; }
    public DateTime OccurredOnUtc { get; }
}
