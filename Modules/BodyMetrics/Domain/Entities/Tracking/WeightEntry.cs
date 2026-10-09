using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects;

namespace FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;

public sealed class WeightEntry : AggregateRoot<WeightEntryId> {
    public UserId UserId { get; private set; }
    public DateTime Date { get; private set; }
    public double WeightKg { get; private set; }

    private WeightEntry() {
    }

    private WeightEntry(WeightEntryId id) : base(id) {
    }

    public static WeightEntry Create(UserId userId, DateTime date, double weight) {
        EnsureUserId(userId);
        return CreateForDay(userId, MeasurementDay.FromDateTimeEncoding(date), weight);
    }

    public static WeightEntry CreateForDay(UserId userId, MeasurementDay day, double weight) {
        EnsureUserId(userId);
        return CreateWithMeasurement(userId, day, MeasuredWeightKg.Create(weight));
    }

    public static WeightEntry CreateWithMeasurement(UserId userId, MeasurementDay day, MeasuredWeightKg weight) {
        EnsureUserId(userId);
        ArgumentNullException.ThrowIfNull(weight);

        var entry = new WeightEntry(WeightEntryId.New()) {
            UserId = userId,
            Date = day.ToUtcDateTime(),
            WeightKg = weight.Value,
        };

        entry.SetCreated();
        return entry;
    }

    public void Update(double? weight = null, DateTime? date = null) {
        UpdateDetails(weight, date.HasValue ? MeasurementDay.FromDateTimeEncoding(date.Value) : null);
    }

    public void UpdateDetails(double? weight = null, MeasurementDay? day = null) {
        UpdateMeasurement(weight.HasValue ? MeasuredWeightKg.Create(weight.Value) : null, day);
    }

    public void UpdateMeasurement(MeasuredWeightKg? weight = null, MeasurementDay? day = null) {
        bool changed = false;

        if (weight is not null) {
            double normalizedWeight = weight.Value;
            if (!AreSame(WeightKg, normalizedWeight)) {
                WeightKg = normalizedWeight;
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
