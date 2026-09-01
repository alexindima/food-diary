using System.Runtime.InteropServices;

namespace FoodDiary.Domain.ValueObjects;

[StructLayout(LayoutKind.Auto)]
public readonly record struct ProductNutrition {
    public double CaloriesPerBase { get; }
    public double ProteinsPerBase { get; }
    public double FatsPerBase { get; }
    public double CarbsPerBase { get; }
    public double FiberPerBase { get; }
    public double AlcoholPerBase { get; }

    public ProductNutrition(
        double caloriesPerBase,
        double proteinsPerBase,
        double fatsPerBase,
        double carbsPerBase,
        double fiberPerBase,
        double alcoholPerBase) {
        CaloriesPerBase = EnsureNonNegative(caloriesPerBase, nameof(caloriesPerBase));
        ProteinsPerBase = EnsureNonNegative(proteinsPerBase, nameof(proteinsPerBase));
        FatsPerBase = EnsureNonNegative(fatsPerBase, nameof(fatsPerBase));
        CarbsPerBase = EnsureNonNegative(carbsPerBase, nameof(carbsPerBase));
        FiberPerBase = EnsureNonNegative(fiberPerBase, nameof(fiberPerBase));
        AlcoholPerBase = EnsureNonNegative(alcoholPerBase, nameof(alcoholPerBase));
    }

    public static ProductNutrition Create(
        double caloriesPerBase,
        double proteinsPerBase,
        double fatsPerBase,
        double carbsPerBase,
        double fiberPerBase,
        double alcoholPerBase) {
        return new ProductNutrition(
            EnsureNonNegative(caloriesPerBase, nameof(caloriesPerBase)),
            EnsureNonNegative(proteinsPerBase, nameof(proteinsPerBase)),
            EnsureNonNegative(fatsPerBase, nameof(fatsPerBase)),
            EnsureNonNegative(carbsPerBase, nameof(carbsPerBase)),
            EnsureNonNegative(fiberPerBase, nameof(fiberPerBase)),
            EnsureNonNegative(alcoholPerBase, nameof(alcoholPerBase)));
    }

    public ProductNutrition With(
        double? caloriesPerBase = null,
        double? proteinsPerBase = null,
        double? fatsPerBase = null,
        double? carbsPerBase = null,
        double? fiberPerBase = null,
        double? alcoholPerBase = null) {
        return new ProductNutrition(
            caloriesPerBase.HasValue
                ? EnsureNonNegative(caloriesPerBase.Value, nameof(caloriesPerBase))
                : CaloriesPerBase,
            proteinsPerBase.HasValue
                ? EnsureNonNegative(proteinsPerBase.Value, nameof(proteinsPerBase))
                : ProteinsPerBase,
            fatsPerBase.HasValue
                ? EnsureNonNegative(fatsPerBase.Value, nameof(fatsPerBase))
                : FatsPerBase,
            carbsPerBase.HasValue
                ? EnsureNonNegative(carbsPerBase.Value, nameof(carbsPerBase))
                : CarbsPerBase,
            fiberPerBase.HasValue
                ? EnsureNonNegative(fiberPerBase.Value, nameof(fiberPerBase))
                : FiberPerBase,
            alcoholPerBase.HasValue
                ? EnsureNonNegative(alcoholPerBase.Value, nameof(alcoholPerBase))
                : AlcoholPerBase);
    }

    public bool IsCloseTo(ProductNutrition other, double epsilon) {
        return Math.Abs(CaloriesPerBase - other.CaloriesPerBase) <= epsilon
               && Math.Abs(ProteinsPerBase - other.ProteinsPerBase) <= epsilon
               && Math.Abs(FatsPerBase - other.FatsPerBase) <= epsilon
               && Math.Abs(CarbsPerBase - other.CarbsPerBase) <= epsilon
               && Math.Abs(FiberPerBase - other.FiberPerBase) <= epsilon
               && Math.Abs(AlcoholPerBase - other.AlcoholPerBase) <= epsilon;
    }

    private static double EnsureNonNegative(double value, string paramName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be a finite number.");
        }

        return value < 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.")
            : value;
    }
}
