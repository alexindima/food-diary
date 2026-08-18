using System.Runtime.InteropServices;

namespace FoodDiary.Domain.ValueObjects;

[StructLayout(LayoutKind.Auto)]
public readonly record struct HealthAreaScores(
    HealthAreaScore Heart,
    HealthAreaScore Bone,
    HealthAreaScore Immune,
    HealthAreaScore Energy,
    HealthAreaScore Antioxidant) {

    public static HealthAreaScores Calculate(
        IReadOnlyDictionary<int, double> nutrientAmounts,
        IReadOnlyDictionary<int, double> dailyValues) {
        return new HealthAreaScores(
            CalculateArea(HeartNutrientIds, nutrientAmounts, dailyValues, SodiumPenaltyId),
            CalculateArea(BoneNutrientIds, nutrientAmounts, dailyValues),
            CalculateArea(ImmuneNutrientIds, nutrientAmounts, dailyValues),
            CalculateArea(EnergyNutrientIds, nutrientAmounts, dailyValues),
            CalculateArea(AntioxidantNutrientIds, nutrientAmounts, dailyValues));
    }

    /// <summary>
    /// Nutrient IDs used for heart health: potassium and magnesium. Excess sodium is applied separately as a penalty.
    /// </summary>
    private static readonly int[] HeartNutrientIds = [1092, 1090];
    private const int SodiumPenaltyId = 1093;

    /// <summary>
    /// Nutrient IDs used for bone health: calcium, vitamins D and K, phosphorus, and magnesium.
    /// </summary>
    private static readonly int[] BoneNutrientIds = [1087, 1110, 1185, 1091, 1090];

    /// <summary>
    /// Nutrient IDs used for immune health: vitamins C, D, and E, zinc, and selenium.
    /// </summary>
    private static readonly int[] ImmuneNutrientIds = [1162, 1110, 1095, 1103, 1109];

    /// <summary>
    /// Nutrient IDs used for energy: iron, vitamins B1, B2, B3, B5, B6, and B12, and magnesium.
    /// </summary>
    private static readonly int[] EnergyNutrientIds = [1089, 1165, 1166, 1167, 1170, 1175, 1178, 1090];

    /// <summary>
    /// Nutrient IDs used for antioxidant health: vitamins C, E, and A, and selenium.
    /// </summary>
    private static readonly int[] AntioxidantNutrientIds = [1162, 1109, 1103, 1106];

    private static HealthAreaScore CalculateArea(
        int[] nutrientIds,
        IReadOnlyDictionary<int, double> nutrientAmounts,
        IReadOnlyDictionary<int, double> dailyValues,
        int? penaltyNutrientId = null) {
        double percentSum = 0.0;
        int count = 0;

        foreach (int id in nutrientIds) {
            if (!dailyValues.TryGetValue(id, out double dv) || dv <= 0) {
                continue;
            }

            nutrientAmounts.TryGetValue(id, out double amount);
            percentSum += Math.Min(amount / dv * 100.0, 150.0);
            count++;
        }

        if (count == 0) {
            return new HealthAreaScore(0, HealthAreaGrade.Unknown);
        }

        double avgPercent = percentSum / count;

        // Apply sodium penalty for heart health (excess sodium is harmful)
        if (penaltyNutrientId.HasValue &&
            dailyValues.TryGetValue(penaltyNutrientId.Value, out double penaltyDv) && penaltyDv > 0 &&
            nutrientAmounts.TryGetValue(penaltyNutrientId.Value, out double penaltyAmount)) {
            double sodiumPercent = penaltyAmount / penaltyDv * 100.0;
            if (sodiumPercent > 100) {
                avgPercent -= (sodiumPercent - 100) * 0.3;
            }
        }

        int score = (int)Math.Round(Math.Clamp(avgPercent, 0, 100), MidpointRounding.ToEven);
        HealthAreaGrade grade = score switch {
            >= 75 => HealthAreaGrade.Excellent,
            >= 50 => HealthAreaGrade.Good,
            >= 25 => HealthAreaGrade.Fair,
            > 0 => HealthAreaGrade.Low,
            _ => HealthAreaGrade.Unknown,
        };

        return new HealthAreaScore(score, grade);
    }
}
