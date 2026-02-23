using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Recipes;

public sealed class Recipe : AggregateRoot<RecipeId> {
    private const int NameMaxLength = 256;
    private const int CategoryMaxLength = 128;
    private const int DescriptionMaxLength = 2048;
    private const int CommentMaxLength = 2048;
    private const int ImageUrlMaxLength = 2048;

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
    public Visibility Visibility { get; private set; } = Visibility.Public;
    public int UsageCount { get; private set; }

    public UserId UserId { get; private set; }
    public User User { get; private set; } = null!;
    private readonly List<RecipeStep> _steps = [];
    public IReadOnlyCollection<RecipeStep> Steps => _steps.AsReadOnly();
    public ICollection<MealItem> MealItems { get; private set; } = new List<MealItem>();
    public ICollection<RecipeIngredient> NestedRecipeUsages { get; private set; } = new List<RecipeIngredient>();

    private Recipe() {
    }

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
        Visibility visibility = Visibility.Public) {
        EnsureUserId(userId);

        var recipe = new Recipe {
            Id = RecipeId.New(),
            UserId = userId,
            Name = NormalizeRequiredName(name),
            Servings = RequirePositive(servings, nameof(servings)),
            Description = NormalizeOptionalText(description, DescriptionMaxLength, nameof(description)),
            Comment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment)),
            Category = NormalizeOptionalText(category, CategoryMaxLength, nameof(category)),
            ImageUrl = NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl)),
            ImageAssetId = imageAssetId,
            PrepTime = NormalizeOptionalNonNegative(prepTime, nameof(prepTime)),
            CookTime = NormalizeOptionalNonNegative(cookTime, nameof(cookTime)),
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
        var changed = false;
        changed |= ApplyIdentityUpdates(name, description, comment, category);
        changed |= ApplyMediaUpdates(imageUrl, imageAssetId);
        changed |= ApplyTimingAndServingsUpdates(prepTime, cookTime, servings);

        if (visibility.HasValue && Visibility != visibility.Value) {
            Visibility = visibility.Value;
            changed = true;
        }

        if (changed) {
            SetModified();
        }
    }

    public void UpdateIdentity(
        string? name = null,
        string? description = null,
        string? comment = null,
        string? category = null) {
        if (ApplyIdentityUpdates(name, description, comment, category)) {
            SetModified();
        }
    }

    public void UpdateMedia(string? imageUrl = null, ImageAssetId? imageAssetId = null) {
        if (ApplyMediaUpdates(imageUrl, imageAssetId)) {
            SetModified();
        }
    }

    public void UpdateTimingAndServings(int? prepTime = null, int? cookTime = null, int? servings = null) {
        if (ApplyTimingAndServingsUpdates(prepTime, cookTime, servings)) {
            SetModified();
        }
    }

    public void ChangeVisibility(Visibility visibility) {
        if (Visibility == visibility) {
            return;
        }

        Visibility = visibility;
        SetModified();
    }

    public RecipeStep AddStep(
        int stepNumber,
        string instruction,
        string? title = null,
        string? imageUrl = null,
        ImageAssetId? imageAssetId = null) {
        if (_steps.Any(step => step.StepNumber == stepNumber)) {
            throw new ArgumentException("Step number must be unique within recipe.", nameof(stepNumber));
        }

        var step = RecipeStep.Create(Id, stepNumber, instruction, title, imageUrl, imageAssetId);
        _steps.Add(step);
        SetModified();
        return step;
    }

    public void ClearSteps() {
        if (_steps.Count == 0) {
            return;
        }

        _steps.Clear();
        SetModified();
    }

    public void RemoveStep(RecipeStep step) {
        ArgumentNullException.ThrowIfNull(step);

        if (_steps.Remove(step)) {
            SetModified();
        }
    }

    public void EnableAutoNutrition() {
        if (IsNutritionAutoCalculated
            && ManualCalories is null
            && ManualProteins is null
            && ManualFats is null
            && ManualCarbs is null
            && ManualFiber is null
            && ManualAlcohol is null) {
            return;
        }

        IsNutritionAutoCalculated = true;
        ApplyManualNutrition(RecipeNutrition.Create(null, null, null, null, null, null));
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
        var manualNutrition = RecipeNutrition.Create(calories, proteins, fats, carbs, fiber, alcohol);
        if (!IsNutritionAutoCalculated
            && GetManualNutrition() == manualNutrition
            && GetTotalNutrition() == manualNutrition) {
            return;
        }

        IsNutritionAutoCalculated = false;
        ApplyManualNutrition(manualNutrition);
        ApplyTotalNutrition(manualNutrition);
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

        var computedNutrition = RecipeNutrition.Create(calories, proteins, fats, carbs, fiber, alcohol);
        if (GetTotalNutrition() == computedNutrition) {
            return;
        }

        ApplyTotalNutrition(computedNutrition);
        SetModified();
    }

    private bool ApplyIdentityUpdates(
        string? name,
        string? description,
        string? comment,
        string? category) {
        var changed = false;

        if (name is not null) {
            var normalizedName = NormalizeRequiredName(name);
            if (!string.Equals(Name, normalizedName, StringComparison.Ordinal)) {
                Name = normalizedName;
                changed = true;
            }
        }

        if (description is not null) {
            var normalizedDescription = NormalizeOptionalText(description, DescriptionMaxLength, nameof(description));
            if (!string.Equals(Description, normalizedDescription, StringComparison.Ordinal)) {
                Description = normalizedDescription;
                changed = true;
            }
        }

        if (comment is not null) {
            var normalizedComment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment));
            if (!string.Equals(Comment, normalizedComment, StringComparison.Ordinal)) {
                Comment = normalizedComment;
                changed = true;
            }
        }

        if (category is null) return changed;
        var normalizedCategory = NormalizeOptionalText(category, CategoryMaxLength, nameof(category));
        if (string.Equals(Category, normalizedCategory, StringComparison.Ordinal)) return changed;
        Category = normalizedCategory;
        changed = true;

        return changed;
    }

    private bool ApplyMediaUpdates(string? imageUrl, ImageAssetId? imageAssetId) {
        var changed = false;

        if (imageUrl is not null) {
            var normalizedImageUrl = NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl));
            if (!string.Equals(ImageUrl, normalizedImageUrl, StringComparison.Ordinal)) {
                ImageUrl = normalizedImageUrl;
                changed = true;
            }
        }

        if (!imageAssetId.HasValue || ImageAssetId == imageAssetId) return changed;
        ImageAssetId = imageAssetId;
        changed = true;

        return changed;
    }

    private bool ApplyTimingAndServingsUpdates(int? prepTime, int? cookTime, int? servings) {
        var changed = false;

        if (prepTime.HasValue) {
            var normalizedPrepTime = NormalizeOptionalNonNegative(prepTime, nameof(prepTime));
            if (PrepTime != normalizedPrepTime) {
                PrepTime = normalizedPrepTime;
                changed = true;
            }
        }

        if (cookTime.HasValue) {
            var normalizedCookTime = NormalizeOptionalNonNegative(cookTime, nameof(cookTime));
            if (CookTime != normalizedCookTime) {
                CookTime = normalizedCookTime;
                changed = true;
            }
        }

        if (!servings.HasValue) return changed;
        var normalizedServings = RequirePositive(servings.Value, nameof(servings));
        if (Servings == normalizedServings) return changed;
        Servings = normalizedServings;
        changed = true;

        return changed;
    }

    private RecipeNutrition GetManualNutrition() {
        return RecipeNutrition.Create(
            ManualCalories,
            ManualProteins,
            ManualFats,
            ManualCarbs,
            ManualFiber,
            ManualAlcohol);
    }

    private RecipeNutrition GetTotalNutrition() {
        return RecipeNutrition.Create(
            TotalCalories,
            TotalProteins,
            TotalFats,
            TotalCarbs,
            TotalFiber,
            TotalAlcohol);
    }

    private void ApplyManualNutrition(RecipeNutrition nutrition) {
        ManualCalories = nutrition.Calories;
        ManualProteins = nutrition.Proteins;
        ManualFats = nutrition.Fats;
        ManualCarbs = nutrition.Carbs;
        ManualFiber = nutrition.Fiber;
        ManualAlcohol = nutrition.Alcohol;
    }

    private void ApplyTotalNutrition(RecipeNutrition nutrition) {
        TotalCalories = nutrition.Calories;
        TotalProteins = nutrition.Proteins;
        TotalFats = nutrition.Fats;
        TotalCarbs = nutrition.Carbs;
        TotalFiber = nutrition.Fiber;
        TotalAlcohol = nutrition.Alcohol;
    }

    private static string NormalizeRequiredName(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Recipe name is required.", nameof(value));
        }

        var normalized = value.Trim();
        if (normalized.Length > NameMaxLength) {
            throw new ArgumentOutOfRangeException(nameof(value), $"Recipe name must be at most {NameMaxLength} characters.");
        }

        return normalized;
    }

    private static int RequirePositive(int value, string paramName) {
        return value <= 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.")
            : value;
    }

    private static int? NormalizeOptionalNonNegative(int? value, string paramName) {
        return value switch {
            null => null,
            < 0 => throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative."),
            _ => value.Value
        };
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : normalized;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }
}
