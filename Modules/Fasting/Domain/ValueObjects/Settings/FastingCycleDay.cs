namespace FoodDiary.Modules.Fasting.Domain.ValueObjects.Settings;

public readonly record struct FastingCycleDay {
    public DateOnly Value { get; }
    private FastingCycleDay(DateOnly value) => Value = value;

    public static FastingCycleDay FromDateTimeEncoding(DateTime value, string paramName = "anchorDateUtc") {
        if (value.Kind == DateTimeKind.Unspecified) {
            throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.");
        }
        return new FastingCycleDay(DateOnly.FromDateTime(value.ToUniversalTime()));
    }

    public DateTime ToUtcDateTime() => Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
