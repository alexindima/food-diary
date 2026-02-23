using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Meals;

public class MealItem : Entity<MealItemId> {
    private const double MaxAmount = 1_000_000d;
    private const double ComparisonEpsilon = 0.000001d;

    public MealId MealId { get; private set; }

    public ProductId? ProductId { get; private set; }
    public RecipeId? RecipeId { get; private set; }

    public double Amount { get; private set; }

    public virtual Meal Meal { get; private set; } = null!;
    public virtual Product? Product { get; private set; }
    public virtual Recipe? Recipe { get; private set; }

    private MealItem() { }

    internal static MealItem CreateWithProduct(MealId mealId, ProductId productId, double amount) {
        EnsureMealId(mealId);
        EnsureProductId(productId);
        var normalizedAmount = ValidateAmount(amount, nameof(amount));

        var item = new MealItem {
            Id = MealItemId.New(),
            MealId = mealId,
            ProductId = productId,
            RecipeId = null,
            Amount = normalizedAmount
        };
        item.SetCreated();
        return item;
    }

    internal static MealItem CreateWithRecipe(MealId mealId, RecipeId recipeId, double servings) {
        EnsureMealId(mealId);
        EnsureRecipeId(recipeId);
        var normalizedServings = ValidateAmount(servings, nameof(servings));

        var item = new MealItem {
            Id = MealItemId.New(),
            MealId = mealId,
            ProductId = null,
            RecipeId = recipeId,
            Amount = normalizedServings
        };
        item.SetCreated();
        return item;
    }

    public void UpdateAmount(double amount) {
        var normalizedAmount = ValidateAmount(amount, nameof(amount));
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

        if (amount <= 0 || amount > MaxAmount) {
            throw new ArgumentOutOfRangeException(paramName, $"Amount must be in range (0, {MaxAmount}].");
        }

        return amount;
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

