using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Commands.TelegramVerify;

public sealed class TelegramVerifyCommandHandler : ICommandHandler<TelegramVerifyCommand, Result<AuthenticationResponse>> {
    private readonly IUserRepository _userRepository;
    private readonly ITelegramAuthValidator _telegramAuthValidator;
    private readonly IAuthenticationTokenService _authenticationTokenService;

    public TelegramVerifyCommandHandler(
        IUserRepository userRepository,
        ITelegramAuthValidator telegramAuthValidator,
        IAuthenticationTokenService authenticationTokenService) {
        _userRepository = userRepository;
        _telegramAuthValidator = telegramAuthValidator;
        _authenticationTokenService = authenticationTokenService;
    }

    public async Task<Result<AuthenticationResponse>> Handle(TelegramVerifyCommand command, CancellationToken cancellationToken) {
        var initDataResult = _telegramAuthValidator.ValidateInitData(command.InitData);
        if (!initDataResult.IsSuccess) {
            return Result.Failure<AuthenticationResponse>(initDataResult.Error);
        }

        var initData = initDataResult.Value;
        var user = await _userRepository.GetByTelegramUserIdAsync(initData.UserId);
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
