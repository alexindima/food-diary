using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Meals;

public sealed class Meal : AggregateRoot<MealId> {
    private const double ComparisonEpsilon = 0.000001d;
    private const int CommentMaxLength = 2048;
    private const int ImageUrlMaxLength = 2048;

    public UserId UserId { get; private set; }
    public DateTime Date { get; private set; } = DateTime.UtcNow;
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
        int preMealSatietyLevel = 0,
        int postMealSatietyLevel = 0) {
        EnsureUserId(userId);

        var meal = new Meal {
            Id = MealId.New(),
            UserId = userId,
            Date = NormalizeDate(date),
            MealType = mealType,
            Comment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment)),
            ImageUrl = NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl)),
            ImageAssetId = imageAssetId,
            PreMealSatietyLevel = NormalizeSatietyLevel(preMealSatietyLevel),
            PostMealSatietyLevel = NormalizeSatietyLevel(postMealSatietyLevel)
        };
        meal.SetCreated();
        return meal;
    }

    public void UpdateComment(string? comment) {
        var normalizedComment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment));
        if (string.Equals(Comment, normalizedComment, StringComparison.Ordinal)) {
            return;
        }

        Comment = normalizedComment;
        SetModified();
    }

    public void UpdateImage(string? imageUrl, ImageAssetId? imageAssetId = null) {
        var changed = false;
        var normalizedImageUrl = NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl));
        if (!string.Equals(ImageUrl, normalizedImageUrl, StringComparison.Ordinal)) {
            ImageUrl = normalizedImageUrl;
            changed = true;
        }

        if (imageAssetId.HasValue && ImageAssetId != imageAssetId) {
            ImageAssetId = imageAssetId;
            changed = true;
        }

        if (!changed) {
            return;
        }

        SetModified();
    }

    public void UpdateDate(DateTime date) {
        var normalizedDate = NormalizeDate(date);
        if (Date == normalizedDate) {
            return;
        }

        Date = normalizedDate;
        SetModified();
    }

    public void UpdateMealType(MealType? mealType) {
        if (MealType == mealType) {
            return;
        }

        MealType = mealType;
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
        if (item is null) {
            throw new ArgumentNullException(nameof(item));
        }

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
        DateTime recognizedAtUtc,
        string? notes,
        IReadOnlyList<MealAiItemData> items) {
        var session = MealAiSession.Create(Id, imageAssetId, recognizedAtUtc, notes);
        _aiSessions.Add(session);
        if (items.Count > 0) {
            var createdItems = items
                .Select(item => MealAiItem.Create(
                    item.NameEn,
                    item.NameLocal,
                    item.Amount,
                    item.Unit,
                    item.Calories,
                    item.Proteins,
                    item.Fats,
                    item.Carbs,
                    item.Fiber,
                    item.Alcohol))
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

    public void ApplyNutrition(
        double totalCalories,
        double totalProteins,
        double totalFats,
        double totalCarbs,
        double totalFiber,
        double totalAlcohol,
        bool isAutoCalculated,
        double? manualCalories = null,
        double? manualProteins = null,
        double? manualFats = null,
        double? manualCarbs = null,
        double? manualFiber = null,
        double? manualAlcohol = null) {
        var normalizedTotalCalories = RequireNonNegative(totalCalories, nameof(totalCalories));
        var normalizedTotalProteins = RequireNonNegative(totalProteins, nameof(totalProteins));
        var normalizedTotalFats = RequireNonNegative(totalFats, nameof(totalFats));
        var normalizedTotalCarbs = RequireNonNegative(totalCarbs, nameof(totalCarbs));
        var normalizedTotalFiber = RequireNonNegative(totalFiber, nameof(totalFiber));
        var normalizedTotalAlcohol = RequireNonNegative(totalAlcohol, nameof(totalAlcohol));

        var nextTotalCalories = Math.Round(normalizedTotalCalories, 2);
        var nextTotalProteins = Math.Round(normalizedTotalProteins, 2);
        var nextTotalFats = Math.Round(normalizedTotalFats, 2);
        var nextTotalCarbs = Math.Round(normalizedTotalCarbs, 2);
        var nextTotalFiber = Math.Round(normalizedTotalFiber, 2);
        var nextTotalAlcohol = Math.Round(normalizedTotalAlcohol, 2);

        var nextManualCalories = isAutoCalculated
            ? (double?)null
            : manualCalories.HasValue
                ? Math.Round(RequireNonNegative(manualCalories.Value, nameof(manualCalories)), 2)
                : nextTotalCalories;
        var nextManualProteins = isAutoCalculated
            ? (double?)null
            : manualProteins.HasValue
                ? Math.Round(RequireNonNegative(manualProteins.Value, nameof(manualProteins)), 2)
                : nextTotalProteins;
        var nextManualFats = isAutoCalculated
            ? (double?)null
            : manualFats.HasValue
                ? Math.Round(RequireNonNegative(manualFats.Value, nameof(manualFats)), 2)
                : nextTotalFats;
        var nextManualCarbs = isAutoCalculated
            ? (double?)null
            : manualCarbs.HasValue
                ? Math.Round(RequireNonNegative(manualCarbs.Value, nameof(manualCarbs)), 2)
                : nextTotalCarbs;
        var nextManualFiber = isAutoCalculated
            ? (double?)null
            : manualFiber.HasValue
                ? Math.Round(RequireNonNegative(manualFiber.Value, nameof(manualFiber)), 2)
                : nextTotalFiber;
        var nextManualAlcohol = isAutoCalculated
            ? (double?)null
            : manualAlcohol.HasValue
                ? Math.Round(RequireNonNegative(manualAlcohol.Value, nameof(manualAlcohol)), 2)
                : nextTotalAlcohol;

        if (AreClose(TotalCalories, nextTotalCalories)
            && AreClose(TotalProteins, nextTotalProteins)
            && AreClose(TotalFats, nextTotalFats)
            && AreClose(TotalCarbs, nextTotalCarbs)
            && AreClose(TotalFiber, nextTotalFiber)
            && AreClose(TotalAlcohol, nextTotalAlcohol)
            && IsNutritionAutoCalculated == isAutoCalculated
            && NullableAreClose(ManualCalories, nextManualCalories)
            && NullableAreClose(ManualProteins, nextManualProteins)
            && NullableAreClose(ManualFats, nextManualFats)
            && NullableAreClose(ManualCarbs, nextManualCarbs)
            && NullableAreClose(ManualFiber, nextManualFiber)
            && NullableAreClose(ManualAlcohol, nextManualAlcohol)) {
            return;
        }

        TotalCalories = nextTotalCalories;
        TotalProteins = nextTotalProteins;
        TotalFats = nextTotalFats;
        TotalCarbs = nextTotalCarbs;
        TotalFiber = nextTotalFiber;
        TotalAlcohol = nextTotalAlcohol;
        IsNutritionAutoCalculated = isAutoCalculated;
        ManualCalories = nextManualCalories;
        ManualProteins = nextManualProteins;
        ManualFats = nextManualFats;
        ManualCarbs = nextManualCarbs;
        ManualFiber = nextManualFiber;
        ManualAlcohol = nextManualAlcohol;

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
        var normalizedPre = NormalizeSatietyLevel(preMealLevel ?? 0);
        var normalizedPost = NormalizeSatietyLevel(postMealLevel ?? 0);

        if (PreMealSatietyLevel == normalizedPre && PostMealSatietyLevel == normalizedPost) {
            return;
        }

        PreMealSatietyLevel = normalizedPre;
        PostMealSatietyLevel = normalizedPost;
        SetModified();
    }

    private static int NormalizeSatietyLevel(int level) {
        return level is < 0 or > 9
            ? throw new ArgumentOutOfRangeException(nameof(level), "Satiety level must be in range [0, 9].")
            : level;
    }

    private static DateTime NormalizeDate(DateTime value) {
        return value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };
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
