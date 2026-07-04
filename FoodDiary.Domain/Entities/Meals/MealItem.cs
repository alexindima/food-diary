using System.Globalization;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Meals;

public sealed class MealItem : Entity<MealItemId> {
    private const double MaxAmount = 1_000_000d;
    private const double ComparisonEpsilon = 0.000001d;
    private const int SnapshotNameMaxLength = 256;
    private const int SnapshotUnitMaxLength = 32;
    private const int SnapshotImageUrlMaxLength = DomainConstants.ImageUrlMaxLength;

    public MealId MealId { get; private set; }

    public ProductId? ProductId { get; private set; }
    public RecipeId? RecipeId { get; private set; }
    public MealAiItemId? SourceAiItemId { get; private set; }
    public MealItemOrigin Origin { get; private set; } = MealItemOrigin.Manual;

    public double Amount { get; private set; }
    public string? SnapshotName { get; private set; }
    public string? SnapshotImageUrl { get; private set; }
    public string? SnapshotUnit { get; private set; }
    public double? SnapshotBaseAmount { get; private set; }
    public double? SnapshotCaloriesPerBase { get; private set; }
    public double? SnapshotProteinsPerBase { get; private set; }
    public double? SnapshotFatsPerBase { get; private set; }
    public double? SnapshotCarbsPerBase { get; private set; }
    public double? SnapshotFiberPerBase { get; private set; }
    public double? SnapshotAlcoholPerBase { get; private set; }

    public Meal Meal { get; private set; } = null!;
    public Product? Product { get; private set; }
    public Recipe? Recipe { get; private set; }

    private MealItem() { }

    internal static MealItem CreateWithProduct(MealId mealId, ProductId productId, double amount) {
        EnsureMealId(mealId);
        EnsureProductId(productId);
        double normalizedAmount = ValidateAmount(amount, nameof(amount));

        var item = new MealItem {
            Id = MealItemId.New(),
            MealId = mealId,
            ProductId = productId,
            RecipeId = null,
            Amount = normalizedAmount,
            Origin = MealItemOrigin.Manual,
        };
        item.SetCreated();
        return item;
    }

    internal static MealItem CreateWithRecipe(MealId mealId, RecipeId recipeId, double servings) {
        EnsureMealId(mealId);
        EnsureRecipeId(recipeId);
        double normalizedServings = ValidateAmount(servings, nameof(servings));

        var item = new MealItem {
            Id = MealItemId.New(),
            MealId = mealId,
            ProductId = null,
            RecipeId = recipeId,
            Amount = normalizedServings,
            Origin = MealItemOrigin.Manual,
        };
        item.SetCreated();
        return item;
    }

    public void ApplyProductSnapshot(Product product) {
        ArgumentNullException.ThrowIfNull(product);
        ApplySnapshot(
            product.Name,
            product.ImageUrl,
            product.BaseUnit.ToString(),
            product.BaseAmount,
            product.CaloriesPerBase,
            product.ProteinsPerBase,
            product.FatsPerBase,
            product.CarbsPerBase,
            product.FiberPerBase,
            product.AlcoholPerBase);
    }

    public void ApplyRecipeSnapshot(Recipe recipe) {
        ArgumentNullException.ThrowIfNull(recipe);
        int servings = recipe.Servings <= 0 ? 1 : recipe.Servings;
        ApplySnapshot(
            recipe.Name,
            recipe.ImageUrl,
            "serving",
            1,
            (recipe.TotalCalories ?? 0) / servings,
            (recipe.TotalProteins ?? 0) / servings,
            (recipe.TotalFats ?? 0) / servings,
            (recipe.TotalCarbs ?? 0) / servings,
            (recipe.TotalFiber ?? 0) / servings,
            (recipe.TotalAlcohol ?? 0) / servings);
    }

    public void UpdateAmount(double amount) {
        double normalizedAmount = ValidateAmount(amount, nameof(amount));
        if (Math.Abs(Amount - normalizedAmount) <= ComparisonEpsilon) {
            return;
        }

        Amount = normalizedAmount;
        SetModified();
    }

    public void ApplySource(MealAiItemId? sourceAiItemId, MealItemOrigin origin) {
        MealItemOrigin normalizedOrigin = NormalizeOrigin(origin);
        MealAiItemId? normalizedSourceAiItemId = NormalizeSourceAiItemId(sourceAiItemId);
        if (normalizedSourceAiItemId.HasValue && normalizedOrigin == MealItemOrigin.Manual) {
            throw new ArgumentException("AI source item cannot be attached to a manual meal item.", nameof(origin));
        }

        if (SourceAiItemId == normalizedSourceAiItemId && Origin == normalizedOrigin) {
            return;
        }

        SourceAiItemId = normalizedSourceAiItemId;
        Origin = normalizedOrigin;
        SetModified();
    }

