using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed partial class UserCleanupService {
    private const long SystemContentOwnerProvisioningLock = 731458329112;

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
            if (!await participant.HasReferencedContentAsync(sourceUserId, cancellationToken).ConfigureAwait(false)) {
                continue;
            }

            // Provision the permanent infrastructure principal before content FK updates begin.
            // This independent coordinated transaction may survive a later content purge rollback.
            await EnsureSystemContentOwnerAsync(cancellationToken).ConfigureAwait(false);
            return SystemContentOwner.Id;
        }
        return null;
    }

    private async Task EnsureSystemContentOwnerAsync(CancellationToken cancellationToken) {
        await transactionCoordinator.ExecuteItemAsync(async (transaction, token) => {
            await dbContext.Database.UseTransactionAsync(transaction, token).ConfigureAwait(false);
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({SystemContentOwnerProvisioningLock})", token).ConfigureAwait(false);
            User? existing = await dbContext.Users.AsNoTracking().Include(user => user.UserRoles)
                .SingleOrDefaultAsync(user => user.Id == SystemContentOwner.Id, token).ConfigureAwait(false);
            if (existing is not null) {
                ValidateSystemContentOwner(existing);
                return false;
            }

            dbContext.Users.Add(User.CreateSystemContentOwner());
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateSystemContentOwner(User user) {
        if (user.IsActive || user.DeletedAt is not null || user.Email is not null || user.HasPassword ||
            user.Password.Length != 0 || user.MustChangePassword || user.IsEmailConfirmed ||
            user.GoogleIssuer is not null || user.GoogleSubject is not null || user.TelegramUserId is not null ||
            user.TelegramOidcIssuer is not null || user.TelegramOidcSubject is not null || user.TelegramLinkVerifiedForRecovery ||
            user.EmailConfirmationTokenHash is not null || user.EmailConfirmationTokenExpiresAtUtc is not null ||
            user.EmailConfirmationSentAtUtc is not null || user.PasswordResetTokenHash is not null ||
            user.PasswordResetTokenExpiresAtUtc is not null || user.PasswordResetSentAtUtc is not null || user.LastLoginAtUtc is not null ||
            user.Username is not null || user.FirstName is not null || user.LastName is not null ||
            user.ProfileImage is not null || user.ProfileImageAssetId is not null || user.UserRoles.Count != 0) {
            throw new InvalidOperationException("The reserved content owner identity is occupied by a different account state.");
        }
    }
}
