using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Шаг рецепта - часть агрегата Recipe
/// НЕ является корнем агрегата
/// </summary>
public sealed class RecipeStep : Entity<RecipeStepId> {
    public RecipeId RecipeId { get; private set; }
    public int StepNumber { get; private set; }
    public string? Title { get; private set; }
    public string Instruction { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public ImageAssetId? ImageAssetId { get; private set; }

    private readonly List<RecipeIngredient> _ingredients = new();
    public IReadOnlyCollection<RecipeIngredient> Ingredients => _ingredients.AsReadOnly();

    // Navigation properties
    public Recipe Recipe { get; private set; } = null!;

    // Конструктор для EF Core
    private RecipeStep() {
    }

    // Factory method (вызывается из Recipe агрегата)
    internal static RecipeStep Create(
        RecipeId recipeId,
        int stepNumber,
        string instruction,
        string? title = null,
        string? imageUrl = null,
        ImageAssetId? imageAssetId = null) {
        if (string.IsNullOrWhiteSpace(instruction)) {
            throw new ArgumentException("Instruction is required", nameof(instruction));
        }

        var step = new RecipeStep {
            Id = RecipeStepId.New(),
            RecipeId = recipeId,
            StepNumber = stepNumber,
            Title = NormalizeTitle(title),
            Instruction = instruction.Trim(),
            ImageUrl = imageUrl,
            ImageAssetId = imageAssetId
        };
        step.SetCreated();
        return step;
    }

    public void Update(string instruction, string? title = null, string? imageUrl = null, ImageAssetId? imageAssetId = null) {
        if (string.IsNullOrWhiteSpace(instruction)) {
            throw new ArgumentException("Instruction is required", nameof(instruction));
        }

        Instruction = instruction.Trim();
        Title = NormalizeTitle(title);
        ImageUrl = imageUrl;
        ImageAssetId = imageAssetId;
        SetModified();
    }

    public RecipeIngredient AddProductIngredient(ProductId productId, double amount) {
        var ingredient = RecipeIngredient.CreateWithProduct(Id, productId, amount);
        _ingredients.Add(ingredient);
        SetModified();
        return ingredient;
    }

    public RecipeIngredient AddNestedRecipeIngredient(RecipeId nestedRecipeId, double servings) {
        var ingredient = RecipeIngredient.CreateWithRecipe(Id, nestedRecipeId, servings);
        _ingredients.Add(ingredient);
        SetModified();
        return ingredient;
    }

    public void RemoveIngredient(RecipeIngredient ingredient) {
        _ingredients.Remove(ingredient);
        SetModified();
    }

    private static string? NormalizeTitle(string? title) {
        if (string.IsNullOrWhiteSpace(title)) {
            return null;
        }

        return title.Trim();
    }
}
