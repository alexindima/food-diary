using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record ShoppingListItemsClearedDomainEvent(
    ShoppingListId ShoppingListId,
    int ClearedItemsCount) : IDomainEvent {
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
