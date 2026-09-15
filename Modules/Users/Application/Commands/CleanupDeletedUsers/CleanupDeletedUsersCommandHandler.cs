using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Contracts.Commands.CleanupDeletedUsers;

namespace FoodDiary.Modules.Users.Application.Commands.CleanupDeletedUsers;

public sealed class CleanupDeletedUsersCommandHandler(IUserCleanupService cleanup) : IRequestHandler<CleanupDeletedUsersCommand, int> {
    public async Task<int> Handle(CleanupDeletedUsersCommand request, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.BatchSize, nameof(request));
        UserCleanupCursor? after = null;
        int removed = 0;
        while (true) {
            cancellationToken.ThrowIfCancellationRequested();
            UserCleanupBatch batch = await cleanup.CleanupDeletedUsersAsync(
                request.OlderThanUtc, request.BatchSize, request.ReassignUserId, after, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            removed = checked(removed + batch.RemovedCount);
            if (batch.LastExamined is null) {
                return removed;
            }
            after = batch.LastExamined;
        }
    }
}
