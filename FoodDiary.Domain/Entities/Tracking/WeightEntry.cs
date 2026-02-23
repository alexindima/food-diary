using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class WeightEntry : AggregateRoot<WeightEntryId> {
    private const double MaxWeight = DesiredWeight.MaxValue;

    public UserId UserId { get; private set; }
    public DateTime Date { get; private set; }
    public double Weight { get; private set; }

    public User User { get; private set; } = null!;

    private WeightEntry() {
    }

    private WeightEntry(WeightEntryId id) : base(id) {
    }

    public static WeightEntry Create(UserId userId, DateTime date, double weight) {
        EnsureUserId(userId);
        var normalizedDate = NormalizeDate(date);
        var normalizedWeight = NormalizeWeight(weight);

        var entry = new WeightEntry(WeightEntryId.New()) {
            UserId = userId,
            Date = normalizedDate,
            Weight = normalizedWeight
        };

        entry.SetCreated();
        return entry;
    }

    public void Update(double? weight = null, DateTime? date = null) {
        var changed = false;

        if (weight.HasValue) {
            var normalizedWeight = NormalizeWeight(weight.Value);
            if (!AreSame(Weight, normalizedWeight)) {
                Weight = normalizedWeight;
                changed = true;
            }
        }

        if (date.HasValue) {
            var normalizedDate = NormalizeDate(date.Value);
            if (Date != normalizedDate) {
                Date = normalizedDate;
                changed = true;
            }
        }

        if (changed) {
            SetModified();
        }
    }

    private static DateTime NormalizeDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    private static double NormalizeWeight(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(nameof(value), "Weight must be a finite number.");
        }

        return value is <= 0 or > MaxWeight
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Weight must be in range (0, {MaxWeight}].")
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
