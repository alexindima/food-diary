using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

public sealed class ShoppingListItem : Entity<ShoppingListItemId>
{
    public ShoppingListId ShoppingListId { get; private set; }
    public ProductId? ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public double? Amount { get; private set; }
    public MeasurementUnit? Unit { get; private set; }
    public string? Category { get; private set; }
    public bool IsChecked { get; private set; }
    public int SortOrder { get; private set; }

    public ShoppingList ShoppingList { get; private set; } = null!;
    public Product? Product { get; private set; }

    private ShoppingListItem()
    {
    }

    public static ShoppingListItem Create(
        ShoppingListId shoppingListId,
        string name,
        ProductId? productId,
        double? amount,
        MeasurementUnit? unit,
        string? category,
        bool isChecked,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (amount.HasValue && amount.Value <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        }

        var item = new ShoppingListItem
        {
            Id = ShoppingListItemId.New(),
            ShoppingListId = shoppingListId,
            ProductId = productId,
            Name = name.Trim(),
            Amount = amount,
            Unit = unit,
            Category = category?.Trim(),
            IsChecked = isChecked,
            SortOrder = sortOrder
        };
        item.SetCreated();
        return item;
    }
}
