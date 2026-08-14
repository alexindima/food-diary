using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Services;

namespace FoodDiary.Application.Identity.Authentication.Commands.TelegramLoginWidget;

public sealed class TelegramLoginWidgetCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    TimeProvider dateTimeProvider,
    ITelegramLoginWidgetValidator telegramLoginWidgetValidator,
    ITelegramAssertionReplayGuard replayGuard,
    IAuthenticationTokenService authenticationTokenService) : ICommandHandler<TelegramLoginWidgetCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(TelegramLoginWidgetCommand command, CancellationToken cancellationToken) {
        var widgetData = new TelegramLoginWidgetData(
            command.Id,
            command.AuthDate,
            command.Hash,
            command.Username,
            command.FirstName,
            command.LastName,
            command.PhotoUrl);

        Result<TelegramInitData> validationResult = telegramLoginWidgetValidator.ValidateLoginWidget(widgetData);
        if (!validationResult.IsSuccess) {
            return Result.Failure<AuthenticationModel>(validationResult.Error);
        }

        bool consumed = await replayGuard
            .TryConsumeAsync("widget:" + command.Hash, validationResult.Value.AuthDateUtc.AddDays(1), cancellationToken)
            .ConfigureAwait(false);
        if (!consumed) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.TelegramAssertionAlreadyUsed);
        }

        Result<UserAuthenticationPrincipalModel> authenticationResult = await userIdentityService
            .AuthenticateTelegramAsync(
                validationResult.Value.UserId,
                dateTimeProvider.GetUtcNow().UtcDateTime,
                cancellationToken)
            .ConfigureAwait(false);
        if (authenticationResult.IsFailure) {
            return Result.Failure<AuthenticationModel>(authenticationResult.Error);
        }

        UserAuthenticationPrincipalModel principal = authenticationResult.Value;
        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueFromPrincipalAsync(principal, cancellationToken, command.ClientContext)
            .ConfigureAwait(false);
        return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User));
    }
}
