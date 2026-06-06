using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record ShoppingListNameUpdatedDomainEvent : IDomainEvent {
    public ShoppingListNameUpdatedDomainEvent(
        ShoppingListId shoppingListId,
        string previousName,
        string currentName,
        DateTime? occurredOnUtcOverride = null) {
        ShoppingListId = shoppingListId;
        PreviousName = previousName;
        CurrentName = currentName;
        OccurredOnUtc = occurredOnUtcOverride ?? DomainTime.UtcNow;
    }

    public ShoppingListId ShoppingListId { get; }
    public string PreviousName { get; }
    public string CurrentName { get; }
    public DateTime OccurredOnUtc { get; }
}
