using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Modules.Hydration.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Hydration.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class HydrationEntryTypedAmountTests {
    private static readonly DateTime Timestamp = new(2026, 10, 8, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateWithAmount_PreservesOwnerQuantityAndUtcTimestamp() {
        var userId = UserId.New();
        var amount = HydrationAmount.FromMilliliters(250);

        var entry = HydrationEntry.CreateWithAmount(userId, Timestamp, amount);

        Assert.Multiple(() => {
            Assert.Equal(userId, entry.UserId);
            Assert.Equal(250, entry.AmountMl);
            Assert.Equal(Timestamp, entry.Timestamp);
            Assert.Equal(DateTimeKind.Utc, entry.Timestamp.Kind);
        });
    }

    [Fact]
    public void CreateWithAmount_RejectsMissingAmount() {
        Assert.Throws<ArgumentNullException>(() => HydrationEntry.CreateWithAmount(UserId.New(), Timestamp, null!));
    }

    [Fact]
    public void UpdateDetails_WithTheSameQuantity_DoesNotMarkModified() {
        var entry = HydrationEntry.CreateWithAmount(UserId.New(), Timestamp, HydrationAmount.FromMilliliters(250));

        entry.UpdateDetails(HydrationAmount.FromMilliliters(250), Timestamp);

        Assert.Null(entry.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateDetails_ChangesTheAmountAndPreservesTheTimestamp() {
        var entry = HydrationEntry.CreateWithAmount(UserId.New(), Timestamp, HydrationAmount.FromMilliliters(250));

        entry.UpdateDetails(HydrationAmount.FromMilliliters(500));

        Assert.Multiple(() => {
            Assert.Equal(500, entry.AmountMl);
            Assert.Equal(Timestamp, entry.Timestamp);
            Assert.NotNull(entry.ModifiedOnUtc);
        });
    }

    [Fact]
    public void UpdateDetails_WithNoAmount_CanChangeOnlyTheTimestamp() {
        var entry = HydrationEntry.CreateWithAmount(UserId.New(), Timestamp, HydrationAmount.FromMilliliters(250));
        DateTime next = Timestamp.AddMinutes(10);

        entry.UpdateDetails(timestampUtc: next);

        Assert.Multiple(() => {
            Assert.Equal(250, entry.AmountMl);
            Assert.Equal(next, entry.Timestamp);
        });
    }

    [Fact]
    public void UpdateDetails_WithInvalidTimestamp_DoesNotApplyTheNewQuantity() {
        var entry = HydrationEntry.CreateWithAmount(UserId.New(), Timestamp, HydrationAmount.FromMilliliters(250));

        Assert.Throws<ArgumentOutOfRangeException>(() => entry.UpdateDetails(
            HydrationAmount.FromMilliliters(500), DateTime.SpecifyKind(Timestamp, DateTimeKind.Unspecified)));

        Assert.Multiple(() => {
            Assert.Equal(250, entry.AmountMl);
            Assert.Equal(Timestamp, entry.Timestamp);
            Assert.Null(entry.ModifiedOnUtc);
        });
    }

    [Fact]
    public void PrimitiveCreate_PreservesUserValidationBeforeAmountValidation() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => HydrationEntry.Create(UserId.Empty, Timestamp, 0));

        Assert.Equal("userId", error.ParamName);
    }

    [Fact]
    public void PrimitiveUpdate_PreservesAmountValidationBeforeTimestampValidation() {
        var entry = HydrationEntry.CreateWithAmount(UserId.New(), Timestamp, HydrationAmount.FromMilliliters(250));

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => entry.Update(
            amountMl: 0, timestampUtc: DateTime.SpecifyKind(Timestamp, DateTimeKind.Unspecified)));

        Assert.Multiple(() => {
            Assert.Equal("value", error.ParamName);
            Assert.Equal(250, entry.AmountMl);
            Assert.Null(entry.ModifiedOnUtc);
        });
    }
}
