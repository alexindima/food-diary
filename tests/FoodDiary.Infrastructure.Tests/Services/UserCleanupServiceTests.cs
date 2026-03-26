using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class UserCleanupServiceTests {
    [Fact]
    public async Task CleanupDeletedUsersAsync_WithNonPositiveBatchSize_Throws() {
        var service = new UserCleanupService(dbContext: null!, logger: NullLogger<UserCleanupService>.Instance);

        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CleanupDeletedUsersAsync(DateTime.UtcNow, 0, reassignUserId: null, CancellationToken.None));

        Assert.Equal("batchSize", ex.ParamName);
    }
}
