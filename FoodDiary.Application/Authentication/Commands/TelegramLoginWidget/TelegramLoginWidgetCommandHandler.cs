using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;

namespace FoodDiary.Application.Authentication.Commands.TelegramLoginWidget;

public sealed class TelegramLoginWidgetCommandHandler : ICommandHandler<TelegramLoginWidgetCommand, Result<AuthenticationModel>> {
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

    public async Task<Result<AuthenticationModel>> Handle(TelegramLoginWidgetCommand command, CancellationToken cancellationToken) {
        var widgetData = new TelegramLoginWidgetData(
            command.Id,
            command.AuthDate,
            command.Hash,
            command.Username,
            command.FirstName,
            command.LastName,
            command.PhotoUrl);

        var validationResult = _telegramLoginWidgetValidator.ValidateLoginWidget(widgetData);
        if (!validationResult.IsSuccess) {
            return Result.Failure<AuthenticationModel>(validationResult.Error);
        }

        var user = await _userRepository.GetByTelegramUserIdAsync(validationResult.Value.UserId, cancellationToken);
        if (user == null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.TelegramNotLinked);
        }

        if (user.DeletedAt is not null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.AccountDeleted);
        }

        if (!user.IsActive) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.InvalidCredentials);
        }

        var tokens = await _authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
