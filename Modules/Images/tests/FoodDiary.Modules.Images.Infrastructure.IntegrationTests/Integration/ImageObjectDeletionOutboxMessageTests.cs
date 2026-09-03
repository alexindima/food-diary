using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Images;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[ExcludeFromCodeCoverage]
public sealed class ImageObjectDeletionOutboxMessageTests {
    [Fact]
    public void Create_WithBlankObjectKey_Throws() {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ImageObjectDeletionOutboxMessage.Create(" ", DateTime.UtcNow));

        Assert.Equal("objectKey", ex.ParamName);
    }

    [Fact]
    public void Create_WithTooLongObjectKey_Throws() {
        string objectKey = new('a', 1025);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImageObjectDeletionOutboxMessage.Create(objectKey, DateTime.UtcNow));

        Assert.Equal("objectKey", ex.ParamName);
    }

    [Fact]
    public void Create_TrimsObjectKeyAndNormalizesLocalDate() {
        var localDate = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Local);

        var message = ImageObjectDeletionOutboxMessage.Create(" users/test/image.webp ", localDate);

        Assert.Multiple(
            () => Assert.Equal("users/test/image.webp", message.ObjectKey),
            () => Assert.Equal(DateTimeKind.Utc, message.CreatedOnUtc.Kind),
            () => Assert.Equal(message.CreatedOnUtc, message.NextAttemptOnUtc));
    }

    [Fact]
    public void MarkFailed_WithBlankError_ClearsLastError() {
        var message = ImageObjectDeletionOutboxMessage.Create("users/test/image.webp", DateTime.UtcNow);

        message.MarkFailed(" ", DateTime.UtcNow.AddMinutes(1));

        Assert.Multiple(
            () => Assert.Equal(1, message.AttemptCount),
            () => Assert.Null(message.LastError));
    }

    [Fact]
    public void MarkDeadLettered_ClearsLockAndStoresTrimmedError() {
        var message = ImageObjectDeletionOutboxMessage.Create("users/test/image.webp", DateTime.UtcNow);
        message.MarkClaimed(DateTime.UtcNow.AddMinutes(5), "worker");

        message.MarkDeadLettered(" final failure ", DateTime.UtcNow.AddMinutes(1));

        Assert.Multiple(
            () => Assert.Equal(1, message.AttemptCount),
            () => Assert.NotNull(message.DeadLetteredOnUtc),
            () => Assert.Null(message.LockedUntilUtc),
            () => Assert.Null(message.LockedBy),
            () => Assert.Equal("final failure", message.LastError));
    }

    [Fact]
    public void MarkReplayed_ClearsDeadLetterAndLockState() {
        DateTime now = DateTime.UtcNow;
        var message = ImageObjectDeletionOutboxMessage.Create("users/test/image.webp", now.AddMinutes(-2));
        message.MarkClaimed(now.AddMinutes(5), "worker");
        message.MarkDeadLettered("failure", now.AddMinutes(-1));

        message.MarkReplayed(now);

        Assert.Multiple(
            () => Assert.Equal(now, message.NextAttemptOnUtc),
            () => Assert.Null(message.DeadLetteredOnUtc),
            () => Assert.Null(message.LockedUntilUtc),
            () => Assert.Null(message.LockedBy),
            () => Assert.Null(message.LastError));
    }

    [Fact]
    public void Create_UnconfirmedDeletion_UsesImagesModelAndPreservesConfirmationFlag() {
        var message = ImageObjectDeletionOutboxMessage.Create("images/pending.webp", isConfirmed: false, DateTime.UtcNow);

        Assert.Multiple(
            () => Assert.False(message.IsConfirmed),
            () => Assert.Same(typeof(ImagesPersistenceModelBuilderExtensions).Assembly, message.GetType().Assembly));
    }
}
