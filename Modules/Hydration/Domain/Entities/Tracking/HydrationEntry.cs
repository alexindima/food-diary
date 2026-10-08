using FoodDiary.Modules.Hydration.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Hydration.Domain.ValueObjects;
using System.Diagnostics.CodeAnalysis;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Hydration.Domain.Entities.Tracking;

public sealed class HydrationEntry : AggregateRoot<HydrationEntryId> {
    public const int MaximumAmountMl = HydrationAmount.MaximumMilliliters;

    public UserId UserId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public int AmountMl { get; private set; }

    [ExcludeFromCodeCoverage]
    private HydrationEntry() {
    }

    private HydrationEntry(HydrationEntryId id) : base(id) {
    }

    public static HydrationEntry Create(UserId userId, DateTime timestampUtc, int amountMl) {
        EnsureUserId(userId);
        return CreateWithAmount(userId, timestampUtc, HydrationAmount.FromMilliliters(amountMl));
    }

    public static HydrationEntry CreateWithAmount(UserId userId, DateTime timestampUtc, HydrationAmount amount) {
        EnsureUserId(userId);
        ArgumentNullException.ThrowIfNull(amount);
        DateTime normalizedTimestamp = Normalize(timestampUtc);

        var entry = new HydrationEntry(HydrationEntryId.New()) {
            UserId = userId,
            Timestamp = normalizedTimestamp,
            AmountMl = amount.Milliliters,
        };

        entry.SetCreated();
        return entry;
    }

    public void Update(int? amountMl = null, DateTime? timestampUtc = null) {
        HydrationAmount? amount = amountMl.HasValue ? HydrationAmount.FromMilliliters(amountMl.Value) : null;
        UpdateDetails(amount, timestampUtc);
    }

    public void UpdateDetails(HydrationAmount? amount = null, DateTime? timestampUtc = null) {
        int? normalizedAmountMl = amount?.Milliliters;
        DateTime? normalizedTimestamp = timestampUtc.HasValue ? Normalize(timestampUtc.Value) : null;
        bool changed = false;

        if (normalizedAmountMl.HasValue) {
            if (AmountMl != normalizedAmountMl.Value) {
                AmountMl = normalizedAmountMl.Value;
                changed = true;
            }
        }

        if (normalizedTimestamp.HasValue) {
            if (Timestamp != normalizedTimestamp.Value) {
                Timestamp = normalizedTimestamp.Value;
                changed = true;
            }
        }

        if (changed) {
            SetModified();
        }
    }

    private static DateTime Normalize(DateTime value) {
        return DomainGuard.RequiredUtc(value, nameof(value));
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

}
