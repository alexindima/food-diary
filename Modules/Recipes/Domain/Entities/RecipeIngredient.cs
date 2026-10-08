using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;
using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Recipes.Domain.Entities;

public sealed class RecipeIngredient : Entity<RecipeIngredientId> {
    public const int TextNameMaxLength = 256;
    public const int AmountTextMaxLength = 128;
    // Author-published description, independent of the linked product or nested recipe's visibility.
    public string? PublicName { get; private set; }
    public string? PublicUnit { get; private set; }

    public void SetPublicDescription(string? name, string? unit) {
        if (name?.Length > TextNameMaxLength || unit?.Length > AmountTextMaxLength) {
            throw new ArgumentException("Public ingredient description exceeds the length limit.", nameof(name));
        }
        PublicName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        PublicUnit = PublicName is null || string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
    }

    public string? TextName { get; private set; }
    public string? AmountText { get; private set; }

    public const double MaxAmount = 1_000_000d;
    private const double ComparisonEpsilon = 0.000001d;

    public RecipeStepId RecipeStepId { get; private set; }
    public ProductId? ProductId { get; private set; }
    public RecipeId? NestedRecipeId { get; private set; }
    public double Amount { get; private set; }
    public int Position { get; private set; }

    internal void SetPosition(int position) {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        Position = position;
    }

    public RecipeStep RecipeStep { get; private set; } = null!;
    public RecipeIngredientProductSnapshot? ProductSnapshot { get; private set; }

    public void SetProductSnapshot(RecipeIngredientProductSnapshot? snapshot) {
        if (snapshot is not null && snapshot.Id != ProductId) {
            throw new ArgumentException("Product snapshot must match the ingredient product.", nameof(snapshot));
        }
        ProductSnapshot = snapshot;
    }
    public Recipe? NestedRecipe { get; private set; }

    private RecipeIngredient() {
    }

    internal static RecipeIngredient CreateWithProduct(RecipeStepId recipeStepId, ProductId productId, ProductUnitQuantity amount) {
        EnsureRecipeStepId(recipeStepId);
        EnsureProductId(productId);
        ArgumentNullException.ThrowIfNull(amount);

        var ingredient = new RecipeIngredient {
            Id = RecipeIngredientId.New(),
            RecipeStepId = recipeStepId,
            ProductId = productId,
            NestedRecipeId = null,
            Amount = amount.Value,
        };
        ingredient.SetCreated();
        return ingredient;
    }

    internal static RecipeIngredient CreateWithRecipe(RecipeStepId recipeStepId, RecipeId nestedRecipeId, RecipeServingQuantity servings) {
        EnsureRecipeStepId(recipeStepId);
        EnsureRecipeId(nestedRecipeId);
        ArgumentNullException.ThrowIfNull(servings);

        var ingredient = new RecipeIngredient {
            Id = RecipeIngredientId.New(),
            RecipeStepId = recipeStepId,
            ProductId = null,
            NestedRecipeId = nestedRecipeId,
            Amount = servings.Value,
        };
        ingredient.SetCreated();
        return ingredient;
    }

    internal static RecipeIngredient CreateWithText(RecipeStepId recipeStepId, string name, string? amountText) {
        EnsureRecipeStepId(recipeStepId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string normalizedName = name.Trim();
        string? normalizedAmount = string.IsNullOrWhiteSpace(amountText) ? null : amountText.Trim();
        if (normalizedName.Length > TextNameMaxLength || normalizedAmount?.Length > AmountTextMaxLength) {
            throw new ArgumentException("Text ingredient exceeds the length limit.", nameof(name));
        }
        var ingredient = new RecipeIngredient {
            Id = RecipeIngredientId.New(),
            RecipeStepId = recipeStepId,
            TextName = normalizedName,
            AmountText = normalizedAmount,
        };
        ingredient.SetCreated();
        return ingredient;
    }

    public void UpdateProductQuantity(ProductUnitQuantity amount) {
        ArgumentNullException.ThrowIfNull(amount);
        if (!ProductId.HasValue || NestedRecipeId.HasValue || TextName is not null) {
            throw new InvalidOperationException("Product quantity can be applied only to a product ingredient.");
        }
        ApplyAmount(amount.Value);
    }

    public void UpdateRecipeServings(RecipeServingQuantity servings) {
        ArgumentNullException.ThrowIfNull(servings);
        if (!NestedRecipeId.HasValue || ProductId.HasValue || TextName is not null) {
            throw new InvalidOperationException("Recipe servings can be applied only to a nested recipe ingredient.");
        }
        ApplyAmount(servings.Value);
    }

    private void ApplyAmount(double normalizedAmount) {
        if (Math.Abs(Amount - normalizedAmount) <= ComparisonEpsilon) {
            return;
        }

        Amount = normalizedAmount;
        SetModified();
    }

    private static void EnsureRecipeStepId(RecipeStepId recipeStepId) {
        if (recipeStepId == RecipeStepId.Empty) {
            throw new ArgumentException("RecipeStepId is required.", nameof(recipeStepId));
        }
    }

    private static void EnsureProductId(ProductId productId) {
        if (productId == global::FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids.ProductId.Empty) {
            throw new ArgumentException("ProductId is required.", nameof(productId));
        }
    }

    private static void EnsureRecipeId(RecipeId recipeId) {
        if (recipeId == RecipeId.Empty) {
            throw new ArgumentException("NestedRecipeId is required.", nameof(recipeId));
        }
    }
}
