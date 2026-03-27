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
    private readonly List<MealItem> _mealItems = [];
    private readonly List<RecipeIngredient> _nestedRecipeUsages = [];
    public IReadOnlyCollection<RecipeStep> Steps => _steps.AsReadOnly();
    public IReadOnlyCollection<MealItem> MealItems => _mealItems.AsReadOnly();
    public IReadOnlyCollection<RecipeIngredient> NestedRecipeUsages => _nestedRecipeUsages.AsReadOnly();

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
        };
        recipe.ApplyDetailsState(new RecipeDetailsState(
            Name: NormalizeRequiredName(name),
            Description: NormalizeOptionalText(description, DescriptionMaxLength, nameof(description)),
            Comment: NormalizeOptionalText(comment, CommentMaxLength, nameof(comment)),
            Category: NormalizeOptionalText(category, CategoryMaxLength, nameof(category)),
            ImageUrl: NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl)),
            ImageAssetId: imageAssetId,
            PrepTime: NormalizeOptionalNonNegative(prepTime, nameof(prepTime)),
            CookTime: NormalizeOptionalNonNegative(cookTime, nameof(cookTime)),
            Servings: RequirePositive(servings, nameof(servings)),
            Visibility: visibility));
        recipe.ApplyNutritionState(RecipeNutritionState.CreateInitial());

        recipe.SetCreated();
        return recipe;
    }

    public void Update(RecipeUpdate update) {
        var changed = false;
        changed |= ApplyIdentityUpdates(
            update.Name,
            update.Description,
            update.ClearDescription,
            update.Comment,
            update.ClearComment,
            update.Category,
            update.ClearCategory);
        changed |= ApplyMediaUpdates(
            update.ImageUrl,
            update.ClearImageUrl,
            update.ImageAssetId,
            update.ClearImageAssetId);
        changed |= ApplyTimingAndServingsUpdates(update.PrepTime, update.CookTime, update.Servings);

        if (update.Visibility.HasValue && Visibility != update.Visibility.Value) {
            Visibility = update.Visibility.Value;
            changed = true;
        }

        if (changed) {
            SetModified();
        }
    }

    public void UpdateIdentity(
        string? name = null,
        string? description = null,
        bool clearDescription = false,
        string? comment = null,
        bool clearComment = false,
        string? category = null,
        bool clearCategory = false) {
        if (ApplyIdentityUpdates(name, description, clearDescription, comment, clearComment, category, clearCategory)) {
            SetModified();
        }
    }

    public void UpdateMedia(
        string? imageUrl = null,
        bool clearImageUrl = false,
        ImageAssetId? imageAssetId = null,
        bool clearImageAssetId = false) {
        if (ApplyMediaUpdates(imageUrl, clearImageUrl, imageAssetId, clearImageAssetId)) {
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
        bool clearDescription,
        string? comment,
        bool clearComment,
        string? category,
        bool clearCategory) {
        var state = GetDetailsState();
        var changed = false;
        var normalizedDescription = NormalizeOptionalText(description, DescriptionMaxLength, nameof(description));
        var normalizedComment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment));
        var normalizedCategory = NormalizeOptionalText(category, CategoryMaxLength, nameof(category));

        EnsureClearConflict(clearDescription, normalizedDescription, nameof(clearDescription), nameof(description));
        EnsureClearConflict(clearComment, normalizedComment, nameof(clearComment), nameof(comment));
        EnsureClearConflict(clearCategory, normalizedCategory, nameof(clearCategory), nameof(category));

        if (name is not null) {
            var normalizedName = NormalizeRequiredName(name);
            if (!string.Equals(state.Name, normalizedName, StringComparison.Ordinal)) {
                state = state with { Name = normalizedName };
                changed = true;
            }
        }

        if (clearDescription) {
            if (state.Description is not null) {
                state = state with { Description = null };
                changed = true;
            }
        }
        else if (description is not null) {
            if (!string.Equals(state.Description, normalizedDescription, StringComparison.Ordinal)) {
                state = state with { Description = normalizedDescription };
                changed = true;
            }
        }

        if (clearComment) {
            if (state.Comment is not null) {
                state = state with { Comment = null };
                changed = true;
            }
        }
        else if (comment is not null) {
            if (!string.Equals(state.Comment, normalizedComment, StringComparison.Ordinal)) {
                state = state with { Comment = normalizedComment };
                changed = true;
            }
        }

        if (clearCategory) {
            if (state.Category is not null) {
                state = state with { Category = null };
                changed = true;
            }
        }
        else if (category is not null) {
            if (!string.Equals(state.Category, normalizedCategory, StringComparison.Ordinal)) {
                state = state with { Category = normalizedCategory };
                changed = true;
            }
        }

        if (changed) {
            ApplyDetailsState(state);
        }

        return changed;
    }

    private bool ApplyMediaUpdates(
        string? imageUrl,
        bool clearImageUrl,
        ImageAssetId? imageAssetId,
        bool clearImageAssetId) {
        var state = GetDetailsState();
        var changed = false;
        var normalizedImageUrl = NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl));

        EnsureClearConflict(clearImageUrl, normalizedImageUrl, nameof(clearImageUrl), nameof(imageUrl));
        EnsureClearConflict(clearImageAssetId, imageAssetId, nameof(clearImageAssetId), nameof(imageAssetId));

        if (clearImageUrl) {
            if (state.ImageUrl is not null) {
                state = state with { ImageUrl = null };
                changed = true;
            }
        }
        else if (imageUrl is not null) {
            if (!string.Equals(state.ImageUrl, normalizedImageUrl, StringComparison.Ordinal)) {
                state = state with { ImageUrl = normalizedImageUrl };
                changed = true;
            }
        }

        if (clearImageAssetId) {
            if (state.ImageAssetId is not null) {
                state = state with { ImageAssetId = null };
                changed = true;
            }
        }
        else if (imageAssetId.HasValue && state.ImageAssetId != imageAssetId) {
            state = state with { ImageAssetId = imageAssetId };
            changed = true;
        }

        if (changed) {
            ApplyDetailsState(state);
        }

        return changed;
    }

    private bool ApplyTimingAndServingsUpdates(int? prepTime, int? cookTime, int? servings) {
        var state = GetDetailsState();
        var changed = false;

        if (prepTime.HasValue) {
            var normalizedPrepTime = NormalizeOptionalNonNegative(prepTime, nameof(prepTime));
            if (state.PrepTime != normalizedPrepTime) {
                state = state with { PrepTime = normalizedPrepTime };
                changed = true;
            }
        }

        if (cookTime.HasValue) {
            var normalizedCookTime = NormalizeOptionalNonNegative(cookTime, nameof(cookTime));
            if (state.CookTime != normalizedCookTime) {
                state = state with { CookTime = normalizedCookTime };
                changed = true;
            }
        }

        if (servings.HasValue) {
            var normalizedServings = RequirePositive(servings.Value, nameof(servings));
            if (state.Servings != normalizedServings) {
                state = state with { Servings = normalizedServings };
                changed = true;
            }
        }

        if (changed) {
            ApplyDetailsState(state);
        }

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
        var state = GetNutritionState() with {
            ManualCalories = nutrition.Calories,
            ManualProteins = nutrition.Proteins,
            ManualFats = nutrition.Fats,
            ManualCarbs = nutrition.Carbs,
            ManualFiber = nutrition.Fiber,
            ManualAlcohol = nutrition.Alcohol
        };
        ApplyNutritionState(state);
    }

    private void ApplyTotalNutrition(RecipeNutrition nutrition) {
        var state = GetNutritionState() with {
            TotalCalories = nutrition.Calories,
            TotalProteins = nutrition.Proteins,
            TotalFats = nutrition.Fats,
            TotalCarbs = nutrition.Carbs,
            TotalFiber = nutrition.Fiber,
            TotalAlcohol = nutrition.Alcohol
        };
        ApplyNutritionState(state);
    }

    private RecipeDetailsState GetDetailsState() {
        return new RecipeDetailsState(
            Name,
            Description,
            Comment,
            Category,
            ImageUrl,
            ImageAssetId,
            PrepTime,
            CookTime,
            Servings,
            Visibility);
    }

    private void ApplyDetailsState(RecipeDetailsState state) {
        Name = state.Name;
        Description = state.Description;
        Comment = state.Comment;
        Category = state.Category;
        ImageUrl = state.ImageUrl;
        ImageAssetId = state.ImageAssetId;
        PrepTime = state.PrepTime;
        CookTime = state.CookTime;
        Servings = state.Servings;
        Visibility = state.Visibility;
    }

    private RecipeNutritionState GetNutritionState() {
        return new RecipeNutritionState(
            TotalCalories,
            TotalProteins,
            TotalFats,
            TotalCarbs,
            TotalFiber,
            TotalAlcohol,
            IsNutritionAutoCalculated,
            ManualCalories,
            ManualProteins,
            ManualFats,
            ManualCarbs,
            ManualFiber,
            ManualAlcohol);
    }

    private void ApplyNutritionState(RecipeNutritionState state) {
        TotalCalories = state.TotalCalories;
        TotalProteins = state.TotalProteins;
        TotalFats = state.TotalFats;
        TotalCarbs = state.TotalCarbs;
        TotalFiber = state.TotalFiber;
        TotalAlcohol = state.TotalAlcohol;
        IsNutritionAutoCalculated = state.IsNutritionAutoCalculated;
        ManualCalories = state.ManualCalories;
        ManualProteins = state.ManualProteins;
        ManualFats = state.ManualFats;
        ManualCarbs = state.ManualCarbs;
        ManualFiber = state.ManualFiber;
        ManualAlcohol = state.ManualAlcohol;
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

    private static void EnsureClearConflict<T>(bool clear, T? value, string clearParamName, string valueParamName)
        where T : class {
        if (clear && value is not null) {
            throw new ArgumentException($"{clearParamName} cannot be true when {valueParamName} is provided.", clearParamName);
        }
    }

    private static void EnsureClearConflict<T>(bool clear, T? value, string clearParamName, string valueParamName)
        where T : struct {
        if (clear && value.HasValue) {
            throw new ArgumentException($"{clearParamName} cannot be true when {valueParamName} is provided.", clearParamName);
        }
    }
}
