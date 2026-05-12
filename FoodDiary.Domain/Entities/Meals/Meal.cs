using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Meals;

public sealed class Meal : AggregateRoot<MealId> {
    private const double ComparisonEpsilon = 0.000001d;
    private const int CommentMaxLength = DomainConstants.CommentMaxLength;
    private const int ImageUrlMaxLength = DomainConstants.ImageUrlMaxLength;

    public UserId UserId { get; private set; }
    public DateTime Date { get; private set; }
    public MealType? MealType { get; private set; }
    public string? Comment { get; private set; }
    public string? ImageUrl { get; private set; }
    public ImageAssetId? ImageAssetId { get; private set; }
    public double TotalCalories { get; private set; }
    public double TotalProteins { get; private set; }
    public double TotalFats { get; private set; }
    public double TotalCarbs { get; private set; }
    public double TotalFiber { get; private set; }
    public double TotalAlcohol { get; private set; }
    public bool IsNutritionAutoCalculated { get; private set; } = true;
    public double? ManualCalories { get; private set; }
    public double? ManualProteins { get; private set; }
    public double? ManualFats { get; private set; }
    public double? ManualCarbs { get; private set; }
    public double? ManualFiber { get; private set; }
    public double? ManualAlcohol { get; private set; }
    public int PreMealSatietyLevel { get; private set; }
    public int PostMealSatietyLevel { get; private set; }

    public User User { get; private set; } = null!;
    private readonly List<MealItem> _items = [];
    public IReadOnlyCollection<MealItem> Items => _items.AsReadOnly();
    private readonly List<MealAiSession> _aiSessions = [];
    public IReadOnlyCollection<MealAiSession> AiSessions => _aiSessions.AsReadOnly();

    private Meal() {
    }

