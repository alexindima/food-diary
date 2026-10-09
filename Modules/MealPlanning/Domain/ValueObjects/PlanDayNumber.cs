namespace FoodDiary.Modules.MealPlanning.Domain.ValueObjects;

public sealed record PlanDayNumber {
    public int Value { get; }
    private PlanDayNumber(int value) => Value = value;

    public static PlanDayNumber FromIndex(int value) {
        if (value is <= 0 or > PlanDurationDays.MaximumDays) {
            throw new ArgumentOutOfRangeException(nameof(value), $"Day number must be between 1 and {PlanDurationDays.MaximumDays}.");
        }
        return new PlanDayNumber(value);
    }
}
