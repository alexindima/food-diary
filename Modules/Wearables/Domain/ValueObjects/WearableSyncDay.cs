namespace FoodDiary.Modules.Wearables.Domain.ValueObjects;

public sealed record WearableSyncDay {
    public DateTime Value { get; }

    private WearableSyncDay(DateTime value) => Value = value;

    public static WearableSyncDay FromDate(DateTime date) {
        DateTime utc = date.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
            : date.ToUniversalTime();
        return new WearableSyncDay(DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc));
    }
}
