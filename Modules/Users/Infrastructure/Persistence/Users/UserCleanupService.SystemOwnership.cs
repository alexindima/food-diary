using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed partial class UserCleanupService {
    private static IReadOnlyList<IUserContentRetentionParticipant> ValidateContentRetentionParticipants(
        IEnumerable<IUserContentRetentionParticipant> participants) {
        IUserContentRetentionParticipant[] ordered = [.. participants.OrderBy(participant => participant.Order)];
        if (ordered.Select(participant => participant.Order).Distinct().Count() != ordered.Length) {
            throw new InvalidOperationException("Content retention participants require unique ordering.");
        }
        return ordered;
    }

    private async Task<UserId?> ResolveRetainedContentOwnerAsync(UserId sourceUserId, CancellationToken cancellationToken) {
        foreach (IUserContentRetentionParticipant participant in _contentRetentionParticipants) {
            if (!await participant.HasReferencedContentAsync(sourceUserId, cancellationToken)) {
                continue;
            }

            // Provision the permanent infrastructure principal before content FK updates begin.
            // This independent coordinated transaction may survive a later content purge rollback.
            await new SystemContentOwnerProvisioner(dbContext, transactionCoordinator).EnsureAsync(cancellationToken);
            return SystemContentOwner.Id;
        }
        return null;
    }
}
