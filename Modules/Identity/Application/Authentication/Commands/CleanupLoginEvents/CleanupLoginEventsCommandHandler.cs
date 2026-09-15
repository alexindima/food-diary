using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Contracts.Authentication.Commands.CleanupLoginEvents;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.CleanupLoginEvents;

public sealed class CleanupLoginEventsCommandHandler(IUserLoginEventWriteRepository repository)
    : IRequestHandler<CleanupLoginEventsCommand, int> {
    public async Task<int> Handle(CleanupLoginEventsCommand request, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.BatchSize, nameof(request));
        DateTime olderThanUtc = request.OlderThanUtc;
        int batchSize = request.BatchSize;
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
