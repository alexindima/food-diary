using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.Enums;

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
    double Alcohol,
    double Confidence = 1,
    MealAiItemResolution Resolution = MealAiItemResolution.Accepted) {
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
            state.Alcohol,
            state.Confidence,
            state.Resolution);
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
            Alcohol,
            Confidence,
            Resolution);
    }
}
