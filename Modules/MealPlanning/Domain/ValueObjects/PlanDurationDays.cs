namespace FoodDiary.Modules.MealPlanning.Domain.ValueObjects;

public sealed record PlanDurationDays {
    public const int MaximumDays = 31;
    public int Value { get; }
    private PlanDurationDays(int value) => Value = value;

    public static PlanDurationDays FromDays(int value) {
        if (value is <= 0 or > MaximumDays) {
            throw new ArgumentOutOfRangeException(nameof(value), $"Duration must be between 1 and {MaximumDays} days.");
        }
        return new PlanDurationDays(value);
    }
}
