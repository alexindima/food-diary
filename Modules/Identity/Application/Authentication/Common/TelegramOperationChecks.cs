using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using System.Text;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Common;

internal static class TelegramOperationChecks {
    internal static readonly Error Unavailable = new("Telegram.OperationsUnavailable", "Telegram operations are disabled.", ErrorKind.Conflict);
    internal static readonly Error Conflict = new("Telegram.OperationConflict", "The operation cannot be processed with this identity or lease.", ErrorKind.Conflict);
    internal static readonly Error Invalid = new("Telegram.InvalidOperation", "Invalid Telegram operation input.", ErrorKind.Validation);
    internal static bool IsEnabled(ITelegramOperationPolicy policy) => policy.OperationsEnabled && policy.BotId > 0;

    internal static async Task<bool> IsCurrentAsync(TelegramOperationLease lease, ITelegramOperationStore store, ITelegramOperationPolicy policy, IUserAuthenticationIdentityService identities, TimeProvider timeProvider, CancellationToken cancellationToken) {
        Result<UserAuthenticationPrincipalModel> principal = await identities.GetAuthenticationPrincipalAsync(new UserId(lease.UserId),
            timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
        if (principal.IsSuccess && principal.Value.User.HasTelegramIdentity && principal.Value.SecurityVersion == lease.SecurityVersion) {
            return true;
        }
        await store.CancelOperationAsync(policy.BotId, lease.OperationId, cancellationToken).ConfigureAwait(false);
        return false;
    }

    internal static bool ValidPayload(string? payload) => !string.IsNullOrWhiteSpace(payload) && Encoding.UTF8.GetByteCount(payload) <= 32768;
}
