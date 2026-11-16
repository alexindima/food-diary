using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed class SystemContentOwnerProvisioner(UsersDbContext dbContext, IModuleTransactionCoordinator transactionCoordinator) {
    private const long SystemContentOwnerProvisioningLock = 731458329112;

    public async Task EnsureAsync(CancellationToken cancellationToken) {
        await transactionCoordinator.ExecuteItemAsync(async (transaction, token) => {
            await dbContext.Database.UseTransactionAsync(transaction, token);
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({SystemContentOwnerProvisioningLock})", token);
            User? existing = await dbContext.Users.AsNoTracking().Include(user => user.UserRoles)
                .SingleOrDefaultAsync(user => user.Id == SystemContentOwner.Id, token);
            if (existing is not null) {
                ValidateSystemContentOwner(existing);
                return false;
            }

            dbContext.Users.Add(User.CreateSystemContentOwner());
            return true;
        }, cancellationToken);
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
