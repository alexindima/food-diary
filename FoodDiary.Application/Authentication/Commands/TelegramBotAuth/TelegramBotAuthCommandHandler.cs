using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.TelegramBotAuth;

public sealed class TelegramBotAuthCommandHandler(
    IAuthenticationUserLookupService userLookupService,
    IAuthenticationTokenService authenticationTokenService) : ICommandHandler<TelegramBotAuthCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(TelegramBotAuthCommand command, CancellationToken cancellationToken) {
        User? user = await userLookupService.GetByTelegramUserIdAsync(command.TelegramUserId, cancellationToken).ConfigureAwait(false);
        Error? accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AuthenticationModel>(user is null ? Errors.Authentication.TelegramNotLinked : accessError);
        }

        User currentUser = user!;
        IssuedAuthenticationTokens tokens = await authenticationTokenService.IssueAndStoreAsync(currentUser, cancellationToken, command.ClientContext).ConfigureAwait(false);
        return Result.Success(currentUser.ToAuthenticationModel(tokens));
    }
}
