namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserCleanupService {
    Task<int> CleanupDeletedUsersAsync(
        DateTime olderThanUtc,
        int batchSize,
        Guid? reassignUserId,
        CancellationToken cancellationToken = default);
}
