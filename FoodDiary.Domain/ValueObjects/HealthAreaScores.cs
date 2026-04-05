namespace FoodDiary.Domain.ValueObjects;

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

    // Heart: Potassium, Magnesium, + penalize excess Sodium
    private static readonly int[] HeartNutrientIds = [1092, 1090];
    private const int SodiumPenaltyId = 1093;

    // Bone: Calcium, Vitamin D, Vitamin K, Phosphorus, Magnesium
    private static readonly int[] BoneNutrientIds = [1087, 1110, 1185, 1091, 1090];

    // Immune: Vitamin C, Vitamin D, Zinc, Selenium, Vitamin E
    private static readonly int[] ImmuneNutrientIds = [1162, 1110, 1095, 1103, 1109];

    // Energy: Iron, B1, B2, B3, B5, B6, B12, Magnesium
    private static readonly int[] EnergyNutrientIds = [1089, 1165, 1166, 1167, 1170, 1175, 1178, 1090];

    // Antioxidant: Vitamin C, Vitamin E, Selenium, Vitamin A
    private static readonly int[] AntioxidantNutrientIds = [1162, 1109, 1103, 1106];

    private static HealthAreaScore CalculateArea(
        int[] nutrientIds,
        IReadOnlyDictionary<int, double> nutrientAmounts,
        IReadOnlyDictionary<int, double> dailyValues,
        int? penaltyNutrientId = null) {
        var percentSum = 0.0;
        var count = 0;

        foreach (var id in nutrientIds) {
            if (!dailyValues.TryGetValue(id, out var dv) || dv <= 0) {
                continue;
            }

            nutrientAmounts.TryGetValue(id, out var amount);
            percentSum += Math.Min(amount / dv * 100.0, 150.0);
            count++;
        }

        if (count == 0) {
            return new HealthAreaScore(0, HealthAreaGrade.Unknown);
        }

        var avgPercent = percentSum / count;

        // Apply sodium penalty for heart health (excess sodium is harmful)
        if (penaltyNutrientId.HasValue &&
            dailyValues.TryGetValue(penaltyNutrientId.Value, out var penaltyDv) && penaltyDv > 0 &&
            nutrientAmounts.TryGetValue(penaltyNutrientId.Value, out var penaltyAmount)) {
            var sodiumPercent = penaltyAmount / penaltyDv * 100.0;
            if (sodiumPercent > 100) {
                avgPercent -= (sodiumPercent - 100) * 0.3;
            }
        }

        var score = (int)Math.Round(Math.Clamp(avgPercent, 0, 100));
        var grade = score switch {
            >= 75 => HealthAreaGrade.Excellent,
            >= 50 => HealthAreaGrade.Good,
            >= 25 => HealthAreaGrade.Fair,
            > 0 => HealthAreaGrade.Low,
            _ => HealthAreaGrade.Unknown
        };

        return new HealthAreaScore(score, grade);
    }
}

public readonly record struct HealthAreaScore(int Score, HealthAreaGrade Grade);

public enum HealthAreaGrade {
    Unknown = 0,
    Low = 1,
    Fair = 2,
    Good = 3,
    Excellent = 4
}
