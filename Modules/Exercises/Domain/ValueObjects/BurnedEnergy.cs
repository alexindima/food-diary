using System.Globalization;

namespace FoodDiary.Modules.Exercises.Domain.ValueObjects;

public sealed record BurnedEnergy {
    public const double MaximumKilocalories = 10_000;
    public double Kilocalories { get; }
    private BurnedEnergy(double kilocalories) => Kilocalories = kilocalories;

    public static BurnedEnergy FromKilocalories(double calories) {
        if (!double.IsFinite(calories)) {
            throw new ArgumentOutOfRangeException(nameof(calories), "Calories must be a finite number.");
        }
        if (calories is < 0 or > MaximumKilocalories) {
            throw new ArgumentOutOfRangeException(nameof(calories), string.Create(CultureInfo.InvariantCulture, $"Calories must be between 0 and {MaximumKilocalories}."));
        }
        return new BurnedEnergy(RoundKilocalories(calories));
    }

    internal static double RoundKilocalories(double calories) =>
        Math.Round(calories * 10, MidpointRounding.ToEven) / 10;
}
