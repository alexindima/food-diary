using FoodDiary.Modules.Recipes.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Events;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using System.Globalization;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Entities;

public sealed class Recipe : AggregateRoot<RecipeId> {
    public const int NameMaxLength = 256;
    public const int CategoryMaxLength = 128;
    public const int DescriptionMaxLength = 2048;
    public const int CommentMaxLength = 2048;
    public const int ImageUrlMaxLength = 2048;

    private readonly List<RecipeImage> _images = [];
    public IReadOnlyCollection<RecipeImage> Images => _images.AsReadOnly();
    public void ReplaceImages(IReadOnlyList<RecipeImage> images) {
        if (images.Count > 5 || images.Select(image => image.ImageAssetId).Distinct().Count() != images.Count) {
            throw new ArgumentException("A recipe can have up to five distinct images.", nameof(images));
        }
        _images.RemoveAll(image => !images.Any(next => next.ImageAssetId == image.ImageAssetId));
        for (int index = 0; index < images.Count; index++) {
            RecipeImage? existing = _images.Find(image => image.ImageAssetId == images[index].ImageAssetId);
            if (existing is null) { _images.Add(new RecipeImage(images[index].ImageAssetId, images[index].ImageUrl, index)); } else { existing.SetPosition(index); }
        }
        ImageAssetId = images.Count > 0 ? images[0].ImageAssetId : null;
        ImageUrl = images.Count > 0 ? images[0].ImageUrl : null;
        SetModified();
    }

    public bool LanguageConfirmed { get; private set; }
    public void SetLanguageConfirmation(bool confirmed) { if (LanguageConfirmed != confirmed) { LanguageConfirmed = confirmed; SetModified(); } }
    public string Language { get; private set; } = "en";
    public void ChangeLanguage(string language) {
        if (!LanguageCode.TryParse(language, out LanguageCode code)) {
            throw new ArgumentOutOfRangeException(nameof(language), "Recipe language must be en or ru.");
        }
        if (!string.Equals(Language, code.Value, StringComparison.Ordinal)) { Language = code.Value; LanguageConfirmed = false; SetModified(); }
    }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Comment { get; private set; }
    public RecipeCategory Category { get; private set; } = RecipeCategory.Other;
    public string? ImageUrl { get; private set; }
    public ImageAssetId? ImageAssetId { get; private set; }
    public int? PrepTime { get; private set; }
    public int? CookTime { get; private set; }
    public int Servings { get; private set; }
    public int MissingIngredientCount { get; private set; }
    public void SetMissingIngredientCount(int count) {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (MissingIngredientCount != count) {
            MissingIngredientCount = count;
            SetModified();
        }
    }

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

