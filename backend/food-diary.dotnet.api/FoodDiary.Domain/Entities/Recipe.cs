using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Рецепт - корень агрегата
/// Управляет коллекцией RecipeSteps и RecipeIngredients
/// </summary>
public class Recipe : AggregateRoot<int>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public string? ImageUrl { get; private set; }
    public int? PrepTime { get; private set; }
    public int? CookTime { get; private set; }
    public int Servings { get; private set; }
    public double? TotalCalories { get; private set; }
    public double? TotalProteins { get; private set; }
    public double? TotalFats { get; private set; }
    public double? TotalCarbs { get; private set; }

    // Foreign keys
    public UserId UserId { get; private set; }

    // Navigation properties
    public virtual User User { get; private set; } = null!;
    private readonly List<RecipeStep> _steps = new();
    public virtual IReadOnlyCollection<RecipeStep> Steps => _steps.AsReadOnly();
    private readonly List<RecipeIngredient> _ingredients = new();
    public virtual IReadOnlyCollection<RecipeIngredient> Ingredients => _ingredients.AsReadOnly();

    // Конструктор для EF Core
    private Recipe() { }

    // Factory method для создания рецепта
    public static Recipe Create(
        UserId userId,
        string name,
        int servings,
        string? description = null,
        string? category = null,
        string? imageUrl = null,
        int? prepTime = null,
        int? cookTime = null)
    {
        var recipe = new Recipe
        {
            UserId = userId,
            Name = name,
            Servings = servings,
            Description = description,
            Category = category,
            ImageUrl = imageUrl,
            PrepTime = prepTime,
            CookTime = cookTime
        };
        recipe.SetCreated();
        return recipe;
    }

    public void Update(
        string? name = null,
        string? description = null,
        string? category = null,
        string? imageUrl = null,
        int? prepTime = null,
        int? cookTime = null,
        int? servings = null)
    {
        if (name is not null) Name = name;
        if (description is not null) Description = description;
        if (category is not null) Category = category;
        if (imageUrl is not null) ImageUrl = imageUrl;
        if (prepTime.HasValue) PrepTime = prepTime;
        if (cookTime.HasValue) CookTime = cookTime;
        if (servings.HasValue) Servings = servings.Value;

        SetModified();
    }

    public RecipeStep AddStep(int stepNumber, string instruction)
    {
        var step = RecipeStep.Create(Id, stepNumber, instruction);
        _steps.Add(step);
        SetModified();
        return step;
    }

    public void RemoveStep(RecipeStep step)
    {
        _steps.Remove(step);
        SetModified();
    }

    /// <summary>
    /// Добавить продукт в ингредиенты рецепта
    /// </summary>
    public RecipeIngredient AddProduct(ProductId productId, double amount)
    {
        var ingredient = RecipeIngredient.CreateWithProduct(Id, productId, amount);
        _ingredients.Add(ingredient);
        SetModified();
        return ingredient;
    }

    /// <summary>
    /// Добавить другой рецепт (блюдо) в ингредиенты рецепта
    /// Например, добавить "соус" в рецепт "пасты"
    /// </summary>
    public RecipeIngredient AddNestedRecipe(int nestedRecipeId, double servings)
    {
        var ingredient = RecipeIngredient.CreateWithRecipe(Id, nestedRecipeId, servings);
        _ingredients.Add(ingredient);
        SetModified();
        return ingredient;
    }

    public void RemoveIngredient(RecipeIngredient ingredient)
    {
        _ingredients.Remove(ingredient);
        SetModified();
    }

    public void UpdateNutrition(double? totalCalories, double? totalProteins, double? totalFats, double? totalCarbs)
    {
        TotalCalories = totalCalories;
        TotalProteins = totalProteins;
        TotalFats = totalFats;
        TotalCarbs = totalCarbs;
        SetModified();
    }
}
