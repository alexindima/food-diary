using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Authentication.Services;

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
        var accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
        if (accessError is not null) {
            return Result.Failure<AuthenticationModel>(user is null ? Errors.Authentication.TelegramNotLinked : accessError);
        }
        if (user is null) {
            return Result.Failure<AuthenticationModel>(Errors.Authentication.TelegramNotLinked);
        }

        var tokens = await _authenticationTokenService.IssueAndStoreAsync(user, cancellationToken, command.ClientContext);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
