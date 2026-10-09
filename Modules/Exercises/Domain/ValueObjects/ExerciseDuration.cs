namespace FoodDiary.Modules.Exercises.Domain.ValueObjects;

public sealed record ExerciseDuration {
    public const int MaximumMinutes = 1440;
    public int Minutes { get; }
    private ExerciseDuration(int minutes) => Minutes = minutes;

    public static ExerciseDuration FromMinutes(int minutes) {
        if (minutes is <= 0 or > MaximumMinutes) {
            throw new ArgumentOutOfRangeException(nameof(minutes), $"Duration must be between 1 and {MaximumMinutes} minutes.");
        }
        return new ExerciseDuration(minutes);
    }
}
