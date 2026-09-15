using FoodDiary.Mediator;
using FoodDiary.Modules.Fasting.Contracts.Commands.CleanupFastingTelemetry;

namespace FoodDiary.Modules.Fasting.Application.Commands.CleanupFastingTelemetry;

public sealed class CleanupFastingTelemetryCommandHandler(IFastingTelemetryEventWriteRepository repository) : IRequestHandler<CleanupFastingTelemetryCommand, int> {
    public async Task<int> Handle(CleanupFastingTelemetryCommand request, CancellationToken cancellationToken) {
        DateTime olderThanUtc = request.OlderThanUtc;
        int batchSize = request.BatchSize;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.BatchSize, nameof(request));
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
