using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.BeginTelegramMiniApp;

public sealed class BeginTelegramMiniAppCommandHandler(
    ITelegramAuthValidator validator,
    ITelegramAssertionReplayGuard replayGuard,
    ITelegramIdentityPolicy policy,
    TelegramAuthenticationIntentService intents) : ICommandHandler<BeginTelegramMiniAppCommand, Result<TelegramAuthenticationIntentModel>> {
    public async Task<Result<TelegramAuthenticationIntentModel>> Handle(BeginTelegramMiniAppCommand command, CancellationToken cancellationToken) {
        if (!policy.LoginEnabled) {
            return Result.Failure<TelegramAuthenticationIntentModel>(TelegramIdentityErrors.NotConfigured);
        }
        Result<TelegramInitData> proof = validator.ValidateInitData(command.InitData);
        if (proof.IsFailure) {
            return Result.Failure<TelegramAuthenticationIntentModel>(proof.Error);
        }
        if (!await replayGuard.TryConsumeAsync(command.InitData, proof.Value.AuthDateUtc.AddDays(1), cancellationToken).ConfigureAwait(false)) {
            return Result.Failure<TelegramAuthenticationIntentModel>(TelegramIdentityErrors.InvalidProof);
        }
        return await intents.CreateAsync(proof.Value.UserId, proof.Value.FirstName, proof.Value.LastName,
            proof.Value.LanguageCode, command.BrowserBinding, command.LinkUserId, cancellationToken).ConfigureAwait(false);
    }
}
