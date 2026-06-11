using System.Globalization;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Shopping;

public sealed class ShoppingListItem : Entity<ShoppingListItemId> {
    private const int NameMaxLength = 256;
    private const int CategoryMaxLength = 128;
    private const int NoteMaxLength = 512;
    private const double MaxAmount = 1_000_000d;
    private readonly List<ShoppingListItemSource> _sources = [];

    public ShoppingListId ShoppingListId { get; private set; }
    public ProductId? ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public double? Amount { get; private set; }
    public MeasurementUnit? Unit { get; private set; }
    public string? Category { get; private set; }
    public string? Aisle { get; private set; }
    public string? Note { get; private set; }
    public bool IsChecked { get; private set; }
    public DateTime? CheckedOnUtc { get; private set; }
    public int SortOrder { get; private set; }

    public ShoppingList ShoppingList { get; private set; } = null!;
    public Product? Product { get; private set; }
    public IReadOnlyCollection<ShoppingListItemSource> Sources => _sources.AsReadOnly();

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
        int sortOrder,
        string? aisle = null,
        string? note = null,
        DateTime? checkedOnUtc = null,
        ShoppingListItemId? id = null) {
        EnsureShoppingListId(shoppingListId);
        EnsureProductId(productId);
        EnsureItemId(id);
        string normalizedName = NormalizeRequiredName(name);
        double? normalizedAmount = NormalizeOptionalAmount(amount, nameof(amount));
        string? normalizedCategory = NormalizeOptionalText(category, CategoryMaxLength, nameof(category));
        string? normalizedAisle = NormalizeOptionalText(aisle, CategoryMaxLength, nameof(aisle));
        string? normalizedNote = NormalizeOptionalText(note, NoteMaxLength, nameof(note));

        if (sortOrder < 0) {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order must be non-negative.");
        }

        var item = new ShoppingListItem {
            Id = id ?? ShoppingListItemId.New(),
            ShoppingListId = shoppingListId,
            ProductId = productId,
            Name = normalizedName,
            Amount = normalizedAmount,
            Unit = unit,
            Category = normalizedCategory,
            Aisle = normalizedAisle,
            Note = normalizedNote,
            IsChecked = isChecked,
            CheckedOnUtc = isChecked ? NormalizeCheckedOnUtc(checkedOnUtc) : null,
            SortOrder = sortOrder,
        };
        item.SetCreated();
        return item;
    }

    public void UpdateDetails(
        string name,
        ProductId? productId,
        double? amount,
        MeasurementUnit? unit,
        string? category,
        string? aisle,
        string? note,
        bool isChecked,
        DateTime? checkedOnUtc,
        int sortOrder) {
        EnsureProductId(productId);
        ProductId = productId;
        Name = NormalizeRequiredName(name);
        Amount = NormalizeOptionalAmount(amount, nameof(amount));
        Unit = unit;
        Category = NormalizeOptionalText(category, CategoryMaxLength, nameof(category));
        Aisle = NormalizeOptionalText(aisle, CategoryMaxLength, nameof(aisle));
        Note = NormalizeOptionalText(note, NoteMaxLength, nameof(note));
        IsChecked = isChecked;
        CheckedOnUtc = isChecked ? NormalizeCheckedOnUtc(checkedOnUtc) : null;

        if (sortOrder < 0) {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order must be non-negative.");
        }

        SortOrder = sortOrder;
        SetModified();
    }

    public ShoppingListItemSource AddMealPlanSource(
        MealPlanId mealPlanId,
        MealPlanMealId mealPlanMealId,
        RecipeId recipeId,
        string label,
        int dayNumber,
        string mealType,
        double amount,
        MeasurementUnit? unit) {
        var source = ShoppingListItemSource.CreateMealPlanSource(
            Id,
            mealPlanId,
            mealPlanMealId,
            recipeId,
            label,
            dayNumber,
            mealType,
            amount,
            unit);
        _sources.Add(source);
        SetModified();
        return source;
    }

    private static string NormalizeRequiredName(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Name is required.", nameof(value));
        }

        string normalized = value.Trim();
        return normalized.Length > NameMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Name must be at most {NameMaxLength} characters.")
            : normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, string.Create(CultureInfo.InvariantCulture, $"Value must be at most {maxLength} characters."))
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
            ? throw new ArgumentOutOfRangeException(paramName, string.Create(CultureInfo.InvariantCulture, $"Amount must be in range (0, {MaxAmount}]."))
            : value.Value;
    }

    private static DateTime? NormalizeCheckedOnUtc(DateTime? value) =>
        value?.ToUniversalTime() ?? DomainTime.UtcNow;

    private static void EnsureShoppingListId(ShoppingListId shoppingListId) {
        if (shoppingListId == ShoppingListId.Empty) {
            throw new ArgumentException("ShoppingListId is required.", nameof(shoppingListId));
        }
    }

    private static void EnsureProductId(ProductId? productId) {
        if (productId == global::FoodDiary.Domain.ValueObjects.Ids.ProductId.Empty) {
            throw new ArgumentException("ProductId cannot be empty.", nameof(productId));
        }
    }

    private static void EnsureItemId(ShoppingListItemId? id) {
        if (id == ShoppingListItemId.Empty) {
            throw new ArgumentException("ShoppingListItemId cannot be empty.", nameof(id));
        }
    }
}
