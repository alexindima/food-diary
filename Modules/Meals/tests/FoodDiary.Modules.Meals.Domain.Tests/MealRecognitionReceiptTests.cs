using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class MealRecognitionReceiptTests {
    [Theory]
    [InlineData("operation")]
    [InlineData("owner")]
    [InlineData("recognition")]
    [InlineData("meal")]
    [InlineData("version")]
    [InlineData("occurred")]
    [InlineData("saved")]
    [InlineData("window")]
    public void Create_RejectsInvalidIdentityOrTime(string field) {
        bool Is(string value) => string.Equals(field, value, StringComparison.Ordinal);
        Assert.ThrowsAny<ArgumentException>(() => MealRecognitionReceipt.Create(
            Is("operation") ? Guid.Empty : Guid.NewGuid(), Is("owner") ? new UserId(Guid.Empty) : UserId.New(),
            Is("recognition") ? Guid.Empty : Guid.NewGuid(), Is("meal") ? new MealId(Guid.Empty) : MealId.New(),
            Is("version") ? 0u : 42u, Is("occurred") ? DateTime.SpecifyKind(Now, DateTimeKind.Unspecified) : Now,
            Is("saved") ? DateTime.SpecifyKind(Now, DateTimeKind.Local) : Now, Is("window") ? TimeSpan.Zero : TimeSpan.FromDays(1)));
    }

    [Fact]
    public void Undo_RejectsNonUtcClockWithoutChangingReceipt() {
        MealRecognitionReceipt receipt = Create();
        Assert.Throws<ArgumentException>(() => receipt.TryUndo(42, DateTime.SpecifyKind(Now, DateTimeKind.Unspecified)));
        Assert.Null(receipt.UndoneAtUtc);
        Assert.Equal(Now, receipt.SavedAtUtc);
        Assert.Equal(Now.AddDays(1), receipt.UndoUntilUtc);
    }

    private static readonly DateTime Now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Undo_UnchangedMealCanBeCancelledOnlyOnce() {
        MealRecognitionReceipt receipt = Create();
        Assert.Equal(MealRecognitionUndoResult.Undone, receipt.TryUndo(42, Now.AddMinutes(1)));
        Assert.Equal(MealRecognitionUndoResult.AlreadyUndone, receipt.TryUndo(currentMealVersion: null, Now.AddDays(2)));
        Assert.Equal(Now.AddMinutes(1), receipt.UndoneAtUtc);
    }

    [Fact]
    public void Undo_ChangedMealAndExpiredWindowDoNotMutateReceipt() {
        MealRecognitionReceipt receipt = Create();
        Assert.Equal(MealRecognitionUndoResult.Changed, receipt.TryUndo(43, Now.AddMinutes(1)));
        Assert.Null(receipt.UndoneAtUtc);
        Assert.Equal(MealRecognitionUndoResult.Expired, receipt.TryUndo(42, Now.AddDays(1)));
        Assert.Null(receipt.UndoneAtUtc);
    }

    [Fact]
    public void DeletedMeal_LeavesPermanentCancellationReceipt() {
        MealRecognitionReceipt receipt = Create();
        Assert.Equal(MealRecognitionUndoResult.AlreadyDeleted, receipt.TryUndo(currentMealVersion: null, Now));
        Assert.Equal(MealRecognitionUndoResult.AlreadyUndone, receipt.TryUndo(42, Now));
        Assert.NotEqual(Guid.Empty, receipt.MealId.Value);
    }

    [Fact]
    public void DuplicatePayload_MustMatchOwnerRecognitionAndOriginalTime() {
        MealRecognitionReceipt receipt = Create();
        Assert.True(receipt.Matches(receipt.UserId, receipt.RecognitionId, Now));
        Assert.False(receipt.Matches(UserId.New(), receipt.RecognitionId, Now));
        Assert.False(receipt.Matches(receipt.UserId, Guid.NewGuid(), Now));
        Assert.False(receipt.Matches(receipt.UserId, receipt.RecognitionId, Now.AddSeconds(1)));
    }

    private static MealRecognitionReceipt Create() => MealRecognitionReceipt.Create(Guid.NewGuid(), UserId.New(), Guid.NewGuid(),
        MealId.New(), 42, Now, Now, TimeSpan.FromDays(1));
}
