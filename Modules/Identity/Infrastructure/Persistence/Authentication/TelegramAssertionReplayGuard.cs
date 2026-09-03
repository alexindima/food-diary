using System.Security.Cryptography;
using System.Text;
using FoodDiary.Application.Abstractions.Authentication.Common;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Authentication;

public sealed class TelegramAssertionReplayGuard(FoodDiaryDbContext context, TimeProvider timeProvider)
    : ITelegramAssertionReplayGuard {
    public async Task<bool> TryConsumeAsync(
        string signedAssertion,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default) {
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        await context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"ConsumedTelegramAssertions\" WHERE \"ExpiresAtUtc\" <= {nowUtc}",
                cancellationToken)
            .ConfigureAwait(false);
        string fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(signedAssertion)));
        int inserted = await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"ConsumedTelegramAssertions\" (\"Fingerprint\", \"ExpiresAtUtc\") VALUES ({fingerprint}, {expiresAtUtc}) ON CONFLICT (\"Fingerprint\") DO NOTHING",
                cancellationToken)
            .ConfigureAwait(false);
        return inserted == 1;
    }
}
