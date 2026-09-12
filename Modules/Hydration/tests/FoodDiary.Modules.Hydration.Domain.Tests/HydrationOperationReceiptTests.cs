using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class HydrationOperationReceiptTests {
    [Fact]
    public void Receipt_PreservesOriginalPayloadAfterEntryEdit() {
        var owner = new UserId(Guid.NewGuid());
        DateTime now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
        var entry = HydrationEntry.Create(owner, now, 250);
        var receipt = HydrationOperationReceipt.Create(Guid.NewGuid(), entry);
        entry.Update(amountMl: 500, timestampUtc: now.AddHours(1));

        Assert.Equal(entry.Id, receipt.EntryId);
        Assert.True(receipt.Matches(owner, 250, now));
        Assert.False(receipt.Matches(owner, 500, now));
        Assert.False(receipt.Matches(owner, 250, now.AddHours(1)));
        Assert.False(receipt.Matches(new UserId(Guid.NewGuid()), 250, now));
    }

    [Fact]
    public void Receipt_UsesDatabaseTimestampPrecisionForReplay() {
        var owner = new UserId(Guid.NewGuid());
        DateTime now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
        var entry = HydrationEntry.Create(owner, now.AddTicks(3), 250);
        var receipt = HydrationOperationReceipt.Create(Guid.NewGuid(), entry);

        Assert.True(receipt.Matches(owner, 250, now));
        Assert.True(receipt.Matches(owner, 250, now.AddTicks(9)));
        Assert.False(receipt.Matches(owner, 250, now.AddTicks(10)));
        Assert.Throws<ArgumentOutOfRangeException>(() => receipt.Matches(owner, 250, DateTime.SpecifyKind(now, DateTimeKind.Unspecified)));
        Assert.Throws<ArgumentException>(() => HydrationOperationReceipt.Create(Guid.Empty, entry));
    }
}
