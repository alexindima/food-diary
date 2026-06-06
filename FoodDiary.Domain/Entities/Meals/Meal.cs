using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using System.Globalization;

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
            UserId = userId,
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
        string? normalizedComment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment));
        MealDetailsState state = GetDetailsState();
        if (string.Equals(state.Comment, normalizedComment, StringComparison.Ordinal)) {
            return;
        }

        ApplyDetailsState(state with { Comment = normalizedComment });
        SetModified();
    }

    public void UpdateImage(string? imageUrl, ImageAssetId? imageAssetId = null) {
        MealDetailsState state = GetDetailsState();
        bool changed = false;
        string? normalizedImageUrl = NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl));
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
        DateTime normalizedDate = NormalizeDate(date);
        MealDetailsState state = GetDetailsState();
        if (state.Date == normalizedDate) {
            return;
        }

        ApplyDetailsState(state with { Date = normalizedDate });
        SetModified();
    }

    public void UpdateMealType(MealType? mealType) {
        MealDetailsState state = GetDetailsState();
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

            foreach (MealAiItem? item in createdItems) {
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
        MealNutritionState nextState = CreateNutritionState(update);
        MealNutritionState currentState = GetNutritionState();
        if (NutritionStatesAreClose(currentState, nextState)) {
            return;
        }

        ApplyNutritionState(nextState);

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

    private static MealNutritionState CreateNutritionState(MealNutritionUpdate update) {
        double nextTotalCalories = RoundNonNegative(update.TotalCalories, nameof(update.TotalCalories));
        double nextTotalProteins = RoundNonNegative(update.TotalProteins, nameof(update.TotalProteins));
        double nextTotalFats = RoundNonNegative(update.TotalFats, nameof(update.TotalFats));
        double nextTotalCarbs = RoundNonNegative(update.TotalCarbs, nameof(update.TotalCarbs));
        double nextTotalFiber = RoundNonNegative(update.TotalFiber, nameof(update.TotalFiber));
        double nextTotalAlcohol = RoundNonNegative(update.TotalAlcohol, nameof(update.TotalAlcohol));

        return new MealNutritionState(
            nextTotalCalories,
            nextTotalProteins,
            nextTotalFats,
            nextTotalCarbs,
            nextTotalFiber,
            nextTotalAlcohol,
            update.IsAutoCalculated,
            NormalizeManualNutrition(update.IsAutoCalculated, update.ManualCalories, nextTotalCalories, nameof(update.ManualCalories)),
            NormalizeManualNutrition(update.IsAutoCalculated, update.ManualProteins, nextTotalProteins, nameof(update.ManualProteins)),
            NormalizeManualNutrition(update.IsAutoCalculated, update.ManualFats, nextTotalFats, nameof(update.ManualFats)),
            NormalizeManualNutrition(update.IsAutoCalculated, update.ManualCarbs, nextTotalCarbs, nameof(update.ManualCarbs)),
            NormalizeManualNutrition(update.IsAutoCalculated, update.ManualFiber, nextTotalFiber, nameof(update.ManualFiber)),
            NormalizeManualNutrition(update.IsAutoCalculated, update.ManualAlcohol, nextTotalAlcohol, nameof(update.ManualAlcohol)));
    }

    public void UpdateSatietyLevels(int? preMealLevel, int? postMealLevel) {
        int normalizedPre = NormalizeSatietyLevel(preMealLevel ?? 3);
        int normalizedPost = NormalizeSatietyLevel(postMealLevel ?? 3);
        MealDetailsState state = GetDetailsState();

        if (state.PreMealSatietyLevel == normalizedPre && state.PostMealSatietyLevel == normalizedPost) {
            return;
        }

        ApplyDetailsState(state with {
            PreMealSatietyLevel = normalizedPre,
            PostMealSatietyLevel = normalizedPost,
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

    private static double RoundNonNegative(double value, string paramName) {
        return Math.Round(RequireNonNegative(value, paramName), 2);
    }

    private static double? NormalizeManualNutrition(
        bool isAutoCalculated,
        double? manualValue,
        double totalValue,
        string paramName) {
        if (isAutoCalculated) {
            return null;
        }

        return manualValue.HasValue
            ? RoundNonNegative(manualValue.Value, paramName)
            : totalValue;
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

    private static bool NutritionStatesAreClose(MealNutritionState currentState, MealNutritionState nextState) {
        return AreClose(currentState.TotalCalories, nextState.TotalCalories)
            && AreClose(currentState.TotalProteins, nextState.TotalProteins)
            && AreClose(currentState.TotalFats, nextState.TotalFats)
            && AreClose(currentState.TotalCarbs, nextState.TotalCarbs)
            && AreClose(currentState.TotalFiber, nextState.TotalFiber)
            && AreClose(currentState.TotalAlcohol, nextState.TotalAlcohol)
            && currentState.IsNutritionAutoCalculated == nextState.IsNutritionAutoCalculated
            && NullableAreClose(currentState.ManualCalories, nextState.ManualCalories)
            && NullableAreClose(currentState.ManualProteins, nextState.ManualProteins)
            && NullableAreClose(currentState.ManualFats, nextState.ManualFats)
            && NullableAreClose(currentState.ManualCarbs, nextState.ManualCarbs)
            && NullableAreClose(currentState.ManualFiber, nextState.ManualFiber)
            && NullableAreClose(currentState.ManualAlcohol, nextState.ManualAlcohol);
    }
}
