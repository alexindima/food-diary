using System.Text.Json;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.TransferTelegramDiary;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.VerifyTelegramDiaryEmail;

public sealed class VerifyTelegramDiaryEmailCommandHandler(
    TelegramBackupEmailService email,
    IUserTelegramAccountService accounts,
    IUserTelegramDiaryTransfer transfer,
    ITelegramLoginTicketStore tickets,
    TimeProvider timeProvider) : ICommandHandler<VerifyTelegramDiaryEmailCommand, Result<TelegramDiaryEmailVerification>> {
    public async Task<Result<TelegramDiaryEmailVerification>> Handle(VerifyTelegramDiaryEmailCommand command, CancellationToken cancellationToken) {
        TelegramBackupEmailService.EmailProof? proof = await email.ConsumeProofAsync(command.UserId, command.Token, cancellationToken);
        if (proof is null || command.BrowserBinding.Length != 43) {
            return Result.Failure<TelegramDiaryEmailVerification>(TransferTelegramDiaryCommandHandler.ExpiredProof);
        }

        var sourceId = new UserId(proof.UserId);
        Result added = await accounts.AddVerifiedEmailAsync(sourceId, proof.Email, proof.SecurityVersion, cancellationToken);
        if (added.IsSuccess) {
            return Result.Success(new TelegramDiaryEmailVerification(TransferTicket: null));
        }
        if (!string.Equals(added.Error.Code, UserErrors.EmailAlreadyExists.Code, StringComparison.Ordinal)) {
            return Result.Failure<TelegramDiaryEmailVerification>(added.Error);
        }

        TelegramDiaryTarget? target = await transfer.FindTargetAsync(proof.Email, cancellationToken);
        if (target is null || target.UserId == sourceId) {
            return Result.Failure<TelegramDiaryEmailVerification>(added.Error);
        }

        var attempt = new TelegramDiaryTransferAttempt(sourceId.Value, proof.SecurityVersion,
            target.UserId.Value, target.SecurityVersion, proof.Email);
        string ticket = await tickets.CreateAsync(TelegramDiaryTransferAttempt.Purpose, command.BrowserBinding,
            JsonSerializer.Serialize(attempt), timeProvider.GetUtcNow().UtcDateTime.AddMinutes(15), cancellationToken);
        return Result.Success(new TelegramDiaryEmailVerification(ticket));
    }
}
