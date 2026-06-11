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
        string? aisle,
        string? note,
        bool isChecked,
        DateTime? checkedOnUtc,
        int sortOrder,
        DateTime? occurredOnUtcOverride = null) {
        ShoppingListId = shoppingListId;
        ShoppingListItemId = shoppingListItemId;
        ProductId = productId;
        Name = name;
        Amount = amount;
        Unit = unit;
        Category = category;
        Aisle = aisle;
        Note = note;
        IsChecked = isChecked;
        CheckedOnUtc = checkedOnUtc;
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
    public string? Aisle { get; }
    public string? Note { get; }
    public bool IsChecked { get; }
    public DateTime? CheckedOnUtc { get; }
    public int SortOrder { get; }
    public DateTime OccurredOnUtc { get; }
}
