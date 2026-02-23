using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record ShoppingListNameUpdatedDomainEvent(
    ShoppingListId ShoppingListId,
    string PreviousName,
    string CurrentName) : IDomainEvent {
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
