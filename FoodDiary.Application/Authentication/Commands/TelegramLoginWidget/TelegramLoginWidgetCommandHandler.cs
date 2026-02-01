using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Commands.TelegramLoginWidget;

public sealed class TelegramLoginWidgetCommandHandler : ICommandHandler<TelegramLoginWidgetCommand, Result<AuthenticationResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITelegramLoginWidgetValidator _telegramLoginWidgetValidator;

    public TelegramLoginWidgetCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher,
        ITelegramLoginWidgetValidator telegramLoginWidgetValidator)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
        _telegramLoginWidgetValidator = telegramLoginWidgetValidator;
    }

    public async Task<Result<AuthenticationResponse>> Handle(TelegramLoginWidgetCommand command, CancellationToken cancellationToken)
    {
        var validationResult = _telegramLoginWidgetValidator.ValidateLoginWidget(
            new TelegramLoginWidgetData(
                command.Id,
                command.AuthDate,
                command.Hash,
                command.Username,
                command.FirstName,
                command.LastName,
                command.PhotoUrl));

        if (!validationResult.IsSuccess)
        {
            return Result.Failure<AuthenticationResponse>(validationResult.Error);
        }

        var initData = validationResult.Value;
        var user = await _userRepository.GetByTelegramUserIdAsync(initData.UserId);
        if (user == null)
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.TelegramNotLinked);
        }

        if (user.DeletedAt is not null)
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.AccountDeleted);
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.InvalidCredentials);
        }

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.Id, user.Email);
        var hashedRefreshToken = _passwordHasher.Hash(refreshToken);
        user.UpdateRefreshToken(hashedRefreshToken);
        await _userRepository.UpdateAsync(user);

        var userResponse = user.ToResponse();
        var authResponse = new AuthenticationResponse(accessToken, refreshToken, userResponse);
        return Result.Success(authResponse);
    }
}
