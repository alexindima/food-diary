using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Authentication.Commands.CleanupSessions;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.CleanupSessions;

public sealed class CleanupSessionsCommandHandler(IRefreshTokenSessionWriteRepository repository)
    : IRequestHandler<CleanupSessionsCommand, int> {
    public async Task<int> Handle(CleanupSessionsCommand request, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.BatchSize, nameof(request));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.BatchSize, 1000, nameof(request));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.MaxBatches, nameof(request));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.MaxBatches, 100, nameof(request));
        int total = 0;
        for (int batch = 0; batch < request.MaxBatches; batch++) {
            cancellationToken.ThrowIfCancellationRequested();
            int deleted = await repository.DeleteInactiveBeforeAsync(request.OlderThanUtc, request.BatchSize, cancellationToken).ConfigureAwait(false);
            total += deleted;
            if (deleted < request.BatchSize) {
                break;
            }
        }
        return total;
    }
}
