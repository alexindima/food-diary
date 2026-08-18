using FoodDiary.Domain.Common;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Wearables;

public sealed class WearableSyncEntry : AggregateRoot<WearableSyncEntryId> {
    public UserId UserId { get; private set; }
    public WearableProvider Provider { get; private set; }
    public WearableDataType DataType { get; private set; }
    public DateTime Date { get; private set; }
    public double Value { get; private set; }

    public User User { get; private set; } = null!;

    private WearableSyncEntry() {
    }

    public static WearableSyncEntry Create(
        UserId userId,
        WearableProvider provider,
        WearableDataType dataType,
        DateTime date,
        double value) {
        EnsureUserId(userId);
        DomainGuard.Defined(provider, nameof(provider));
        DomainGuard.Defined(dataType, nameof(dataType));

        var entry = new WearableSyncEntry {
            Id = WearableSyncEntryId.New(),
            UserId = userId,
            Provider = provider,
            DataType = dataType,
            Date = NormalizeUtcDate(date),
            Value = DomainGuard.Finite(value, nameof(value)),
        };
        entry.SetCreated();
        return entry;
    }

    public void UpdateValue(double value) {
        DomainGuard.Finite(value, nameof(value));
        if (Math.Abs(Value - value) <= 0.000001) {
            return;
        }

        Value = value;
        SetModified();
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        DateTime utcValue = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
        return DateTime.SpecifyKind(utcValue.Date, DateTimeKind.Utc);
    }
}
