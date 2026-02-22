using System;
using System.Linq;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Meals;

/// <summary>
/// ÐŸÑ€Ð¸ÐµÐ¼ Ð¿Ð¸Ñ‰Ð¸ - ÐºÐ¾Ñ€ÐµÐ½ÑŒ Ð°Ð³Ñ€ÐµÐ³Ð°Ñ‚Ð°
/// Ð£Ð¿Ñ€Ð°Ð²Ð»ÑÐµÑ‚ ÐºÐ¾Ð»Ð»ÐµÐºÑ†Ð¸ÐµÐ¹ MealItems (Ð¿Ñ€Ð¾Ð´ÑƒÐºÑ‚Ð¾Ð² Ð¸ Ð±Ð»ÑŽÐ´)
/// </summary>
public sealed class Meal : AggregateRoot<MealId> {
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

    // Navigation properties
    public User User { get; private set; } = null!;
    private readonly List<MealItem> _items = new();
    public IReadOnlyCollection<MealItem> Items => _items.AsReadOnly();
    private readonly List<MealAiSession> _aiSessions = new();
    public IReadOnlyCollection<MealAiSession> AiSessions => _aiSessions.AsReadOnly();

    // ÐšÐ¾Ð½ÑÑ‚Ñ€ÑƒÐºÑ‚Ð¾Ñ€ Ð´Ð»Ñ EF Core
    private Meal() {
        _items = new List<MealItem>();
        _aiSessions = new List<MealAiSession>();
    }

    // Factory method Ð´Ð»Ñ ÑÐ¾Ð·Ð´Ð°Ð½Ð¸Ñ Ð¿Ñ€Ð¸ÐµÐ¼Ð° Ð¿Ð¸Ñ‰Ð¸
    public static Meal Create(
        UserId userId,
        DateTime date,
        MealType? mealType = null,
        string? comment = null,
        string? imageUrl = null,
        ImageAssetId? imageAssetId = null,
        int preMealSatietyLevel = 0,
        int postMealSatietyLevel = 0) {
        var meal = new Meal {
            Id = MealId.New(),
            UserId = userId,
            Date = NormalizeDate(date),
            MealType = mealType,
            Comment = comment,
            ImageUrl = imageUrl,
            ImageAssetId = imageAssetId,
            PreMealSatietyLevel = NormalizeSatietyLevel(preMealSatietyLevel),
            PostMealSatietyLevel = NormalizeSatietyLevel(postMealSatietyLevel)
        };
        meal.SetCreated();
        return meal;
    }

    public void UpdateComment(string? comment) {
        Comment = comment;
        SetModified();
    }

    public void UpdateImage(string? imageUrl, ImageAssetId? imageAssetId = null) {
        ImageUrl = imageUrl;
        if (imageAssetId.HasValue) {
            ImageAssetId = imageAssetId;
        }
        SetModified();
    }

    public void UpdateDate(DateTime date) {
        Date = NormalizeDate(date);
        SetModified();
    }

    public void UpdateMealType(MealType? mealType) {
        MealType = mealType;
        SetModified();
    }

    /// <summary>
    /// Ð”Ð¾Ð±Ð°Ð²Ð¸Ñ‚ÑŒ Ð¿Ñ€Ð¾Ð´ÑƒÐºÑ‚ Ð² Ð¿Ñ€Ð¸ÐµÐ¼ Ð¿Ð¸Ñ‰Ð¸
    /// </summary>
    public MealItem AddProduct(ProductId productId, double amount) {
        var item = MealItem.CreateWithProduct(Id, productId, amount);
        _items.Add(item);
        SetModified();
        return item;
    }

    /// <summary>
    /// Ð”Ð¾Ð±Ð°Ð²Ð¸Ñ‚ÑŒ Ð±Ð»ÑŽÐ´Ð¾ (Ñ€ÐµÑ†ÐµÐ¿Ñ‚) Ð² Ð¿Ñ€Ð¸ÐµÐ¼ Ð¿Ð¸Ñ‰Ð¸
    /// </summary>
    public MealItem AddRecipe(RecipeId recipeId, double servings) {
        var item = MealItem.CreateWithRecipe(Id, recipeId, servings);
        _items.Add(item);
        SetModified();
        return item;
    }

    public void RemoveItem(MealItem item) {
        _items.Remove(item);
        SetModified();
    }

    public void ClearItems() {
        _items.Clear();
        SetModified();
    }

    public MealAiSession AddAiSession(
        ImageAssetId? imageAssetId,
        DateTime recognizedAtUtc,
        string? notes,
        IReadOnlyList<MealAiItemData> items)
    {
        var session = MealAiSession.Create(Id, imageAssetId, recognizedAtUtc, notes);
        _aiSessions.Add(session);
        if (items.Count > 0)
        {
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

            foreach (var item in createdItems)
            {
                item.AttachToSession(session.Id);
            }
            session.AddItems(createdItems);
        }
        SetModified();
        return session;
    }

    public void ClearAiSessions()
    {
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

        TotalCalories = Math.Round(normalizedTotalCalories, 2);
        TotalProteins = Math.Round(normalizedTotalProteins, 2);
        TotalFats = Math.Round(normalizedTotalFats, 2);
        TotalCarbs = Math.Round(normalizedTotalCarbs, 2);
        TotalFiber = Math.Round(normalizedTotalFiber, 2);
        TotalAlcohol = Math.Round(normalizedTotalAlcohol, 2);

        IsNutritionAutoCalculated = isAutoCalculated;

        if (isAutoCalculated) {
            ManualCalories = null;
            ManualProteins = null;
            ManualFats = null;
            ManualCarbs = null;
            ManualFiber = null;
            ManualAlcohol = null;
        } else {
            ManualCalories = manualCalories.HasValue
                ? Math.Round(RequireNonNegative(manualCalories.Value, nameof(manualCalories)), 2)
                : TotalCalories;
            ManualProteins = manualProteins.HasValue
                ? Math.Round(RequireNonNegative(manualProteins.Value, nameof(manualProteins)), 2)
                : TotalProteins;
            ManualFats = manualFats.HasValue
                ? Math.Round(RequireNonNegative(manualFats.Value, nameof(manualFats)), 2)
                : TotalFats;
            ManualCarbs = manualCarbs.HasValue
                ? Math.Round(RequireNonNegative(manualCarbs.Value, nameof(manualCarbs)), 2)
                : TotalCarbs;
            ManualFiber = manualFiber.HasValue
                ? Math.Round(RequireNonNegative(manualFiber.Value, nameof(manualFiber)), 2)
                : TotalFiber;
            ManualAlcohol = manualAlcohol.HasValue
                ? Math.Round(RequireNonNegative(manualAlcohol.Value, nameof(manualAlcohol)), 2)
                : TotalAlcohol;
        }

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

    private static int NormalizeSatietyLevel(int level) =>
        level is >= 0 and <= 9 ? level : 0;

    private static DateTime NormalizeDate(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };
    }

    private static double RequireNonNegative(double value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.");
        }

        return value;
    }
}

