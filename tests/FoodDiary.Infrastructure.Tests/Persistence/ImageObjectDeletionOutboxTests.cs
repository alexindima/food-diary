using System.Globalization;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Images;
using FoodDiary.Infrastructure.Options;
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
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
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
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            TimeProvider.System,
            NullLogger<ImageObjectDeletionOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        Assert.Equal(0, processed);
        ImageObjectDeletionOutboxMessage message = Assert.Single(context.ImageObjectDeletionOutbox);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(1, message.AttemptCount);
        Assert.True(message.NextAttemptOnUtc > DateTime.UtcNow);
        Assert.Equal("Outbox dispatch failed (InvalidOperationException).", message.LastError);
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
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            TimeProvider.System,
            NullLogger<ImageObjectDeletionOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        Assert.Equal(0, processed);
        ImageObjectDeletionOutboxMessage saved = Assert.Single(context.ImageObjectDeletionOutbox);
        Assert.Equal(10, saved.AttemptCount);
        Assert.NotNull(saved.DeadLetteredOnUtc);
        Assert.Null(saved.LockedUntilUtc);
        Assert.Null(saved.LockedBy);
        Assert.Equal("Outbox dispatch failed (InvalidOperationException).", saved.LastError);
    }

    [Fact]
    public async Task ProcessDueAsync_WhenBatchSizeIsNotPositive_ReturnsZero() {
        await using FoodDiaryDbContext context = CreateContext();
        var processor = new ImageObjectDeletionOutboxProcessor(
            context,
            new RecordingImageStorageService(),
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            TimeProvider.System,
            NullLogger<ImageObjectDeletionOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 0, CancellationToken.None);

        Assert.Equal(0, processed);
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
