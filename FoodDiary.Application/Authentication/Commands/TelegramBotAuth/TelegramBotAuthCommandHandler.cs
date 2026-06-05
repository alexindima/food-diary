using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Authentication.Services;

namespace FoodDiary.Application.Authentication.Commands.TelegramBotAuth;

public sealed class TelegramBotAuthCommandHandler : ICommandHandler<TelegramBotAuthCommand, Result<AuthenticationModel>> {
    private readonly IUserRepository _userRepository;
    private readonly IAuthenticationTokenService _authenticationTokenService;

    public TelegramBotAuthCommandHandler(
        IUserRepository userRepository,
        IAuthenticationTokenService authenticationTokenService) {
        _userRepository = userRepository;
        _authenticationTokenService = authenticationTokenService;
    }

    public async Task<Result<AuthenticationModel>> Handle(TelegramBotAuthCommand command, CancellationToken cancellationToken) {
        var user = await _userRepository.GetByTelegramUserIdAsync(command.TelegramUserId, cancellationToken).ConfigureAwait(false);
        var accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AuthenticationModel>(user is null ? Errors.Authentication.TelegramNotLinked : accessError);
        }

        var currentUser = user!;
        var tokens = await _authenticationTokenService.IssueAndStoreAsync(currentUser, cancellationToken, command.ClientContext).ConfigureAwait(false);
        return Result.Success(currentUser.ToAuthenticationModel(tokens));
    }
}
