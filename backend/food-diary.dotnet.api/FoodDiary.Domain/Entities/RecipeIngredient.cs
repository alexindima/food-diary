using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Ингредиент рецепта - часть агрегата Recipe
/// НЕ является корнем агрегата
/// Может быть либо Product (базовый продукт), либо другой Recipe (вложенное блюдо)
/// </summary>
public class RecipeIngredient : Entity<int>
{
    public int RecipeId { get; private set; }

    // XOR: либо ProductId, либо NestedRecipeId (для рекурсивных рецептов)
    public ProductId? ProductId { get; private set; }
    public int? NestedRecipeId { get; private set; }

    public double Amount { get; private set; }

    // Navigation properties
    public virtual Recipe Recipe { get; private set; } = null!;
    public virtual Product? Product { get; private set; }
    public virtual Recipe? NestedRecipe { get; private set; }

    // Конструктор для EF Core
    private RecipeIngredient() { }

    // Factory method для добавления продукта
    internal static RecipeIngredient CreateWithProduct(int recipeId, ProductId productId, double amount)
    {
        ValidateAmount(amount);

        var ingredient = new RecipeIngredient
        {
            RecipeId = recipeId,
            ProductId = productId,
            NestedRecipeId = null,
            Amount = amount
        };
        ingredient.SetCreated();
        return ingredient;
    }

    // Factory method для добавления вложенного рецепта (блюда внутри блюда)
    internal static RecipeIngredient CreateWithRecipe(int recipeId, int nestedRecipeId, double servings)
    {
        ValidateAmount(servings);

        var ingredient = new RecipeIngredient
        {
            RecipeId = recipeId,
            ProductId = null,
            NestedRecipeId = nestedRecipeId,
            Amount = servings
        };
        ingredient.SetCreated();
        return ingredient;
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
    /// Проверяет, является ли этот ингредиент продуктом
    /// </summary>
    public bool IsProduct => ProductId.HasValue;

    /// <summary>
    /// Проверяет, является ли этот ингредиент вложенным рецептом
    /// </summary>
    public bool IsNestedRecipe => NestedRecipeId.HasValue;
}

