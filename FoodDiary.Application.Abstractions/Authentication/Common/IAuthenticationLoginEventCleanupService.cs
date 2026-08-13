namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface IAuthenticationLoginEventCleanupService {
    Task<int> CleanupAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken);
}
