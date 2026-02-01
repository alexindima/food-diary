using FoodDiary.Application.Authentication.Commands.Login;
using FoodDiary.Application.Authentication.Commands.RefreshToken;
using FoodDiary.Application.Authentication.Commands.Register;
using FoodDiary.Application.Authentication.Commands.RestoreAccount;
using FoodDiary.Application.Authentication.Commands.LinkTelegram;
using FoodDiary.Application.Authentication.Commands.TelegramVerify;
using FoodDiary.Application.Authentication.Commands.TelegramBotAuth;
using FoodDiary.Contracts.Authentication;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Authentication.Mappings;

public static class AuthenticationMappings
{
    public static RegisterCommand ToCommand(this RegisterRequest request)
    {
        return new RegisterCommand(request.Email, request.Password);
    }

    public static RestoreAccountCommand ToCommand(this RestoreAccountRequest request)
    {
        return new RestoreAccountCommand(request.Email, request.Password);
    }

    public static LoginCommand ToCommand(this LoginRequest request)
    {
        return new LoginCommand(request.Email, request.Password);
    }

    public static RefreshTokenCommand ToCommand(this RefreshTokenRequest request)
    {
        return new RefreshTokenCommand(request.RefreshToken);
    }

    public static TelegramVerifyCommand ToCommand(this TelegramAuthRequest request)
    {
        return new TelegramVerifyCommand(request.InitData);
    }

    public static LinkTelegramCommand ToLinkCommand(this TelegramAuthRequest request, UserId userId)
    {
        return new LinkTelegramCommand(userId, request.InitData);
    }

    public static TelegramBotAuthCommand ToCommand(this TelegramBotAuthRequest request)
    {
        return new TelegramBotAuthCommand(request.TelegramUserId);
    }
}
