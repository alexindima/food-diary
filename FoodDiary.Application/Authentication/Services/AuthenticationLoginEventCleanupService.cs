using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Authentication.Common;

namespace FoodDiary.Application.Authentication.Services;

public sealed class AuthenticationLoginEventCleanupService(IUserLoginEventWriteRepository repository)
    : IAuthenticationLoginEventCleanupService {
    public async Task<int> CleanupAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken) {
        int totalDeletedCount = 0;
        int deletedCount;
        do {
            cancellationToken.ThrowIfCancellationRequested();
            deletedCount = await repository
                .DeleteOlderThanAsync(olderThanUtc, batchSize, cancellationToken)
                .ConfigureAwait(false);
            totalDeletedCount += deletedCount;
        } while (deletedCount == batchSize);

        return totalDeletedCount;
    }
}
