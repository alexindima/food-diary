using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class WaistEntry : AggregateRoot<WaistEntryId> {
    private const double MaxCircumference = DesiredWaist.MaxValue;

    public UserId UserId { get; private set; }
    public DateTime Date { get; private set; }
    public double Circumference { get; private set; }

    public User User { get; private set; } = null!;

    private WaistEntry() {
    }

    private WaistEntry(WaistEntryId id) : base(id) {
    }

    public static WaistEntry Create(UserId userId, DateTime date, double circumference) {
        EnsureUserId(userId);
        var normalizedDate = NormalizeDate(date);
        var normalizedCircumference = NormalizeCircumference(circumference);

        var entry = new WaistEntry(WaistEntryId.New()) {
            UserId = userId,
            Date = normalizedDate,
            Circumference = normalizedCircumference,
        };

        entry.SetCreated();
        return entry;
    }

    public void Update(double? circumference = null, DateTime? date = null) {
        var changed = false;

        if (circumference.HasValue) {
            var normalizedCircumference = NormalizeCircumference(circumference.Value);
            if (!AreSame(Circumference, normalizedCircumference)) {
                Circumference = normalizedCircumference;
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
        var dateOnly = value.Date;
        return dateOnly.Kind == DateTimeKind.Utc
            ? dateOnly
            : DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);
    }

    private static double NormalizeCircumference(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(nameof(value), "Circumference must be a finite number.");
        }

        return value is <= 0 or > MaxCircumference
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Circumference must be in range (0, {MaxCircumference}].")
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