    public UserId UserId { get; private set; }
    private readonly List<RecipeStep> _steps = [];
    private readonly List<RecipeIngredient> _nestedRecipeUsages = [];
    public IReadOnlyCollection<RecipeStep> Steps => _steps.AsReadOnly();
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
        Visibility visibility = Visibility.Public, string language = "en", RecipeId? importId = null) {
        EnsureUserId(userId);
        DomainGuard.Defined(visibility, nameof(visibility));

        var recipe = new Recipe {
            Id = importId ?? RecipeId.New(),
            UserId = userId,
        };
        recipe.ApplyDetailsState(new RecipeDetailsState(
            Name: NormalizeRequiredName(name),
            Description: NormalizeOptionalText(description, DescriptionMaxLength, nameof(description)),
            Comment: NormalizeOptionalText(comment, CommentMaxLength, nameof(comment)),
            Category: category is null ? RecipeCategory.Other : RecipeCategoryCodes.Parse(category),
            ImageUrl: NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl)),
            ImageAssetId: imageAssetId,
            PrepTime: NormalizeOptionalNonNegative(prepTime, nameof(prepTime)),
            CookTime: NormalizeOptionalNonNegative(cookTime, nameof(cookTime)),
            Servings: RequirePositive(servings, nameof(servings)),
            Visibility: visibility));
        recipe.ApplyNutritionState(RecipeNutritionState.CreateInitial());

        recipe.ChangeLanguage(language);
        recipe.SetCreated();
        return recipe;
    }

    public void UpdateIdentityChanges(RecipeIdentityChanges changes) {
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(changes.Description, nameof(changes));
        ArgumentNullException.ThrowIfNull(changes.Comment, nameof(changes));
        ArgumentNullException.ThrowIfNull(changes.Category, nameof(changes));
        UpdateIdentity(changes.Name,
            changes.Description.IsSet ? changes.Description.Value : null, changes.Description.IsClear,
            changes.Comment.IsSet ? changes.Comment.Value : null, changes.Comment.IsClear,
            changes.Category.IsSet ? changes.Category.Value : null, changes.Category.IsClear);
    }

    public void UpdateMediaChanges(RecipeMediaChanges changes) {
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(changes.ImageUrl, nameof(changes));
        ArgumentNullException.ThrowIfNull(changes.ImageAssetId, nameof(changes));
        UpdateMedia(changes.ImageUrl.IsSet ? changes.ImageUrl.Value : null, changes.ImageUrl.IsClear,
            changes.ImageAssetId.IsSet ? changes.ImageAssetId.Value : null, changes.ImageAssetId.IsClear);
    }

    public void Update(RecipeUpdate update) {
        ValidateUpdate(update);
        if (update.Visibility.HasValue) {
            DomainGuard.Defined(update.Visibility.Value, nameof(update.Visibility));
        }

        bool changed = false;
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

    private static void ValidateUpdate(RecipeUpdate update) {
        string? normalizedDescription = NormalizeOptionalText(update.Description, DescriptionMaxLength, nameof(update.Description));
        string? normalizedComment = NormalizeOptionalText(update.Comment, CommentMaxLength, nameof(update.Comment));
        string? normalizedCategory = NormalizeOptionalText(update.Category, CategoryMaxLength, nameof(update.Category));
        string? normalizedImageUrl = NormalizeOptionalText(update.ImageUrl, ImageUrlMaxLength, nameof(update.ImageUrl));

        EnsureClearConflict(update.ClearDescription, normalizedDescription, nameof(update.ClearDescription), nameof(update.Description));
        EnsureClearConflict(update.ClearComment, normalizedComment, nameof(update.ClearComment), nameof(update.Comment));
        EnsureClearConflict(update.ClearCategory, normalizedCategory, nameof(update.ClearCategory), nameof(update.Category));
        if (update.Category is not null) {
            RecipeCategoryCodes.Parse(update.Category);
        }
        EnsureClearConflict(update.ClearImageUrl, normalizedImageUrl, nameof(update.ClearImageUrl), nameof(update.ImageUrl));
        EnsureClearConflict(update.ClearImageAssetId, update.ImageAssetId, nameof(update.ClearImageAssetId), nameof(update.ImageAssetId));

        if (update.Name is not null) {
            NormalizeRequiredName(update.Name);
        }
        if (update.PrepTime.HasValue) {
            NormalizeOptionalNonNegative(update.PrepTime, nameof(update.PrepTime));
        }
        if (update.CookTime.HasValue) {
            NormalizeOptionalNonNegative(update.CookTime, nameof(update.CookTime));
        }
        if (update.Servings.HasValue) {
            RequirePositive(update.Servings.Value, nameof(update.Servings));
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
        DomainGuard.Defined(visibility, nameof(visibility));
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
        if (_steps.Exists(step => step.StepNumber == stepNumber)) {
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
        ApplyManualNutrition(RecipeNutrition.Create(calories: null, proteins: null, fats: null, carbs: null, fiber: null, alcohol: null));
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
        SetMissingIngredientCount(0);
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
        RecipeDetailsState state = GetDetailsState();
        bool changed = false;
        string? normalizedDescription = NormalizeOptionalText(description, DescriptionMaxLength, nameof(description));
        string? normalizedComment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment));
        string? normalizedCategory = NormalizeOptionalText(category, CategoryMaxLength, nameof(category));

        EnsureClearConflict(clearDescription, normalizedDescription, nameof(clearDescription), nameof(description));
        EnsureClearConflict(clearComment, normalizedComment, nameof(clearComment), nameof(comment));
        EnsureClearConflict(clearCategory, normalizedCategory, nameof(clearCategory), nameof(category));

        if (name is not null) {
            string normalizedName = NormalizeRequiredName(name);
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
        } else if (description is not null) {
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
        } else if (comment is not null) {
            if (!string.Equals(state.Comment, normalizedComment, StringComparison.Ordinal)) {
                state = state with { Comment = normalizedComment };
                changed = true;
            }
        }

        RecipeCategory nextCategory = state.Category;
        if (clearCategory) {
            nextCategory = RecipeCategory.Other;
        } else if (category is not null) {
            nextCategory = RecipeCategoryCodes.Parse(category);
        }
        if (state.Category != nextCategory) {
            state = state with { Category = nextCategory };
            changed = true;
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
        RecipeDetailsState state = GetDetailsState();
        bool changed = false;
        string? normalizedImageUrl = NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl));

        EnsureClearConflict(clearImageUrl, normalizedImageUrl, nameof(clearImageUrl), nameof(imageUrl));
        EnsureClearConflict(clearImageAssetId, imageAssetId, nameof(clearImageAssetId), nameof(imageAssetId));

        if (clearImageUrl) {
            if (state.ImageUrl is not null) {
                state = state with { ImageUrl = null };
                changed = true;
            }
        } else if (imageUrl is not null) {
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
        } else if (imageAssetId.HasValue && state.ImageAssetId != imageAssetId) {
            state = state with { ImageAssetId = imageAssetId };
            changed = true;
        }

        if (changed) {
            ApplyDetailsState(state);
        }

        return changed;
    }

    private bool ApplyTimingAndServingsUpdates(int? prepTime, int? cookTime, int? servings) {
        RecipeDetailsState state = GetDetailsState();
        bool changed = false;

        if (prepTime.HasValue) {
            int? normalizedPrepTime = NormalizeOptionalNonNegative(prepTime, nameof(prepTime));
            if (state.PrepTime != normalizedPrepTime) {
                state = state with { PrepTime = normalizedPrepTime };
                changed = true;
            }
        }

        if (cookTime.HasValue) {
            int? normalizedCookTime = NormalizeOptionalNonNegative(cookTime, nameof(cookTime));
            if (state.CookTime != normalizedCookTime) {
                state = state with { CookTime = normalizedCookTime };
                changed = true;
            }
        }

        if (servings.HasValue) {
            int normalizedServings = RequirePositive(servings.Value, nameof(servings));
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
        RecipeNutritionState state = GetNutritionState() with {
            ManualCalories = nutrition.Calories,
            ManualProteins = nutrition.Proteins,
            ManualFats = nutrition.Fats,
            ManualCarbs = nutrition.Carbs,
            ManualFiber = nutrition.Fiber,
            ManualAlcohol = nutrition.Alcohol,
        };
        ApplyNutritionState(state);
    }

    private void ApplyTotalNutrition(RecipeNutrition nutrition) {
        RecipeNutritionState state = GetNutritionState() with {
            TotalCalories = nutrition.Calories,
            TotalProteins = nutrition.Proteins,
            TotalFats = nutrition.Fats,
            TotalCarbs = nutrition.Carbs,
            TotalFiber = nutrition.Fiber,
            TotalAlcohol = nutrition.Alcohol,
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

        string normalized = value.Trim();
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
            _ => value.Value,
        };
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, string.Create(CultureInfo.InvariantCulture, $"Value must be at most {maxLength} characters."))
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
