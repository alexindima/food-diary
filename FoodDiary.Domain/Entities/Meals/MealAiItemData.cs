using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Entities.Meals;

public sealed record MealAiItemData {
    public string NameEn { get; }
    public string? NameLocal { get; }
    public double Amount { get; }
    public string Unit { get; }
    public double Calories { get; }
    public double Proteins { get; }
    public double Fats { get; }
    public double Carbs { get; }
    public double Fiber { get; }
    public double Alcohol { get; }
    public double Confidence { get; }
    public MealAiItemResolution Resolution { get; }

    public MealAiItemData(
        string nameEn,
        string? nameLocal,
        double amount,
        string unit,
        double calories,
        double proteins,
        double fats,
        double carbs,
        double fiber,
        double alcohol,
        double confidence = 1,
        MealAiItemResolution resolution = MealAiItemResolution.Accepted)
        : this(MealAiItemState.Create(
            nameEn,
            nameLocal,
            amount,
            unit,
            calories,
            proteins,
            fats,
            carbs,
            fiber,
            alcohol,
            confidence,
            resolution)) {
    }

    private MealAiItemData(MealAiItemState state) {
        NameEn = state.NameEn;
        NameLocal = state.NameLocal;
        Amount = state.Amount;
        Unit = state.Unit;
        Calories = state.Calories;
        Proteins = state.Proteins;
        Fats = state.Fats;
        Carbs = state.Carbs;
        Fiber = state.Fiber;
        Alcohol = state.Alcohol;
        Confidence = state.Confidence;
        Resolution = state.Resolution;
    }

    public static MealAiItemData Create(
        string nameEn,
        string? nameLocal,
        double amount,
        string unit,
        double calories,
        double proteins,
        double fats,
        double carbs,
        double fiber,
        double alcohol,
        double confidence = 1,
        MealAiItemResolution resolution = MealAiItemResolution.Accepted) {
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
            alcohol,
            confidence,
            resolution);

        return new MealAiItemData(state);
    }

    public static bool TryCreate(
        string nameEn,
        string? nameLocal,
        double amount,
        string unit,
        double calories,
        double proteins,
        double fats,
        double carbs,
        double fiber,
        double alcohol,
        out MealAiItemData? data,
        out string? error) {
        return TryCreate(
            nameEn,
            nameLocal,
            amount,
            unit,
            calories,
            proteins,
            fats,
            carbs,
            fiber,
            alcohol,
            confidence: 1,
            resolution: MealAiItemResolution.Accepted,
            out data,
            out error);
    }

    public static bool TryCreate(
        string nameEn,
        string? nameLocal,
        double amount,
        string unit,
        double calories,
        double proteins,
        double fats,
        double carbs,
        double fiber,
        double alcohol,
        double confidence,
        MealAiItemResolution resolution,
        out MealAiItemData? data,
        out string? error) {
        try {
            data = Create(nameEn, nameLocal, amount, unit, calories, proteins, fats, carbs, fiber, alcohol, confidence, resolution);
            error = null;
            return true;
        } catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException) {
            data = null;
            error = ex.Message;
            return false;
        }
    }

    internal MealAiItemState ToState() {
        return MealAiItemState.Create(
            NameEn,
            NameLocal,
            Amount,
            Unit,
            Calories,
            Proteins,
            Fats,
            Carbs,
            Fiber,
            Alcohol,
            Confidence,
            Resolution);
    }
}
