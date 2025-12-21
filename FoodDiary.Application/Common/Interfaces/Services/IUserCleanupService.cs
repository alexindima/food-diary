namespace FoodDiary.Application.Common.Interfaces.Services;

public interface IUserCleanupService
{
    Task<int> CleanupDeletedUsersAsync(
        DateTime olderThanUtc,
        int batchSize,
        Guid? reassignUserId,
        CancellationToken cancellationToken = default);
}
