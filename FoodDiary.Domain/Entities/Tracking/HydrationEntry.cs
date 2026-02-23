using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class HydrationEntry : AggregateRoot<HydrationEntryId> {
    private const int MaxSingleEntryMl = 10000;

    public UserId UserId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public int AmountMl { get; private set; }

    public User User { get; private set; } = null!;

    private HydrationEntry() {
    }

    private HydrationEntry(HydrationEntryId id) : base(id) {
    }

    public static HydrationEntry Create(UserId userId, DateTime timestampUtc, int amountMl) {
        EnsureUserId(userId);
        var normalizedAmountMl = NormalizeAmount(amountMl);
        var normalizedTimestamp = Normalize(timestampUtc);

        var entry = new HydrationEntry(HydrationEntryId.New()) {
            UserId = userId,
            Timestamp = normalizedTimestamp,
            AmountMl = normalizedAmountMl
        };

        entry.SetCreated();
        return entry;
    }

    public void Update(int? amountMl = null, DateTime? timestampUtc = null) {
        var changed = false;

        if (amountMl.HasValue) {
            var normalizedAmountMl = NormalizeAmount(amountMl.Value);
            if (AmountMl != normalizedAmountMl) {
                AmountMl = normalizedAmountMl;
                changed = true;
            }
        }

        if (timestampUtc.HasValue) {
            var normalizedTimestamp = Normalize(timestampUtc.Value);
            if (Timestamp != normalizedTimestamp) {
                Timestamp = normalizedTimestamp;
                changed = true;
            }
        }

        if (changed) {
            SetModified();
        }
    }

    private static DateTime Normalize(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };
        return utc;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static int NormalizeAmount(int value) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxSingleEntryMl);
        return value;
    }
}
