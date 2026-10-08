namespace FoodDiary.Modules.BodyMetrics.Domain.ValueObjects;

public readonly record struct MeasurementDay(DateOnly Value) {
    public static MeasurementDay FromDateTimeEncoding(DateTime value) {
        DateTime utc = value.Kind == DateTimeKind.Unspecified ? value : value.ToUniversalTime();
        return new MeasurementDay(DateOnly.FromDateTime(utc));
    }

    public DateTime ToUtcDateTime() => Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
