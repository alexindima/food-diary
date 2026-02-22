using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Events;

public sealed record ShoppingListItemAddedDomainEvent(
    ShoppingListId ShoppingListId,
    ShoppingListItemId ShoppingListItemId,
    ProductId? ProductId,
    string Name,
    double? Amount,
    MeasurementUnit? Unit,
    string? Category,
    bool IsChecked,
    int SortOrder) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
