using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    private const double KcalPerKgBodyWeight = 7700.0;

    /// <summary>
    /// Calculates Basal Metabolic Rate using the Mifflin-St Jeor equation.
    /// Returns null if required profile data (weight, height, birth date, gender) is missing.
    /// </summary>
    public double? CalculateBmr() => CalculateBmr(WeightKg, HeightCm, BirthDate, Gender);

    public static double? CalculateBmr(double? weightKg, double? heightCm, DateTime? birthDate, string? gender) {
        if (weightKg is null || heightCm is null || birthDate is null || gender is null) {
            return null;
        }

        int age = CalculateAge(birthDate.Value, DomainTime.UtcNow);
        if (age <= 0) {
            return null;
        }

        // Mifflin-St Jeor: 10 * weight(kg) + 6.25 * height(cm) - 5 * age + offset
        double bmr = (10.0 * weightKg.Value) + (6.25 * heightCm.Value) - (5.0 * age);

        bmr += gender.ToUpperInvariant() switch {
            "M" => 5.0,
            _ => -161.0,
        };

        return bmr > 0 && double.IsFinite(bmr)
            ? Math.Round(bmr, 0, MidpointRounding.ToEven)
            : null;
    }

    /// <summary>
    /// Estimates TDEE by multiplying BMR by the activity level multiplier.
    /// Returns null if BMR cannot be calculated.
    /// </summary>
    public double? CalculateEstimatedTdee() => CalculateEstimatedTdee(CalculateBmr(), ActivityLevel);

    public static double? CalculateEstimatedTdee(double? bmr, ActivityLevel activityLevel) {
        if (bmr is null) {
            return null;
        }

        double multiplier = GetActivityMultiplier(activityLevel);
        double estimatedTdee = bmr.Value * multiplier;
        return double.IsFinite(estimatedTdee)
            ? Math.Round(estimatedTdee, 0, MidpointRounding.ToEven)
            : null;
    }

    internal static double GetActivityMultiplier(ActivityLevel level) {
        return level switch {
            ActivityLevel.Minimal => 1.2,
            ActivityLevel.Light => 1.375,
            ActivityLevel.Moderate => 1.55,
            ActivityLevel.High => 1.725,
            ActivityLevel.Extreme => 1.9,
            _ => throw new ArgumentOutOfRangeException(nameof(level), "Activity level must be one of the supported values."),
        };
    }

    private static int CalculateAge(DateTime birthDate, DateTime now) {
        int age = now.Year - birthDate.Year;
        if (now.Date < birthDate.Date.AddYears(age)) {
            age--;
        }

        return age;
    }
}
