using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects;

namespace FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;

public sealed class WaistEntry : AggregateRoot<WaistEntryId> {
    public UserId UserId { get; private set; }
    public DateTime Date { get; private set; }
    public double CircumferenceCm { get; private set; }

    private WaistEntry() {
    }

    private WaistEntry(WaistEntryId id) : base(id) {
    }

    public static WaistEntry Create(UserId userId, DateTime date, double circumference) {
        EnsureUserId(userId);
        return CreateForDay(userId, MeasurementDay.FromDateTimeEncoding(date), circumference);
    }

    public static WaistEntry CreateForDay(UserId userId, MeasurementDay day, double circumference) {
        EnsureUserId(userId);
        return CreateWithMeasurement(userId, day, MeasuredWaistCm.Create(circumference));
    }

    public static WaistEntry CreateWithMeasurement(UserId userId, MeasurementDay day, MeasuredWaistCm circumference) {
        EnsureUserId(userId);
        ArgumentNullException.ThrowIfNull(circumference);

        var entry = new WaistEntry(WaistEntryId.New()) {
            UserId = userId,
            Date = day.ToUtcDateTime(),
            CircumferenceCm = circumference.Value,
        };

        entry.SetCreated();
        return entry;
    }

    public void Update(double? circumference = null, DateTime? date = null) {
        UpdateDetails(circumference, date.HasValue ? MeasurementDay.FromDateTimeEncoding(date.Value) : null);
    }

    public void UpdateDetails(double? circumference = null, MeasurementDay? day = null) {
        UpdateMeasurement(circumference.HasValue ? MeasuredWaistCm.Create(circumference.Value) : null, day);
    }

    public void UpdateMeasurement(MeasuredWaistCm? circumference = null, MeasurementDay? day = null) {
        bool changed = false;

        if (circumference is not null) {
            double normalizedCircumference = circumference.Value;
            if (!AreSame(CircumferenceCm, normalizedCircumference)) {
                CircumferenceCm = normalizedCircumference;
                changed = true;
            }
        }

        if (day.HasValue) {
            DateTime normalizedDate = day.Value.ToUtcDateTime();
            if (Date != normalizedDate) {
                Date = normalizedDate;
                changed = true;
            }
        }

        if (changed) {
            SetModified();
        }
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static bool AreSame(double left, double right) =>
        Math.Abs(left - right) < 0.0000001d;
}
