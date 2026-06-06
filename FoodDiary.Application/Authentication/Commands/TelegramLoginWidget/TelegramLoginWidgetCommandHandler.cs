using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.TelegramLoginWidget;

public sealed class TelegramLoginWidgetCommandHandler(
    IUserRepository userRepository,
    ITelegramLoginWidgetValidator telegramLoginWidgetValidator,
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

        User? user = await userRepository.GetByTelegramUserIdAsync(validationResult.Value.UserId, cancellationToken).ConfigureAwait(false);
        Error? accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AuthenticationModel>(user is null ? Errors.Authentication.TelegramNotLinked : accessError);
        }

        User currentUser = user!;
        IssuedAuthenticationTokens tokens = await authenticationTokenService.IssueAndStoreAsync(currentUser, cancellationToken, command.ClientContext).ConfigureAwait(false);
        return Result.Success(currentUser.ToAuthenticationModel(tokens));
    }
}