    public void CopySourceAndSnapshotFrom(MealItem source) {
        ArgumentNullException.ThrowIfNull(source);

        SourceAiItemId = source.SourceAiItemId;
        Origin = source.Origin;
        SnapshotName = source.SnapshotName;
        SnapshotImageUrl = source.SnapshotImageUrl;
        SnapshotUnit = source.SnapshotUnit;
        SnapshotBaseAmount = source.SnapshotBaseAmount;
        SnapshotCaloriesPerBase = source.SnapshotCaloriesPerBase;
        SnapshotProteinsPerBase = source.SnapshotProteinsPerBase;
        SnapshotFatsPerBase = source.SnapshotFatsPerBase;
        SnapshotCarbsPerBase = source.SnapshotCarbsPerBase;
        SnapshotFiberPerBase = source.SnapshotFiberPerBase;
        SnapshotAlcoholPerBase = source.SnapshotAlcoholPerBase;
        SetModified();
    }

    public bool HasNutritionSnapshot =>
        SnapshotBaseAmount.HasValue
        && SnapshotCaloriesPerBase.HasValue
        && SnapshotProteinsPerBase.HasValue
        && SnapshotFatsPerBase.HasValue
        && SnapshotCarbsPerBase.HasValue
        && SnapshotFiberPerBase.HasValue
        && SnapshotAlcoholPerBase.HasValue;

    private void ApplySnapshot(
        string name,
        string? imageUrl,
        string unit,
        double baseAmount,
        double caloriesPerBase,
        double proteinsPerBase,
        double fatsPerBase,
        double carbsPerBase,
        double fiberPerBase,
        double alcoholPerBase) {
        SnapshotName = NormalizeOptionalText(name, SnapshotNameMaxLength, nameof(name));
        SnapshotImageUrl = NormalizeOptionalText(imageUrl, SnapshotImageUrlMaxLength, nameof(imageUrl));
        SnapshotUnit = NormalizeOptionalText(unit, SnapshotUnitMaxLength, nameof(unit));
        SnapshotBaseAmount = ValidateAmount(baseAmount, nameof(baseAmount));
        SnapshotCaloriesPerBase = ValidateNonNegative(caloriesPerBase, nameof(caloriesPerBase));
        SnapshotProteinsPerBase = ValidateNonNegative(proteinsPerBase, nameof(proteinsPerBase));
        SnapshotFatsPerBase = ValidateNonNegative(fatsPerBase, nameof(fatsPerBase));
        SnapshotCarbsPerBase = ValidateNonNegative(carbsPerBase, nameof(carbsPerBase));
        SnapshotFiberPerBase = ValidateNonNegative(fiberPerBase, nameof(fiberPerBase));
        SnapshotAlcoholPerBase = ValidateNonNegative(alcoholPerBase, nameof(alcoholPerBase));
        SetModified();
    }

    private static double ValidateAmount(double amount, string paramName) {
        if (double.IsNaN(amount) || double.IsInfinity(amount)) {
            throw new ArgumentOutOfRangeException(paramName, "Amount must be a finite number.");
        }

        if (amount is <= 0 or > MaxAmount) {
            throw new ArgumentOutOfRangeException(paramName, string.Create(CultureInfo.InvariantCulture, $"Amount must be in range (0, {MaxAmount}]."));
        }

        return amount;
    }

    private static MealItemOrigin NormalizeOrigin(MealItemOrigin origin) {
        return Enum.IsDefined(origin)
            ? origin
            : throw new ArgumentOutOfRangeException(nameof(origin), "Unknown meal item origin.");
    }

    private static MealAiItemId? NormalizeSourceAiItemId(MealAiItemId? sourceAiItemId) {
        return sourceAiItemId == MealAiItemId.Empty
            ? throw new ArgumentException("Source AI item id must not be empty.", nameof(sourceAiItemId))
            : sourceAiItemId;
    }

    private static double ValidateNonNegative(double value, string paramName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be a finite number.");
        }

        return value < 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.")
            : value;
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

    private static void EnsureMealId(MealId mealId) {
        if (mealId == MealId.Empty) {
            throw new ArgumentException("MealId is required.", nameof(mealId));
        }
    }

    private static void EnsureProductId(ProductId productId) {
        if (productId == global::FoodDiary.Domain.ValueObjects.Ids.ProductId.Empty) {
            throw new ArgumentException("ProductId is required.", nameof(productId));
        }
    }

    private static void EnsureRecipeId(RecipeId recipeId) {
        if (recipeId == global::FoodDiary.Domain.ValueObjects.Ids.RecipeId.Empty) {
            throw new ArgumentException("RecipeId is required.", nameof(recipeId));
        }
    }

    public bool IsProduct => ProductId.HasValue;

    public bool IsRecipe => RecipeId.HasValue;
}
