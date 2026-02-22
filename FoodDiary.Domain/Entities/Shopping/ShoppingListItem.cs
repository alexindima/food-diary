using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Shopping;

public sealed class ShoppingListItem : Entity<ShoppingListItemId> {
    private const int NameMaxLength = 256;
    private const int CategoryMaxLength = 128;
    private const double MaxAmount = 1_000_000d;

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

    private ShoppingListItem() {
    }

    public static ShoppingListItem Create(
        ShoppingListId shoppingListId,
        string name,
        ProductId? productId,
        double? amount,
        MeasurementUnit? unit,
        string? category,
        bool isChecked,
        int sortOrder) {
        EnsureShoppingListId(shoppingListId);
        EnsureProductId(productId);
        var normalizedName = NormalizeRequiredName(name);
        var normalizedAmount = NormalizeOptionalAmount(amount, nameof(amount));
        var normalizedCategory = NormalizeOptionalText(category, CategoryMaxLength, nameof(category));

        if (sortOrder < 0) {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order must be non-negative.");
        }

        var item = new ShoppingListItem {
            Id = ShoppingListItemId.New(),
            ShoppingListId = shoppingListId,
            ProductId = productId,
            Name = normalizedName,
            Amount = normalizedAmount,
            Unit = unit,
            Category = normalizedCategory,
            IsChecked = isChecked,
            SortOrder = sortOrder
        };
        item.SetCreated();
        return item;
    }

    private static string NormalizeRequiredName(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Name is required.", nameof(value));
        }

        var normalized = value.Trim();
        return normalized.Length > NameMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Name must be at most {NameMaxLength} characters.")
            : normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : normalized;
    }

    private static double? NormalizeOptionalAmount(double? value, string paramName) {
        if (!value.HasValue) {
            return null;
        }

        if (double.IsNaN(value.Value) || double.IsInfinity(value.Value)) {
            throw new ArgumentOutOfRangeException(paramName, "Amount must be a finite number.");
        }

        return value.Value is <= 0 or > MaxAmount
            ? throw new ArgumentOutOfRangeException(paramName, $"Amount must be in range (0, {MaxAmount}].")
            : value.Value;
    }

    private static void EnsureShoppingListId(ShoppingListId shoppingListId) {
        if (shoppingListId == ShoppingListId.Empty) {
            throw new ArgumentException("ShoppingListId is required.", nameof(shoppingListId));
        }
    }

    private static void EnsureProductId(ProductId? productId) {
        if (productId.HasValue && productId.Value == global::FoodDiary.Domain.ValueObjects.ProductId.Empty) {
            throw new ArgumentException("ProductId cannot be empty.", nameof(productId));
        }
    }
}
