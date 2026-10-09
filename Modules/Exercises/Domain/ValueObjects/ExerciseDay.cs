namespace FoodDiary.Modules.Exercises.Domain.ValueObjects;

public readonly record struct ExerciseDay {
    public DateOnly Value { get; }
    private ExerciseDay(DateOnly value) => Value = value;

    public static ExerciseDay FromDateTimeEncoding(DateTime value) => new(DateOnly.FromDateTime(
        value.Kind == DateTimeKind.Unspecified ? value : value.ToUniversalTime()));

    public DateTime ToUtcDateTime() => Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
