using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    private const double KcalPerKgBodyWeight = 7700.0;

    /// <summary>
    /// Calculates Basal Metabolic Rate using the Mifflin-St Jeor equation.
    /// Returns null if required profile data (weight, height, birth date, gender) is missing.
    /// </summary>
    public double? CalculateBmr() {
        if (Weight is null || Height is null || BirthDate is null || Gender is null) {
            return null;
        }

        var age = CalculateAge(BirthDate.Value, DomainTime.UtcNow);
        if (age <= 0) {
            return null;
        }

        // Mifflin-St Jeor: 10 * weight(kg) + 6.25 * height(cm) - 5 * age + offset
        var bmr = 10.0 * Weight.Value + 6.25 * Height.Value - 5.0 * age;

        bmr += Gender.ToUpperInvariant() switch {
            "M" => 5.0,
            _ => -161.0
        };

        return bmr > 0 ? Math.Round(bmr, 0) : null;
    }

    /// <summary>
    /// Estimates TDEE by multiplying BMR by the activity level multiplier.
    /// Returns null if BMR cannot be calculated.
    /// </summary>
    public double? CalculateEstimatedTdee() {
        var bmr = CalculateBmr();
        if (bmr is null) {
            return null;
        }

        var multiplier = GetActivityMultiplier(ActivityLevel);
        return Math.Round(bmr.Value * multiplier, 0);
    }

    internal static double GetActivityMultiplier(ActivityLevel level) {
        return level switch {
            ActivityLevel.Minimal => 1.2,
            ActivityLevel.Light => 1.375,
            ActivityLevel.Moderate => 1.55,
            ActivityLevel.High => 1.725,
            ActivityLevel.Extreme => 1.9,
            _ => 1.55
        };
    }

    private static int CalculateAge(DateTime birthDate, DateTime now) {
        var age = now.Year - birthDate.Year;
        if (now.Date < birthDate.Date.AddYears(age)) {
            age--;
        }

        return age;
    }
}
