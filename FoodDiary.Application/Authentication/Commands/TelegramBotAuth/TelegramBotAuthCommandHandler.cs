using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Commands.TelegramBotAuth;

public sealed class TelegramBotAuthCommandHandler : ICommandHandler<TelegramBotAuthCommand, Result<AuthenticationResponse>> {
    private readonly IUserRepository _userRepository;
    private readonly IAuthenticationTokenService _authenticationTokenService;

    public TelegramBotAuthCommandHandler(
        IUserRepository userRepository,
        IAuthenticationTokenService authenticationTokenService) {
        _userRepository = userRepository;
        _authenticationTokenService = authenticationTokenService;
    }

    public async Task<Result<AuthenticationResponse>> Handle(TelegramBotAuthCommand command, CancellationToken cancellationToken) {
        var user = await _userRepository.GetByTelegramUserIdAsync(command.TelegramUserId);
        if (user == null) {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.TelegramNotLinked);
        }

        if (user.DeletedAt is not null) {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.AccountDeleted);
        }

        if (!user.IsActive) {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.InvalidCredentials);
        }

        var tokens = await _authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);

        var userResponse = user.ToResponse();
        var authResponse = new AuthenticationResponse(tokens.AccessToken, tokens.RefreshToken, userResponse);
        return Result.Success(authResponse);
    }
}
