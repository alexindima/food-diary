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
