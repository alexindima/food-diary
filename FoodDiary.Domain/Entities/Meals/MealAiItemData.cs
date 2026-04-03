using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Meals;

public sealed record MealAiItemData(
    string NameEn,
    string? NameLocal,
    double Amount,
    string Unit,
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber,
    double Alcohol) {
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

        return new MealAiItemData(
            state.NameEn,
            state.NameLocal,
            state.Amount,
            state.Unit,
            state.Calories,
            state.Proteins,
            state.Fats,
            state.Carbs,
            state.Fiber,
            state.Alcohol);
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
        try {
            data = Create(nameEn, nameLocal, amount, unit, calories, proteins, fats, carbs, fiber, alcohol);
            error = null;
            return true;
        } catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException) {
            data = null;
            error = ex.Message;
            return false;
        }
    }

    internal MealAiItemState ToState() {
        return new MealAiItemState(
            NameEn,
            NameLocal,
            Amount,
            Unit,
            Calories,
            Proteins,
            Fats,
            Carbs,
            Fiber,
            Alcohol);
    }
}
