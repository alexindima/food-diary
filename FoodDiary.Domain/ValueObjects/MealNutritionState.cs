namespace FoodDiary.Domain.ValueObjects;

public readonly record struct MealNutritionState(
    double TotalCalories,
    double TotalProteins,
    double TotalFats,
    double TotalCarbs,
    double TotalFiber,
    double TotalAlcohol,
    bool IsNutritionAutoCalculated,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    double? ManualAlcohol) {
    public static MealNutritionState CreateInitial() {
        return new MealNutritionState(
            TotalCalories: 0,
            TotalProteins: 0,
            TotalFats: 0,
            TotalCarbs: 0,
            TotalFiber: 0,
            TotalAlcohol: 0,
            IsNutritionAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null);
    }
}
