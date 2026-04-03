using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Meals;

public sealed class MealAiItem : Entity<MealAiItemId> {
    private const int NameMaxLength = 256;
    private const int UnitMaxLength = 32;

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
        double alcohol) {
        var state = MealAiItemState.Create(
            nameEn,
            nameLocal,
            amount,
            unit,
            calories,
            proteins,
            fats,
            carbs,
            fiber,
            alcohol);

        return CreateFromState(state);
    }

    internal static MealAiItem CreateFromState(MealAiItemState state) {
        var item = new MealAiItem {
            Id = MealAiItemId.New(),
            NameEn = state.NameEn,
            NameLocal = state.NameLocal,
            Amount = state.Amount,
            Unit = state.Unit,
            Calories = state.Calories,
            Proteins = state.Proteins,
            Fats = state.Fats,
            Carbs = state.Carbs,
            Fiber = state.Fiber,
            Alcohol = state.Alcohol
        };
        item.SetCreated();
        return item;
    }

    internal void AttachToSession(MealAiSessionId sessionId) {
        if (sessionId == MealAiSessionId.Empty) {
            throw new ArgumentException("SessionId is required.", nameof(sessionId));
        }

        if (MealAiSessionId == sessionId) {
            return;
        }

        MealAiSessionId = sessionId;
    }
}
