using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Shopping;

public sealed class ShoppingList : AggregateRoot<ShoppingListId>
{
    private readonly List<ShoppingListItem> _items = new();

    public UserId UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public User User { get; private set; } = null!;
    public IReadOnlyCollection<ShoppingListItem> Items => _items.AsReadOnly();

    private ShoppingList()
    {
        _items = new List<ShoppingListItem>();
    }

    public static ShoppingList Create(UserId userId, string name)
    {
        var list = new ShoppingList
        {
            Id = ShoppingListId.New(),
            UserId = userId,
            Name = NormalizeRequiredName(name)
        };
        list.SetCreated();
        return list;
    }

    public void UpdateName(string name)
    {
        Name = NormalizeRequiredName(name);
        SetModified();
    }

    public void ClearItems()
    {
        _items.Clear();
        SetModified();
    }

    public ShoppingListItem AddItem(
        string name,
        ProductId? productId,
        double? amount,
        MeasurementUnit? unit,
        string? category,
        bool isChecked,
        int sortOrder)
    {
        var item = ShoppingListItem.Create(
            Id,
            name,
            productId,
            amount,
            unit,
            category,
            isChecked,
            sortOrder);
        _items.Add(item);
        SetModified();
        return item;
    }

    private static string NormalizeRequiredName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Shopping list name is required.", nameof(value));
        }

        return value.Trim();
    }
}

