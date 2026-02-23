using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Commands.TelegramLoginWidget;

public sealed class TelegramLoginWidgetCommandHandler : ICommandHandler<TelegramLoginWidgetCommand, Result<AuthenticationResponse>> {
    private readonly IUserRepository _userRepository;
    private readonly ITelegramLoginWidgetValidator _telegramLoginWidgetValidator;
    private readonly IAuthenticationTokenService _authenticationTokenService;

    public TelegramLoginWidgetCommandHandler(
        IUserRepository userRepository,
        ITelegramLoginWidgetValidator telegramLoginWidgetValidator,
        IAuthenticationTokenService authenticationTokenService) {
        _userRepository = userRepository;
        _telegramLoginWidgetValidator = telegramLoginWidgetValidator;
        _authenticationTokenService = authenticationTokenService;
    }

    public async Task<Result<AuthenticationResponse>> Handle(TelegramLoginWidgetCommand command, CancellationToken cancellationToken) {
        var validationResult = _telegramLoginWidgetValidator.ValidateLoginWidget(
            new TelegramLoginWidgetData(
                command.Id,
                command.AuthDate,
                command.Hash,
                command.Username,
                command.FirstName,
                command.LastName,
                command.PhotoUrl));

        if (!validationResult.IsSuccess) {
            return Result.Failure<AuthenticationResponse>(validationResult.Error);
        }

        var initData = validationResult.Value;
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
