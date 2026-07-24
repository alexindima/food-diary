using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.SetAttentionSignalState;

public sealed class SetAttentionSignalStateCommandHandler(
    IDietologistInvitationReadModelRepository invitationRepository,
    IAuditEntryWriter auditWriter,
    IUserContextService userContextService,
    TimeProvider timeProvider)
    : ICommandHandler<SetAttentionSignalStateCommand, Result> {
    public async Task<Result> Handle(
        SetAttentionSignalStateCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> dietologistIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            userContextService,
            cancellationToken).ConfigureAwait(false);
        if (dietologistIdResult.IsFailure) {
            return Result.Failure(dietologistIdResult.Error);
        }

        Result<UserId> clientIdResult = UserIdParser.Parse(
            command.ClientUserId,
            Errors.Validation.Invalid(nameof(command.ClientUserId), "Client user id must not be empty."));
        if (clientIdResult.IsFailure) {
            return Result.Failure(clientIdResult.Error);
        }

        UserId clientId = clientIdResult.Value;
        Result accessResult = await DietologistAccessPolicy.EnsureCanAccessClientReadModelAsync(
            invitationRepository,
            dietologistIdResult.Value,
            clientId,
            cancellationToken).ConfigureAwait(false);
        if (accessResult.IsFailure) {
            return Result.Failure(accessResult.Error);
        }

        bool snooze = string.Equals(command.Action, "Snooze", StringComparison.OrdinalIgnoreCase);
        DateTime? snoozedUntilUtc = command.SnoozedUntilUtc?.ToUniversalTime();
        if (snooze && snoozedUntilUtc <= timeProvider.GetUtcNow().UtcDateTime) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(command.SnoozedUntilUtc),
                "Snooze end must be in the future."));
        }

        string? metadata = snoozedUntilUtc?.ToString("O", CultureInfo.InvariantCulture);
        await auditWriter.AddAsync(
            dietologistIdResult.Value,
            clientId.Value,
            snooze ? "dietologist.attention.snoozed" : "dietologist.attention.acknowledged",
            "AttentionSignal",
            command.SignalId,
            metadata,
            cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
