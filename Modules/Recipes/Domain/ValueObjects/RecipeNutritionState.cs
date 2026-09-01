namespace FoodDiary.Domain.ValueObjects;

public readonly record struct RecipeNutritionState(
    double? TotalCalories,
    double? TotalProteins,
    double? TotalFats,
    double? TotalCarbs,
    double? TotalFiber,
    double? TotalAlcohol,
    bool IsNutritionAutoCalculated,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    double? ManualAlcohol) {
    public static RecipeNutritionState CreateInitial() {
        return new RecipeNutritionState(
            TotalCalories: null,
            TotalProteins: null,
            TotalFats: null,
            TotalCarbs: null,
            TotalFiber: null,
            TotalAlcohol: null,
            IsNutritionAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null);
    }
}
