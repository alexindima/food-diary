using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Рецепт - корень агрегата
/// Управляет коллекцией RecipeSteps
/// </summary>
public sealed class Recipe : AggregateRoot<RecipeId> {
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public string? ImageUrl { get; private set; }
    public ImageAssetId? ImageAssetId { get; private set; }
    public int? PrepTime { get; private set; }
    public int? CookTime { get; private set; }
    public int Servings { get; private set; }
    public double? TotalCalories { get; private set; }
    public double? TotalProteins { get; private set; }
    public double? TotalFats { get; private set; }
    public double? TotalCarbs { get; private set; }
    public double? TotalFiber { get; private set; }
    public bool IsNutritionAutoCalculated { get; private set; } = true;
    public double? ManualCalories { get; private set; }
    public double? ManualProteins { get; private set; }
    public double? ManualFats { get; private set; }
    public double? ManualCarbs { get; private set; }
    public double? ManualFiber { get; private set; }
    public Visibility Visibility { get; private set; } = Visibility.PUBLIC;
    public int UsageCount { get; private set; }

    // Foreign keys
    public UserId UserId { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    private readonly List<RecipeStep> _steps = new();
    public IReadOnlyCollection<RecipeStep> Steps => _steps.AsReadOnly();
    public ICollection<MealItem> MealItems { get; private set; } = new List<MealItem>();
    public ICollection<RecipeIngredient> NestedRecipeUsages { get; private set; } = new List<RecipeIngredient>();

    // Конструктор для EF Core
    private Recipe() {
    }

    // Factory method для создания рецепта
    public static Recipe Create(
        UserId userId,
        string name,
        int servings,
        string? description = null,
        string? category = null,
        string? imageUrl = null,
        ImageAssetId? imageAssetId = null,
        int? prepTime = null,
        int? cookTime = null,
        Visibility visibility = Visibility.PUBLIC) {
        var recipe = new Recipe {
            Id = RecipeId.New(),
            UserId = userId,
            Name = name,
            Servings = servings,
            Description = description,
            Category = category,
            ImageUrl = imageUrl,
            ImageAssetId = imageAssetId,
            PrepTime = prepTime,
            CookTime = cookTime,
            Visibility = visibility
        };
        recipe.SetCreated();
        return recipe;
    }

    public void Update(
        string? name = null,
        string? description = null,
        string? category = null,
        string? imageUrl = null,
        ImageAssetId? imageAssetId = null,
        int? prepTime = null,
        int? cookTime = null,
        int? servings = null,
        Visibility? visibility = null) {
        if (name is not null) Name = name;
        if (description is not null) Description = description;
        if (category is not null) Category = category;
        if (imageUrl is not null) ImageUrl = imageUrl;
        if (imageAssetId.HasValue) ImageAssetId = imageAssetId;
        if (prepTime.HasValue) PrepTime = prepTime;
        if (cookTime.HasValue) CookTime = cookTime;
        if (servings.HasValue) Servings = servings.Value;
        if (visibility.HasValue) Visibility = visibility.Value;

        SetModified();
    }

    public RecipeStep AddStep(int stepNumber, string instruction, string? imageUrl = null) {
        var step = RecipeStep.Create(Id, stepNumber, instruction, imageUrl);
        _steps.Add(step);
        SetModified();
        return step;
    }

    public void ClearSteps() {
        _steps.Clear();
        SetModified();
    }

    public void RemoveStep(RecipeStep step) {
        _steps.Remove(step);
        SetModified();
    }

    public void EnableAutoNutrition() {
        if (IsNutritionAutoCalculated && ManualCalories is null && ManualProteins is null &&
            ManualFats is null && ManualCarbs is null && ManualFiber is null) {
            return;
        }

        IsNutritionAutoCalculated = true;
        ManualCalories = ManualProteins = ManualFats = ManualCarbs = ManualFiber = null;
        SetModified();
    }

    public void SetManualNutrition(
        double? calories,
        double? proteins,
        double? fats,
        double? carbs,
        double? fiber) {
        IsNutritionAutoCalculated = false;
        ManualCalories = calories;
        ManualProteins = proteins;
        ManualFats = fats;
        ManualCarbs = carbs;
        ManualFiber = fiber;
        TotalCalories = calories;
        TotalProteins = proteins;
        TotalFats = fats;
        TotalCarbs = carbs;
        TotalFiber = fiber;
        SetModified();
    }

    public void ApplyComputedNutrition(
        double? calories,
        double? proteins,
        double? fats,
        double? carbs,
        double? fiber) {
        if (!IsNutritionAutoCalculated) {
            return;
        }

        TotalCalories = calories;
        TotalProteins = proteins;
        TotalFats = fats;
        TotalCarbs = carbs;
        TotalFiber = fiber;
        SetModified();
    }
}
