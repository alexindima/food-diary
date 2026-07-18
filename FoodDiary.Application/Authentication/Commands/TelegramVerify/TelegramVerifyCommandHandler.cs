using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.TelegramVerify;

public sealed class TelegramVerifyCommandHandler(
    IAuthenticationUserLookupService userLookupService,
    ITelegramAuthValidator telegramAuthValidator,
    ITelegramAssertionReplayGuard replayGuard,
    IAuthenticationTokenService authenticationTokenService) : ICommandHandler<TelegramVerifyCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(TelegramVerifyCommand command, CancellationToken cancellationToken) {
        Result<TelegramInitData> initDataResult = telegramAuthValidator.ValidateInitData(command.InitData);
        if (!initDataResult.IsSuccess) {
            return Result.Failure<AuthenticationModel>(initDataResult.Error);
        }

        TelegramInitData initData = initDataResult.Value;
        bool consumed = await replayGuard
            .TryConsumeAsync(command.InitData, initData.AuthDateUtc.AddDays(1), cancellationToken)
            .ConfigureAwait(false);
        if (!consumed) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.TelegramAssertionAlreadyUsed);
        }

        User? user = await userLookupService.GetByTelegramUserIdAsync(initData.UserId, cancellationToken).ConfigureAwait(false);
        Error? accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AuthenticationModel>(user is null ? Errors.Authentication.TelegramNotLinked : accessError);
        }

        User currentUser = user!;
        IssuedAuthenticationTokens tokens = await authenticationTokenService.IssueAndStoreAsync(currentUser, cancellationToken, command.ClientContext).ConfigureAwait(false);
        return Result.Success(currentUser.ToAuthenticationModel(tokens));
    }
}
