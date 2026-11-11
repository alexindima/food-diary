using System.Text.Json;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.TransferTelegramDiary;

public sealed class TransferTelegramDiaryCommandHandler(
    ITelegramLoginTicketStore tickets,
    IUserTelegramDiaryTransfer transfer,
    ITelegramOperationStore operations,
    IUserAuthenticationIdentityService users,
    PrivilegedSessionService privilegedSessions,
    TimeProvider clock) : ICommandHandler<TransferTelegramDiaryCommand, Result<TelegramDiaryPreview>> {
    internal static readonly Error ExpiredProof = new("Authentication.TelegramDiaryProofExpired",
        "Request a new email confirmation to combine your diaries.", ErrorKind.Conflict);
    private static readonly Error OperationPending = new("Authentication.TelegramDiaryOperationPending",
        "Wait until the current Telegram action finishes.", ErrorKind.Conflict);

    public async Task<Result<TelegramDiaryPreview>> Handle(TransferTelegramDiaryCommand command, CancellationToken cancellationToken) {
        string? payload = await tickets.ReadAsync(command.Ticket, TelegramDiaryTransferAttempt.Purpose, command.BrowserBinding, cancellationToken);
        if (payload is null || JsonSerializer.Deserialize<TelegramDiaryTransferAttempt>(payload) is not { } attempt) {
            return Result.Failure<TelegramDiaryPreview>(ExpiredProof);
        }
        if (attempt.TargetId != command.UserId) {
            return Result.Failure<TelegramDiaryPreview>(new Error("Authentication.TelegramDiaryTargetSignInRequired",
                "Sign in to the existing email account to combine your diaries.", ErrorKind.Forbidden));
        }

        var targetId = new UserId(attempt.TargetId);
        Result<UserAuthenticationPrincipalModel> principal = await users.GetAuthenticationPrincipalAsync(targetId, clock.GetUtcNow().UtcDateTime, cancellationToken);
        if (principal.IsFailure) {
            return Result.Failure<TelegramDiaryPreview>(principal.Error);
        }
        bool adminTarget = principal.Value.Roles.Contains(RoleNames.Admin, StringComparer.Ordinal);
        if (adminTarget && !await privilegedSessions.IsConfirmedAsync(targetId, command.SessionId, allowAdminSession: false, cancellationToken)) {
            return Result.Failure<TelegramDiaryPreview>(IdentityErrors.ReauthenticationRequired);
        }
        if (command.UseTelegramGoals.HasValue && !string.Equals(
            await tickets.ConsumeAsync(command.Ticket, TelegramDiaryTransferAttempt.Purpose, command.BrowserBinding, cancellationToken),
            payload, StringComparison.Ordinal)) {
            return Result.Failure<TelegramDiaryPreview>(ExpiredProof);
        }

        var sourceId = new UserId(attempt.SourceId);
        if (await operations.HasPendingUserAsync(sourceId, cancellationToken)) {
            return Result.Failure<TelegramDiaryPreview>(OperationPending);
        }

        var proof = new TelegramDiaryProof(sourceId, attempt.SourceSecurityVersion,
            targetId, attempt.TargetSecurityVersion, attempt.Email, AdminTargetConfirmed: adminTarget);
        Result<TelegramDiaryPreview> result = await transfer.TransferAsync(proof, command.UseTelegramGoals, cancellationToken);
        if (result.IsSuccess && command.UseTelegramGoals.HasValue) {
            // An operation may have started while the account rows were being locked.
            if (await operations.HasPendingUserAsync(sourceId, cancellationToken)) {
                return Result.Failure<TelegramDiaryPreview>(OperationPending);
            }
            await operations.CancelUserAsync(sourceId, cancellationToken);
        }
        return result;
    }
}
