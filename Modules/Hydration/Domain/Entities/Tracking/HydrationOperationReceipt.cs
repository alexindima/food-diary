using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class HydrationOperationReceipt {
    public Guid OperationId { get; private init; }
    public UserId UserId { get; private init; }
    public HydrationEntryId EntryId { get; private init; }
    public DateTime TimestampUtc { get; private init; }
    public int AmountMl { get; private init; }

    private HydrationOperationReceipt() { }

    public static HydrationOperationReceipt Create(Guid operationId, HydrationEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);
        if (operationId == Guid.Empty) {
            throw new ArgumentException("A hydration operation ID is required.", nameof(operationId));
        }
        return new HydrationOperationReceipt {
            OperationId = operationId,
            UserId = entry.UserId,
            EntryId = entry.Id,
            TimestampUtc = NormalizeTimestamp(entry.Timestamp),
            AmountMl = entry.AmountMl,
        };
    }

    public bool Matches(UserId userId, int amountMl, DateTime timestampUtc) =>
        UserId == userId && AmountMl == amountMl && TimestampUtc == NormalizeTimestamp(timestampUtc);

    private static DateTime NormalizeTimestamp(DateTime timestampUtc) {
        DateTime value = DomainGuard.RequiredUtc(timestampUtc, nameof(timestampUtc));
        return new DateTime(value.Ticks / 10 * 10, DateTimeKind.Utc);
    }
}
