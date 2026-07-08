using System.Globalization;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Images;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class ImageObjectDeletionOutboxTests {
    [Fact]
    public async Task EnqueueAsync_PersistsDueMessage() {
        await using FoodDiaryDbContext context = CreateContext();
        var outbox = new ImageObjectDeletionOutbox(context, TimeProvider.System);

        await outbox.EnqueueAsync("users/test/image.webp", CancellationToken.None);
        await context.SaveChangesAsync();

        ImageObjectDeletionOutboxMessage message = Assert.Single(context.ImageObjectDeletionOutbox);
        Assert.Equal("users/test/image.webp", message.ObjectKey);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(0, message.AttemptCount);
    }

    [Fact]
    public async Task ProcessDueAsync_WhenDeleteSucceeds_MarksMessageProcessed() {
        await using FoodDiaryDbContext context = CreateContext();
        context.ImageObjectDeletionOutbox.Add(ImageObjectDeletionOutboxMessage.Create("users/test/image.webp", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var storage = new RecordingImageStorageService();
        var processor = new ImageObjectDeletionOutboxProcessor(
            context,
            storage,
            TimeProvider.System,
            NullLogger<ImageObjectDeletionOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal(["users/test/image.webp"], storage.DeletedObjectKeys);
        ImageObjectDeletionOutboxMessage message = Assert.Single(context.ImageObjectDeletionOutbox);
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.Null(message.LastError);
    }

    [Fact]
    public async Task ProcessDueAsync_WhenDeleteFails_SchedulesRetry() {
        await using FoodDiaryDbContext context = CreateContext();
        context.ImageObjectDeletionOutbox.Add(ImageObjectDeletionOutboxMessage.Create("users/test/fail.webp", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var processor = new ImageObjectDeletionOutboxProcessor(
            context,
            new ThrowingImageStorageService(),
            TimeProvider.System,
            NullLogger<ImageObjectDeletionOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        Assert.Equal(0, processed);
        ImageObjectDeletionOutboxMessage message = Assert.Single(context.ImageObjectDeletionOutbox);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(1, message.AttemptCount);
        Assert.True(message.NextAttemptOnUtc > DateTime.UtcNow);
        Assert.Contains("Simulated", message.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessDueAsync_WhenMaxAttemptReached_DeadLettersMessage() {
        await using FoodDiaryDbContext context = CreateContext();
        var message = ImageObjectDeletionOutboxMessage.Create("users/test/dead-letter.webp", DateTime.UtcNow.AddMinutes(-1));
        for (int i = 0; i < 9; i++) {
            message.MarkFailed(string.Create(CultureInfo.InvariantCulture, $"failure {i}"), DateTime.UtcNow.AddMinutes(-1));
        }

        context.ImageObjectDeletionOutbox.Add(message);
        await context.SaveChangesAsync();
        var processor = new ImageObjectDeletionOutboxProcessor(
            context,
            new ThrowingImageStorageService(),
            TimeProvider.System,
            NullLogger<ImageObjectDeletionOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        Assert.Equal(0, processed);
        ImageObjectDeletionOutboxMessage saved = Assert.Single(context.ImageObjectDeletionOutbox);
        Assert.Equal(10, saved.AttemptCount);
        Assert.NotNull(saved.DeadLetteredOnUtc);
        Assert.Null(saved.LockedUntilUtc);
        Assert.Null(saved.LockedBy);
        Assert.Contains("Simulated", saved.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessDueAsync_WhenBatchSizeIsNotPositive_ReturnsZero() {
        await using FoodDiaryDbContext context = CreateContext();
        var processor = new ImageObjectDeletionOutboxProcessor(
            context,
            new RecordingImageStorageService(),
            TimeProvider.System,
            NullLogger<ImageObjectDeletionOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 0, CancellationToken.None);

        Assert.Equal(0, processed);
    }

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

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingImageStorageService : IImageStorageService {
        public List<string> DeletedObjectKeys { get; } = [];

        public Task<PresignedUpload> CreatePresignedUploadAsync(
            FoodDiary.Domain.ValueObjects.Ids.UserId userId,
            string fileName,
            string contentType,
            long fileSizeBytes,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) {
            DeletedObjectKeys.Add(objectKey);
            return Task.CompletedTask;
        }

        public Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(string objectKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingImageStorageService : IImageStorageService {
        public Task<PresignedUpload> CreatePresignedUploadAsync(
            FoodDiary.Domain.ValueObjects.Ids.UserId userId,
            string fileName,
            string contentType,
            long fileSizeBytes,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("Simulated storage failure."));

        public Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(string objectKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
