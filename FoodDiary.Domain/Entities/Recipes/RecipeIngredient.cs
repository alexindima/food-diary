using System.Globalization;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Recipes;

public sealed class RecipeIngredient : Entity<RecipeIngredientId> {
    private const double MaxAmount = 1_000_000d;
    private const double ComparisonEpsilon = 0.000001d;

    public RecipeStepId RecipeStepId { get; private set; }
    public ProductId? ProductId { get; private set; }
    public RecipeId? NestedRecipeId { get; private set; }
    public double Amount { get; private set; }

    public RecipeStep RecipeStep { get; private set; } = null!;
    public Product? Product { get; private set; }
    public Recipe? NestedRecipe { get; private set; }

    private RecipeIngredient() {
    }

    internal static RecipeIngredient CreateWithProduct(RecipeStepId recipeStepId, ProductId productId, double amount) {
        EnsureRecipeStepId(recipeStepId);
        EnsureProductId(productId);
        double normalizedAmount = ValidateAmount(amount, nameof(amount));

        var ingredient = new RecipeIngredient {
            Id = RecipeIngredientId.New(),
            RecipeStepId = recipeStepId,
            ProductId = productId,
            NestedRecipeId = null,
            Amount = normalizedAmount,
        };
        ingredient.SetCreated();
        return ingredient;
    }

    internal static RecipeIngredient CreateWithRecipe(RecipeStepId recipeStepId, RecipeId nestedRecipeId, double servings) {
        EnsureRecipeStepId(recipeStepId);
        EnsureRecipeId(nestedRecipeId);
        double normalizedServings = ValidateAmount(servings, nameof(servings));

        var ingredient = new RecipeIngredient {
            Id = RecipeIngredientId.New(),
            RecipeStepId = recipeStepId,
            ProductId = null,
            NestedRecipeId = nestedRecipeId,
            Amount = normalizedServings,
        };
        ingredient.SetCreated();
        return ingredient;
    }

    public void UpdateAmount(double amount) {
        double normalizedAmount = ValidateAmount(amount, nameof(amount));
        if (Math.Abs(Amount - normalizedAmount) <= ComparisonEpsilon) {
            return;
        }

        Amount = normalizedAmount;
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

    private static void EnsureRecipeStepId(RecipeStepId recipeStepId) {
        if (recipeStepId == RecipeStepId.Empty) {
            throw new ArgumentException("RecipeStepId is required.", nameof(recipeStepId));
        }
    }

    private static void EnsureProductId(ProductId productId) {
        if (productId == global::FoodDiary.Domain.ValueObjects.Ids.ProductId.Empty) {
            throw new ArgumentException("ProductId is required.", nameof(productId));
        }
    }

    private static void EnsureRecipeId(RecipeId recipeId) {
        if (recipeId == RecipeId.Empty) {
            throw new ArgumentException("NestedRecipeId is required.", nameof(recipeId));
        }
    }
}
