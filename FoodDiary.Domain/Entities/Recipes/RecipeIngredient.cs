using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Recipes;

/// <summary>
/// Ð˜Ð½Ð³Ñ€ÐµÐ´Ð¸ÐµÐ½Ñ‚ Ð²Ð½ÑƒÑ‚Ñ€Ð¸ ÑˆÐ°Ð³Ð° Ñ€ÐµÑ†ÐµÐ¿Ñ‚Ð°
/// </summary>
public sealed class RecipeIngredient : Entity<RecipeIngredientId>
{
    public RecipeStepId RecipeStepId { get; private set; }
    public ProductId? ProductId { get; private set; }
    public RecipeId? NestedRecipeId { get; private set; }
    public double Amount { get; private set; }

    public RecipeStep RecipeStep { get; private set; } = null!;
    public Product? Product { get; private set; }
    public Recipe? NestedRecipe { get; private set; }

    private RecipeIngredient() { }

    internal static RecipeIngredient CreateWithProduct(RecipeStepId recipeStepId, ProductId productId, double amount)
    {
        ValidateAmount(amount);

        var ingredient = new RecipeIngredient
        {
            Id = RecipeIngredientId.New(),
            RecipeStepId = recipeStepId,
            ProductId = productId,
            NestedRecipeId = null,
            Amount = amount
        };
        ingredient.SetCreated();
        return ingredient;
    }

    internal static RecipeIngredient CreateWithRecipe(RecipeStepId recipeStepId, RecipeId nestedRecipeId, double servings)
    {
        ValidateAmount(servings);

        var ingredient = new RecipeIngredient
        {
            Id = RecipeIngredientId.New(),
            RecipeStepId = recipeStepId,
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
        {
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));
        }
    }
}

