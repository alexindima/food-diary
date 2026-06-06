using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record ShoppingListItemsClearedDomainEvent : IDomainEvent {
    public ShoppingListItemsClearedDomainEvent(
        ShoppingListId shoppingListId,
        int clearedItemsCount,
        DateTime? occurredOnUtcOverride = null) {
        ShoppingListId = shoppingListId;
        ClearedItemsCount = clearedItemsCount;
        OccurredOnUtc = occurredOnUtcOverride ?? DomainTime.UtcNow;
    }

    public ShoppingListId ShoppingListId { get; }
    public int ClearedItemsCount { get; }
    public DateTime OccurredOnUtc { get; }
}
