using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;

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
        var user = await _userRepository.GetByTelegramUserIdAsync(command.TelegramUserId, cancellationToken);
        var accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AuthenticationModel>(user is null ? Errors.Authentication.TelegramNotLinked : accessError);
        }
        if (user is null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.TelegramNotLinked);
        }

        var tokens = await _authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
