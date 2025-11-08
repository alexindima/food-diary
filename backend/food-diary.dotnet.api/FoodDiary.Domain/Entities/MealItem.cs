using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Элемент приема пищи - часть агрегата Meal
/// НЕ является корнем агрегата
/// Может быть либо Product (простой продукт), либо Recipe (блюдо)
/// </summary>
public class MealItem : Entity<int>
{
    public int MealId { get; private set; }

    // XOR: либо ProductId, либо RecipeId
    public ProductId? ProductId { get; private set; }
    public int? RecipeId { get; private set; }

    public double Amount { get; private set; }

    // Navigation properties
    public virtual Meal Meal { get; private set; } = null!;
    public virtual Product? Product { get; private set; }
    public virtual Recipe? Recipe { get; private set; }

    // Конструктор для EF Core
    private MealItem() { }

    // Factory method для добавления продукта
    internal static MealItem CreateWithProduct(int mealId, ProductId productId, double amount)
    {
        ValidateAmount(amount);

        var item = new MealItem
        {
            MealId = mealId,
            ProductId = productId,
            RecipeId = null,
            Amount = amount
        };
        item.SetCreated();
        return item;
    }

    // Factory method для добавления рецепта (блюда)
    internal static MealItem CreateWithRecipe(int mealId, int recipeId, double servings)
    {
        ValidateAmount(servings);

        var item = new MealItem
        {
            MealId = mealId,
            ProductId = null,
            RecipeId = recipeId,
            Amount = servings
        };
        item.SetCreated();
        return item;
    }

    public void UpdateAmount(double amount)
    {
        ValidateAmount(amount);
        Amount = amount;
        SetModified();
    }

    private static void ValidateAmount(double amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));
    }

    /// <summary>
    /// Проверяет, является ли этот элемент продуктом
    /// </summary>
    public bool IsProduct => ProductId.HasValue;

    /// <summary>
    /// Проверяет, является ли этот элемент рецептом (блюдом)
    /// </summary>
    public bool IsRecipe => RecipeId.HasValue;
}
