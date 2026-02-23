using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Shopping;

public sealed class ShoppingList : AggregateRoot<ShoppingListId> {
    private const int NameMaxLength = 128;
    private readonly List<ShoppingListItem> _items = [];

    public UserId UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public User User { get; private set; } = null!;
    public IReadOnlyCollection<ShoppingListItem> Items => _items.AsReadOnly();

    private ShoppingList() {
    }

    public static ShoppingList Create(UserId userId, string name) {
        EnsureUserId(userId);

        var normalizedName = NormalizeRequiredName(name);
        var list = new ShoppingList {
            Id = ShoppingListId.New(),
            UserId = userId,
            Name = normalizedName
        };
        list.SetCreated();
        return list;
    }

    public void UpdateName(string name) {
        var normalizedName = NormalizeRequiredName(name);
        if (Name == normalizedName) {
            return;
        }

        var previousName = Name;
        Name = normalizedName;
        SetModified();
        RaiseDomainEvent(new ShoppingListNameUpdatedDomainEvent(Id, previousName, Name));
    }

    public void ClearItems() {
        if (_items.Count == 0) {
            return;
        }

        var clearedItemsCount = _items.Count;
        _items.Clear();
        SetModified();
        RaiseDomainEvent(new ShoppingListItemsClearedDomainEvent(Id, clearedItemsCount));
    }

    public ShoppingListItem AddItem(
        string name,
        ProductId? productId,
        double? amount,
        MeasurementUnit? unit,
        string? category,
        bool isChecked,
        int sortOrder) {
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
        RaiseDomainEvent(new ShoppingListItemAddedDomainEvent(
            Id,
            item.Id,
            item.ProductId,
            item.Name,
            item.Amount,
            item.Unit,
            item.Category,
            item.IsChecked,
            item.SortOrder));
        return item;
    }

    private static string NormalizeRequiredName(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Shopping list name is required.", nameof(value));
        }

        var normalized = value.Trim();
        return normalized.Length > NameMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Shopping list name must be at most {NameMaxLength} characters.")
            : normalized;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }
}
