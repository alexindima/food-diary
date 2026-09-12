using System.Text;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Services;

public sealed class TelegramOperationService(ITelegramOperationStore store, ITelegramOperationPolicy policy,
    IUserAuthenticationIdentityService identities, TimeProvider timeProvider) {
    private static readonly Error Unavailable = new("Telegram.OperationsUnavailable", "Telegram operations are disabled.", ErrorKind.Conflict);
    private static readonly Error Conflict = new("Telegram.OperationConflict", "The operation cannot be processed with this identity or lease.", ErrorKind.Conflict);
    private static readonly Error Invalid = new("Telegram.InvalidOperation", "Invalid Telegram operation input.", ErrorKind.Validation);
    private bool Enabled => policy.OperationsEnabled && policy.BotId > 0;

    public async Task<Result<Guid>> RegisterAsync(long updateId, long telegramUserId, string payload, CancellationToken cancellationToken) {
        if (!Enabled) {
            return Result.Failure<Guid>(Unavailable);
        }
        if (updateId < 0 || telegramUserId <= 0 || !ValidPayload(payload)) {
            return Result.Failure<Guid>(Invalid);
        }
        Result<UserAuthenticationPrincipalModel> principal = await identities.AuthenticateTelegramAsync(telegramUserId,
            timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
        if (principal.IsFailure) {
            return Result.Failure<Guid>(principal.Error);
        }
        Guid? id = await store.RegisterAsync(policy.BotId, updateId, principal.Value.UserId.Value,
            principal.Value.SecurityVersion, payload, cancellationToken).ConfigureAwait(false);
        return id.HasValue ? Result.Success(id.Value) : Result.Failure<Guid>(Conflict);
    }

    public async Task<Result<IReadOnlyList<Guid>>> ListReadyAsync(CancellationToken cancellationToken) =>
        Enabled ? Result.Success(await store.ListReadyAsync(policy.BotId, cancellationToken).ConfigureAwait(false)) :
            Result.Failure<IReadOnlyList<Guid>>(Unavailable);

    public async Task<Result<TelegramOperationLease>> AcquireAsync(Guid operationId, CancellationToken cancellationToken) {
        if (!Enabled) {
            return Result.Failure<TelegramOperationLease>(Unavailable);
        }
        TelegramOperationLease? lease = await store.AcquireAsync(policy.BotId, operationId, cancellationToken).ConfigureAwait(false);
        if (lease is null || !await IsCurrentAsync(lease, cancellationToken).ConfigureAwait(false)) {
            return Result.Failure<TelegramOperationLease>(Conflict);
        }
        return Result.Success(lease);
    }

    public async Task<Result> CheckpointAsync(Guid operationId, Guid leaseId, string checkpoint, bool completed,
        DateTime nextAttemptAtUtc, CancellationToken cancellationToken) {
        if (!Enabled) {
            return Result.Failure(Unavailable);
        }
        if (!ValidPayload(checkpoint) || nextAttemptAtUtc.Kind != DateTimeKind.Utc || nextAttemptAtUtc > timeProvider.GetUtcNow().UtcDateTime.AddDays(1)) {
            return Result.Failure(Invalid);
        }
        TelegramOperationLease? lease = await store.GetLeaseAsync(policy.BotId, operationId, leaseId, cancellationToken).ConfigureAwait(false);
        if (lease is null || !await IsCurrentAsync(lease, cancellationToken).ConfigureAwait(false)) {
            return Result.Failure(Conflict);
        }
        return await store.CheckpointAsync(policy.BotId, operationId, leaseId, checkpoint, completed, nextAttemptAtUtc,
            cancellationToken).ConfigureAwait(false) ? Result.Success() : Result.Failure(Conflict);
    }

    private async Task<bool> IsCurrentAsync(TelegramOperationLease lease, CancellationToken cancellationToken) {
        Result<UserAuthenticationPrincipalModel> principal = await identities.GetAuthenticationPrincipalAsync(new UserId(lease.UserId),
            timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
        if (principal.IsSuccess && principal.Value.User.HasTelegramIdentity && principal.Value.SecurityVersion == lease.SecurityVersion) {
            return true;
        }
        await store.CancelOperationAsync(policy.BotId, lease.OperationId, cancellationToken).ConfigureAwait(false);
        return false;
    }

    private static bool ValidPayload(string? payload) => !string.IsNullOrWhiteSpace(payload) && Encoding.UTF8.GetByteCount(payload) <= 32768;
}