    public static Meal Create(
        UserId userId,
        DateTime date,
        MealType? mealType = null,
        string? comment = null,
        string? imageUrl = null,
        ImageAssetId? imageAssetId = null,
        int preMealSatietyLevel = 3,
        int postMealSatietyLevel = 3) {
        EnsureUserId(userId);

        var meal = new Meal {
            Id = MealId.New(),
            UserId = userId
        };
        meal.ApplyDetailsState(new MealDetailsState(
            Date: NormalizeDate(date),
            MealType: mealType,
            Comment: NormalizeOptionalText(comment, CommentMaxLength, nameof(comment)),
            ImageUrl: NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl)),
            ImageAssetId: imageAssetId,
            PreMealSatietyLevel: NormalizeSatietyLevel(preMealSatietyLevel),
            PostMealSatietyLevel: NormalizeSatietyLevel(postMealSatietyLevel)));
        meal.ApplyNutritionState(MealNutritionState.CreateInitial());
        meal.SetCreated();
        return meal;
    }

    public void UpdateComment(string? comment) {
        var normalizedComment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment));
        var state = GetDetailsState();
        if (string.Equals(state.Comment, normalizedComment, StringComparison.Ordinal)) {
            return;
        }

        ApplyDetailsState(state with { Comment = normalizedComment });
        SetModified();
    }

    public void UpdateImage(string? imageUrl, ImageAssetId? imageAssetId = null) {
        var state = GetDetailsState();
        var changed = false;
        var normalizedImageUrl = NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl));
        if (!string.Equals(state.ImageUrl, normalizedImageUrl, StringComparison.Ordinal)) {
            state = state with { ImageUrl = normalizedImageUrl };
            changed = true;
        }

        if (imageAssetId.HasValue && state.ImageAssetId != imageAssetId) {
            state = state with { ImageAssetId = imageAssetId };
            changed = true;
        }

        if (!changed) {
            return;
        }

        ApplyDetailsState(state);
        SetModified();
    }

    public void UpdateDate(DateTime date) {
        var normalizedDate = NormalizeDate(date);
        var state = GetDetailsState();
        if (state.Date == normalizedDate) {
            return;
        }

        ApplyDetailsState(state with { Date = normalizedDate });
        SetModified();
    }

    public void UpdateMealType(MealType? mealType) {
        var state = GetDetailsState();
        if (state.MealType == mealType) {
            return;
        }

        ApplyDetailsState(state with { MealType = mealType });
        SetModified();
    }

    public MealItem AddProduct(ProductId productId, double amount) {
        var item = MealItem.CreateWithProduct(Id, productId, amount);
        _items.Add(item);
        SetModified();
        return item;
    }

    public MealItem AddRecipe(RecipeId recipeId, double servings) {
        var item = MealItem.CreateWithRecipe(Id, recipeId, servings);
        _items.Add(item);
        SetModified();
        return item;
    }

    public void RemoveItem(MealItem item) {
        ArgumentNullException.ThrowIfNull(item);

        if (_items.Remove(item)) {
            SetModified();
        }
    }

    public void ClearItems() {
        if (_items.Count == 0) {
            return;
        }

        _items.Clear();
        SetModified();
    }

    public MealAiSession AddAiSession(
        ImageAssetId? imageAssetId,
        AiRecognitionSource source,
        DateTime recognizedAtUtc,
        string? notes,
        IReadOnlyList<MealAiItemData> items) {
        var session = MealAiSession.Create(Id, imageAssetId, source, recognizedAtUtc, notes);
        _aiSessions.Add(session);
        if (items.Count > 0) {
            var createdItems = items
                .Select(item => MealAiItem.CreateFromState(item.ToState()))
                .ToList();

            foreach (var item in createdItems) {
                item.AttachToSession(session.Id);
            }

            session.AddItems(createdItems);
        }

        SetModified();
        return session;
    }

    public void ClearAiSessions() {
        if (_aiSessions.Count == 0) {
            return;
        }

        _aiSessions.Clear();
        SetModified();
    }

    public void ApplyNutrition(MealNutritionUpdate update) {
        var normalizedTotalCalories = RequireNonNegative(update.TotalCalories, nameof(update.TotalCalories));
        var normalizedTotalProteins = RequireNonNegative(update.TotalProteins, nameof(update.TotalProteins));
        var normalizedTotalFats = RequireNonNegative(update.TotalFats, nameof(update.TotalFats));
        var normalizedTotalCarbs = RequireNonNegative(update.TotalCarbs, nameof(update.TotalCarbs));
        var normalizedTotalFiber = RequireNonNegative(update.TotalFiber, nameof(update.TotalFiber));
        var normalizedTotalAlcohol = RequireNonNegative(update.TotalAlcohol, nameof(update.TotalAlcohol));

        var nextTotalCalories = Math.Round(normalizedTotalCalories, 2);
        var nextTotalProteins = Math.Round(normalizedTotalProteins, 2);
        var nextTotalFats = Math.Round(normalizedTotalFats, 2);
        var nextTotalCarbs = Math.Round(normalizedTotalCarbs, 2);
        var nextTotalFiber = Math.Round(normalizedTotalFiber, 2);
        var nextTotalAlcohol = Math.Round(normalizedTotalAlcohol, 2);

        var nextManualCalories = update.IsAutoCalculated
            ? (double?)null
            : update.ManualCalories.HasValue
                ? Math.Round(RequireNonNegative(update.ManualCalories.Value, nameof(update.ManualCalories)), 2)
                : nextTotalCalories;
        var nextManualProteins = update.IsAutoCalculated
            ? (double?)null
            : update.ManualProteins.HasValue
                ? Math.Round(RequireNonNegative(update.ManualProteins.Value, nameof(update.ManualProteins)), 2)
                : nextTotalProteins;
        var nextManualFats = update.IsAutoCalculated
            ? (double?)null
            : update.ManualFats.HasValue
                ? Math.Round(RequireNonNegative(update.ManualFats.Value, nameof(update.ManualFats)), 2)
                : nextTotalFats;
        var nextManualCarbs = update.IsAutoCalculated
            ? (double?)null
            : update.ManualCarbs.HasValue
                ? Math.Round(RequireNonNegative(update.ManualCarbs.Value, nameof(update.ManualCarbs)), 2)
                : nextTotalCarbs;
        var nextManualFiber = update.IsAutoCalculated
            ? (double?)null
            : update.ManualFiber.HasValue
                ? Math.Round(RequireNonNegative(update.ManualFiber.Value, nameof(update.ManualFiber)), 2)
                : nextTotalFiber;
        var nextManualAlcohol = update.IsAutoCalculated
            ? (double?)null
            : update.ManualAlcohol.HasValue
                ? Math.Round(RequireNonNegative(update.ManualAlcohol.Value, nameof(update.ManualAlcohol)), 2)
                : nextTotalAlcohol;

        var currentState = GetNutritionState();
        if (AreClose(currentState.TotalCalories, nextTotalCalories)
            && AreClose(currentState.TotalProteins, nextTotalProteins)
            && AreClose(currentState.TotalFats, nextTotalFats)
            && AreClose(currentState.TotalCarbs, nextTotalCarbs)
            && AreClose(currentState.TotalFiber, nextTotalFiber)
            && AreClose(currentState.TotalAlcohol, nextTotalAlcohol)
            && currentState.IsNutritionAutoCalculated == update.IsAutoCalculated
            && NullableAreClose(currentState.ManualCalories, nextManualCalories)
            && NullableAreClose(currentState.ManualProteins, nextManualProteins)
            && NullableAreClose(currentState.ManualFats, nextManualFats)
            && NullableAreClose(currentState.ManualCarbs, nextManualCarbs)
            && NullableAreClose(currentState.ManualFiber, nextManualFiber)
            && NullableAreClose(currentState.ManualAlcohol, nextManualAlcohol)) {
            return;
        }

        ApplyNutritionState(currentState with {
            TotalCalories = nextTotalCalories,
            TotalProteins = nextTotalProteins,
            TotalFats = nextTotalFats,
            TotalCarbs = nextTotalCarbs,
            TotalFiber = nextTotalFiber,
            TotalAlcohol = nextTotalAlcohol,
            IsNutritionAutoCalculated = update.IsAutoCalculated,
            ManualCalories = nextManualCalories,
            ManualProteins = nextManualProteins,
            ManualFats = nextManualFats,
            ManualCarbs = nextManualCarbs,
            ManualFiber = nextManualFiber,
            ManualAlcohol = nextManualAlcohol
        });

        RaiseDomainEvent(new MealNutritionAppliedDomainEvent(
            Id,
            IsNutritionAutoCalculated,
            TotalCalories,
            TotalProteins,
            TotalFats,
            TotalCarbs,
            TotalFiber,
            TotalAlcohol));

        SetModified();
    }

    public void UpdateSatietyLevels(int? preMealLevel, int? postMealLevel) {
        var normalizedPre = NormalizeSatietyLevel(preMealLevel ?? 3);
        var normalizedPost = NormalizeSatietyLevel(postMealLevel ?? 3);
        var state = GetDetailsState();

        if (state.PreMealSatietyLevel == normalizedPre && state.PostMealSatietyLevel == normalizedPost) {
            return;
        }

        ApplyDetailsState(state with {
            PreMealSatietyLevel = normalizedPre,
            PostMealSatietyLevel = normalizedPost
        });
        SetModified();
    }

    private MealDetailsState GetDetailsState() {
        return new MealDetailsState(
            Date,
            MealType,
            Comment,
            ImageUrl,
            ImageAssetId,
            PreMealSatietyLevel,
            PostMealSatietyLevel);
    }

    private void ApplyDetailsState(MealDetailsState state) {
        Date = state.Date;
        MealType = state.MealType;
        Comment = state.Comment;
        ImageUrl = state.ImageUrl;
        ImageAssetId = state.ImageAssetId;
        PreMealSatietyLevel = state.PreMealSatietyLevel;
        PostMealSatietyLevel = state.PostMealSatietyLevel;
    }

    private MealNutritionState GetNutritionState() {
        return new MealNutritionState(
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

    private void ApplyNutritionState(MealNutritionState state) {
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

    private static int NormalizeSatietyLevel(int level) {
        if (level == 0) {
            return 3;
        }

        return level is < 1 or > 5
            ? throw new ArgumentOutOfRangeException(nameof(level), "Satiety level must be in range [1, 5].")
            : level;
    }

    private static DateTime NormalizeDate(DateTime value) {
        if (value.Kind == DateTimeKind.Unspecified) {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        return DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);
    }

    private static double RequireNonNegative(double value, string paramName) {
        if (value < 0) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.");
        }

        return value;
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

    private static bool AreClose(double left, double right) {
        return Math.Abs(left - right) <= ComparisonEpsilon;
    }

    private static bool NullableAreClose(double? left, double? right) {
        if (!left.HasValue && !right.HasValue) {
            return true;
        }

        if (!left.HasValue || !right.HasValue) {
            return false;
        }

        return AreClose(left.Value, right.Value);
    }
}
