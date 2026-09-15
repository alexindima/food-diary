using FoodDiary.Modules.Users.Application.Abstractions.Models;

namespace FoodDiary.Modules.Users.Application.Abstractions.Common;

public interface IUserCleanupService {
    Task<UserCleanupBatch> CleanupDeletedUsersAsync(
        DateTime olderThanUtc,
        int batchSize,
        Guid? reassignUserId,
        UserCleanupCursor? after = null,
        CancellationToken cancellationToken = default);
}
