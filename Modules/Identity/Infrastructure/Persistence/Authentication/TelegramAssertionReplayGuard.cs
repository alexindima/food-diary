using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Identity.Infrastructure.Persistence.Authentication;

public sealed class TelegramAssertionReplayGuard(IdentityDbContext context, TimeProvider timeProvider, Func<CancellationToken, Task>? synchronizeTransactionAsync = null)
    : ITelegramAssertionReplayGuard {
    public async Task<bool> TryConsumeAsync(
        string signedAssertion,
        DateTime expiresAtUtc,
        DateTime authDateUtc,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        DateTime notBeforeUtc = await context.TelegramAssertionPolicies
            .AsNoTracking().Where(policy => policy.Id == 1).Select(policy => policy.NotBeforeUtc)
            .SingleAsync(cancellationToken).ConfigureAwait(false);
        if (authDateUtc < notBeforeUtc) {
            return false;
        }
        await context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"ConsumedTelegramAssertions\" WHERE \"ExpiresAtUtc\" <= {nowUtc}",
                cancellationToken)
            .ConfigureAwait(false);
        string fingerprint = "v2:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(signedAssertion)));
        int inserted = await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"ConsumedTelegramAssertions\" (\"Fingerprint\", \"ExpiresAtUtc\") VALUES ({fingerprint}, {expiresAtUtc}) ON CONFLICT (\"Fingerprint\") DO NOTHING",
                cancellationToken)
            .ConfigureAwait(false);
        return inserted == 1;
    }
}
