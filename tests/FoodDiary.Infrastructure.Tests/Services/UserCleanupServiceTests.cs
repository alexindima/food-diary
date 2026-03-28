using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using FoodDiary.Application.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class UserCleanupServiceTests {
    [Fact]
    public async Task CleanupDeletedUsersAsync_WithNonPositiveBatchSize_Throws() {
        var service = new UserCleanupService(dbContext: null!, imageStorageService: new NoOpImageStorageService(), logger: NullLogger<UserCleanupService>.Instance);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CleanupDeletedUsersAsync(DateTime.UtcNow, 0, reassignUserId: null, CancellationToken.None));

        Assert.Equal("batchSize", ex.ParamName);
    }

    private sealed class NoOpImageStorageService : IImageStorageService {
        public Task<PresignedUpload> CreatePresignedUploadAsync(
            UserId userId,
            string fileName,
            string contentType,
            long fileSizeBytes,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
