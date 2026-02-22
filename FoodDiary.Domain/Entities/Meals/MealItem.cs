using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Meals;

/// <summary>
/// Ð­Ð»ÐµÐ¼ÐµÐ½Ñ‚ Ð¿Ñ€Ð¸ÐµÐ¼Ð° Ð¿Ð¸Ñ‰Ð¸ - Ñ‡Ð°ÑÑ‚ÑŒ Ð°Ð³Ñ€ÐµÐ³Ð°Ñ‚Ð° Meal
/// ÐÐ• ÑÐ²Ð»ÑÐµÑ‚ÑÑ ÐºÐ¾Ñ€Ð½ÐµÐ¼ Ð°Ð³Ñ€ÐµÐ³Ð°Ñ‚Ð°
/// ÐœÐ¾Ð¶ÐµÑ‚ Ð±Ñ‹Ñ‚ÑŒ Ð»Ð¸Ð±Ð¾ Product (Ð¿Ñ€Ð¾ÑÑ‚Ð¾Ð¹ Ð¿Ñ€Ð¾Ð´ÑƒÐºÑ‚), Ð»Ð¸Ð±Ð¾ Recipe (Ð±Ð»ÑŽÐ´Ð¾)
/// </summary>
public class MealItem : Entity<MealItemId>
{
    public MealId MealId { get; private set; }

    // XOR: Ð»Ð¸Ð±Ð¾ ProductId, Ð»Ð¸Ð±Ð¾ RecipeId
    public ProductId? ProductId { get; private set; }
    public RecipeId? RecipeId { get; private set; }

    public double Amount { get; private set; }

    // Navigation properties
    public virtual Meal Meal { get; private set; } = null!;
    public virtual Product? Product { get; private set; }
    public virtual Recipe? Recipe { get; private set; }

    // ÐšÐ¾Ð½ÑÑ‚Ñ€ÑƒÐºÑ‚Ð¾Ñ€ Ð´Ð»Ñ EF Core
    private MealItem() { }

    // Factory method Ð´Ð»Ñ Ð´Ð¾Ð±Ð°Ð²Ð»ÐµÐ½Ð¸Ñ Ð¿Ñ€Ð¾Ð´ÑƒÐºÑ‚Ð°
    internal static MealItem CreateWithProduct(MealId mealId, ProductId productId, double amount)
    {
        ValidateAmount(amount);

        var item = new MealItem
        {
            Id = MealItemId.New(),
            MealId = mealId,
            ProductId = productId,
            RecipeId = null,
            Amount = amount
        };
        item.SetCreated();
        return item;
    }

    // Factory method Ð´Ð»Ñ Ð´Ð¾Ð±Ð°Ð²Ð»ÐµÐ½Ð¸Ñ Ñ€ÐµÑ†ÐµÐ¿Ñ‚Ð° (Ð±Ð»ÑŽÐ´Ð°)
    internal static MealItem CreateWithRecipe(MealId mealId, RecipeId recipeId, double servings)
    {
        ValidateAmount(servings);

        var item = new MealItem
        {
            Id = MealItemId.New(),
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
    /// ÐŸÑ€Ð¾Ð²ÐµÑ€ÑÐµÑ‚, ÑÐ²Ð»ÑÐµÑ‚ÑÑ Ð»Ð¸ ÑÑ‚Ð¾Ñ‚ ÑÐ»ÐµÐ¼ÐµÐ½Ñ‚ Ð¿Ñ€Ð¾Ð´ÑƒÐºÑ‚Ð¾Ð¼
    /// </summary>
    public bool IsProduct => ProductId.HasValue;

    /// <summary>
    /// ÐŸÑ€Ð¾Ð²ÐµÑ€ÑÐµÑ‚, ÑÐ²Ð»ÑÐµÑ‚ÑÑ Ð»Ð¸ ÑÑ‚Ð¾Ñ‚ ÑÐ»ÐµÐ¼ÐµÐ½Ñ‚ Ñ€ÐµÑ†ÐµÐ¿Ñ‚Ð¾Ð¼ (Ð±Ð»ÑŽÐ´Ð¾Ð¼)
    /// </summary>
    public bool IsRecipe => RecipeId.HasValue;
}

