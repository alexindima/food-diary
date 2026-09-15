namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserCleanupService {
    Task<int> CleanupDeletedUsersAsync(
        DateTime olderThanUtc,
        int batchSize,
        Guid? reassignUserId,
        CancellationToken cancellationToken = default);
}
