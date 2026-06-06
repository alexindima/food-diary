using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Events;

public sealed record ShoppingListItemAddedDomainEvent : IDomainEvent {
    public ShoppingListItemAddedDomainEvent(
        ShoppingListId shoppingListId,
        ShoppingListItemId shoppingListItemId,
        ProductId? productId,
        string name,
        double? amount,
        MeasurementUnit? unit,
        string? category,
        bool isChecked,
        int sortOrder,
        DateTime? occurredOnUtcOverride = null) {
        ShoppingListId = shoppingListId;
        ShoppingListItemId = shoppingListItemId;
        ProductId = productId;
        Name = name;
        Amount = amount;
        Unit = unit;
        Category = category;
        IsChecked = isChecked;
        SortOrder = sortOrder;
        OccurredOnUtc = occurredOnUtcOverride ?? DomainTime.UtcNow;
    }

    public ShoppingListId ShoppingListId { get; }
    public ShoppingListItemId ShoppingListItemId { get; }
    public ProductId? ProductId { get; }
    public string Name { get; }
    public double? Amount { get; }
    public MeasurementUnit? Unit { get; }
    public string? Category { get; }
    public bool IsChecked { get; }
    public int SortOrder { get; }
    public DateTime OccurredOnUtc { get; }
}
