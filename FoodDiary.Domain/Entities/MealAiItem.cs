using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// AI recognized item for a meal session.
/// </summary>
public sealed class MealAiItem : Entity<MealAiItemId>
{
    public MealAiSessionId MealAiSessionId { get; private set; }
    public string NameEn { get; private set; } = string.Empty;
    public string? NameLocal { get; private set; }
    public double Amount { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public double Calories { get; private set; }
    public double Proteins { get; private set; }
    public double Fats { get; private set; }
    public double Carbs { get; private set; }
    public double Fiber { get; private set; }
    public double Alcohol { get; private set; }

    // Navigation properties
    public MealAiSession Session { get; private set; } = null!;

    private MealAiItem() { }

    internal static MealAiItem Create(
        string nameEn,
        string? nameLocal,
        double amount,
        string unit,
        double calories,
        double proteins,
        double fats,
        double carbs,
        double fiber,
        double alcohol)
    {
        var item = new MealAiItem
        {
            Id = MealAiItemId.New(),
            NameEn = nameEn.Trim(),
            NameLocal = string.IsNullOrWhiteSpace(nameLocal) ? null : nameLocal.Trim(),
            Amount = amount,
            Unit = unit.Trim(),
            Calories = calories,
            Proteins = proteins,
            Fats = fats,
            Carbs = carbs,
            Fiber = fiber,
            Alcohol = alcohol
        };
        item.SetCreated();
        return item;
    }

    internal void AttachToSession(MealAiSessionId sessionId)
    {
        MealAiSessionId = sessionId;
    }
}
