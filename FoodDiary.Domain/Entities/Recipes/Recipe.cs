using System;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Recipes;

/// <summary>
/// Ð ÐµÑ†ÐµÐ¿Ñ‚ - ÐºÐ¾Ñ€ÐµÐ½ÑŒ Ð°Ð³Ñ€ÐµÐ³Ð°Ñ‚Ð°
/// Ð£Ð¿Ñ€Ð°Ð²Ð»ÑÐµÑ‚ ÐºÐ¾Ð»Ð»ÐµÐºÑ†Ð¸ÐµÐ¹ RecipeSteps
/// </summary>
public sealed class Recipe : AggregateRoot<RecipeId> {
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Comment { get; private set; }
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
    public double? TotalAlcohol { get; private set; }
    public bool IsNutritionAutoCalculated { get; private set; } = true;
    public double? ManualCalories { get; private set; }
    public double? ManualProteins { get; private set; }
    public double? ManualFats { get; private set; }
    public double? ManualCarbs { get; private set; }
    public double? ManualFiber { get; private set; }
    public double? ManualAlcohol { get; private set; }
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

    // ÐšÐ¾Ð½ÑÑ‚Ñ€ÑƒÐºÑ‚Ð¾Ñ€ Ð´Ð»Ñ EF Core
    private Recipe() {
    }

    // Factory method Ð´Ð»Ñ ÑÐ¾Ð·Ð´Ð°Ð½Ð¸Ñ Ñ€ÐµÑ†ÐµÐ¿Ñ‚Ð°
    public static Recipe Create(
        UserId userId,
        string name,
        int servings,
        string? description = null,
        string? comment = null,
        string? category = null,
        string? imageUrl = null,
        ImageAssetId? imageAssetId = null,
        int? prepTime = null,
        int? cookTime = null,
        Visibility visibility = Visibility.PUBLIC) {
        var normalizedName = NormalizeRequiredName(name);
        var normalizedServings = RequirePositive(servings, nameof(servings));
        var normalizedPrepTime = NormalizeOptionalNonNegative(prepTime, nameof(prepTime));
        var normalizedCookTime = NormalizeOptionalNonNegative(cookTime, nameof(cookTime));

        var recipe = new Recipe {
            Id = RecipeId.New(),
            UserId = userId,
            Name = normalizedName,
            Servings = normalizedServings,
            Description = description,
            Comment = comment,
            Category = category,
            ImageUrl = imageUrl,
            ImageAssetId = imageAssetId,
            PrepTime = normalizedPrepTime,
            CookTime = normalizedCookTime,
            Visibility = visibility
        };
        recipe.SetCreated();
        return recipe;
    }

    public void Update(
        string? name = null,
        string? description = null,
        string? comment = null,
        string? category = null,
        string? imageUrl = null,
        ImageAssetId? imageAssetId = null,
        int? prepTime = null,
        int? cookTime = null,
        int? servings = null,
        Visibility? visibility = null) {
        if (name is not null) Name = NormalizeRequiredName(name);
        if (description is not null) Description = description;
        if (comment is not null) Comment = comment;
        if (category is not null) Category = category;
        if (imageUrl is not null) ImageUrl = imageUrl;
        if (imageAssetId.HasValue) ImageAssetId = imageAssetId;
        if (prepTime.HasValue) PrepTime = NormalizeOptionalNonNegative(prepTime, nameof(prepTime));
        if (cookTime.HasValue) CookTime = NormalizeOptionalNonNegative(cookTime, nameof(cookTime));
        if (servings.HasValue) Servings = RequirePositive(servings.Value, nameof(servings));
        if (visibility.HasValue) Visibility = visibility.Value;

        SetModified();
    }

    public RecipeStep AddStep(
        int stepNumber,
        string instruction,
        string? title = null,
        string? imageUrl = null,
        ImageAssetId? imageAssetId = null) {
        var step = RecipeStep.Create(Id, stepNumber, instruction, title, imageUrl, imageAssetId);
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
            ManualFats is null && ManualCarbs is null && ManualFiber is null && ManualAlcohol is null) {
            return;
        }

        IsNutritionAutoCalculated = true;
        ManualCalories = ManualProteins = ManualFats = ManualCarbs = ManualFiber = ManualAlcohol = null;
        RaiseDomainEvent(new RecipeAutoNutritionEnabledDomainEvent(Id));
        SetModified();
    }

    public void SetManualNutrition(
        double? calories,
        double? proteins,
        double? fats,
        double? carbs,
        double? fiber,
        double? alcohol) {
        var normalizedCalories = NormalizeOptionalNonNegative(calories, nameof(calories));
        var normalizedProteins = NormalizeOptionalNonNegative(proteins, nameof(proteins));
        var normalizedFats = NormalizeOptionalNonNegative(fats, nameof(fats));
        var normalizedCarbs = NormalizeOptionalNonNegative(carbs, nameof(carbs));
        var normalizedFiber = NormalizeOptionalNonNegative(fiber, nameof(fiber));
        var normalizedAlcohol = NormalizeOptionalNonNegative(alcohol, nameof(alcohol));

        IsNutritionAutoCalculated = false;
        ManualCalories = normalizedCalories;
        ManualProteins = normalizedProteins;
        ManualFats = normalizedFats;
        ManualCarbs = normalizedCarbs;
        ManualFiber = normalizedFiber;
        ManualAlcohol = normalizedAlcohol;
        TotalCalories = normalizedCalories;
        TotalProteins = normalizedProteins;
        TotalFats = normalizedFats;
        TotalCarbs = normalizedCarbs;
        TotalFiber = normalizedFiber;
        TotalAlcohol = normalizedAlcohol;
        RaiseDomainEvent(new RecipeManualNutritionSetDomainEvent(Id));
        SetModified();
    }

    public void ApplyComputedNutrition(
        double? calories,
        double? proteins,
        double? fats,
        double? carbs,
        double? fiber,
        double? alcohol) {
        if (!IsNutritionAutoCalculated) {
            return;
        }

        TotalCalories = NormalizeOptionalNonNegative(calories, nameof(calories));
        TotalProteins = NormalizeOptionalNonNegative(proteins, nameof(proteins));
        TotalFats = NormalizeOptionalNonNegative(fats, nameof(fats));
        TotalCarbs = NormalizeOptionalNonNegative(carbs, nameof(carbs));
        TotalFiber = NormalizeOptionalNonNegative(fiber, nameof(fiber));
        TotalAlcohol = NormalizeOptionalNonNegative(alcohol, nameof(alcohol));
        SetModified();
    }

    private static string NormalizeRequiredName(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Recipe name is required.", nameof(value));
        }

        return value.Trim();
    }

    private static int RequirePositive(int value, string paramName) {
        if (value <= 0) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.");
        }

        return value;
    }

    private static int? NormalizeOptionalNonNegative(int? value, string paramName) {
        if (!value.HasValue) {
            return null;
        }

        if (value.Value < 0) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.");
        }

        return value.Value;
    }

    private static double? NormalizeOptionalNonNegative(double? value, string paramName) {
        if (!value.HasValue) {
            return null;
        }

        if (value.Value < 0) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.");
        }

        return value.Value;
    }
}

