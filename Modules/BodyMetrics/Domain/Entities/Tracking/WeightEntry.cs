using System.Globalization;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects;

namespace FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;

public sealed class WeightEntry : AggregateRoot<WeightEntryId> {
    private const double MaxWeight = DesiredWeightKg.MaxValue;

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
        double normalizedWeight = NormalizeWeight(weight);

        var entry = new WeightEntry(WeightEntryId.New()) {
            UserId = userId,
            Date = day.ToUtcDateTime(),
            WeightKg = normalizedWeight,
        };

        entry.SetCreated();
        return entry;
    }

    public void Update(double? weight = null, DateTime? date = null) {
        UpdateDetails(weight, date.HasValue ? MeasurementDay.FromDateTimeEncoding(date.Value) : null);
    }

    public void UpdateDetails(double? weight = null, MeasurementDay? day = null) {
        bool changed = false;

        if (weight.HasValue) {
            double normalizedWeight = NormalizeWeight(weight.Value);
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

    private static double NormalizeWeight(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(nameof(value), "WeightKg must be a finite number.");
        }

        return value is <= 0 or > MaxWeight
            ? throw new ArgumentOutOfRangeException(nameof(value), string.Create(CultureInfo.InvariantCulture, $"WeightKg must be in range (0, {MaxWeight}]."))
            : value;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static bool AreSame(double left, double right) =>
        Math.Abs(left - right) < 0.0000001d;
}
