using FoodDiary.Application.Authentication.Commands.AdminSsoExchange;
using FoodDiary.Application.Authentication.Commands.AdminSsoStart;
using FoodDiary.Application.Authentication.Commands.ConfirmPasswordReset;
using FoodDiary.Application.Authentication.Commands.LinkTelegram;
using FoodDiary.Application.Authentication.Commands.Login;
using FoodDiary.Application.Authentication.Commands.RefreshToken;
using FoodDiary.Application.Authentication.Commands.Register;
using FoodDiary.Application.Authentication.Commands.RequestPasswordReset;
using FoodDiary.Application.Authentication.Commands.ResendEmailVerification;
using FoodDiary.Application.Authentication.Commands.RestoreAccount;
using FoodDiary.Application.Authentication.Commands.TelegramBotAuth;
using FoodDiary.Application.Authentication.Commands.TelegramLoginWidget;
using FoodDiary.Application.Authentication.Commands.TelegramVerify;
using FoodDiary.Application.Authentication.Commands.VerifyEmail;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Auth.Requests;

namespace FoodDiary.Presentation.Api.Features.Auth.Mappings;

public static class AuthHttpMappings {
    public static RegisterCommand ToCommand(this RegisterHttpRequest request) {
        return new RegisterCommand(request.Email, request.Password, request.Language);
    }

    public static RestoreAccountCommand ToCommand(this RestoreAccountHttpRequest request) {
        return new RestoreAccountCommand(request.Email, request.Password);
    }

    public static LoginCommand ToCommand(this LoginHttpRequest request) {
        return new LoginCommand(request.Email, request.Password);
    }

    public static RefreshTokenCommand ToCommand(this RefreshTokenHttpRequest request) {
        return new RefreshTokenCommand(request.RefreshToken);
    }

    public static TelegramVerifyCommand ToCommand(this TelegramAuthHttpRequest request) {
        return new TelegramVerifyCommand(request.InitData);
    }

    public static LinkTelegramCommand ToLinkCommand(this TelegramAuthHttpRequest request, UserId userId) {
        return new LinkTelegramCommand(userId, request.InitData);
    }

    public static TelegramBotAuthCommand ToCommand(this TelegramBotAuthHttpRequest request) {
        return new TelegramBotAuthCommand(request.TelegramUserId);
    }

    public static AdminSsoExchangeCommand ToCommand(this AdminSsoExchangeHttpRequest request) {
        return new AdminSsoExchangeCommand(request.Code);
    }

    public static ResendEmailVerificationCommand ToResendVerificationCommand(this UserId userId) {
        return new ResendEmailVerificationCommand(userId);
    }

    public static AdminSsoStartCommand ToAdminSsoStartCommand(this UserId userId) {
        return new AdminSsoStartCommand(userId);
    }

    public static VerifyEmailCommand ToCommand(this VerifyEmailHttpRequest request) {
        return new VerifyEmailCommand(new UserId(request.UserId), request.Token);
    }

    public static RequestPasswordResetCommand ToCommand(this RequestPasswordResetHttpRequest request) {
        return new RequestPasswordResetCommand(request.Email);
    }

    public static ConfirmPasswordResetCommand ToCommand(this ConfirmPasswordResetHttpRequest request) {
        return new ConfirmPasswordResetCommand(new UserId(request.UserId), request.Token, request.NewPassword);
    }

    public static TelegramLoginWidgetCommand ToCommand(this TelegramLoginWidgetHttpRequest request) {
        return new TelegramLoginWidgetCommand(
            request.Id,
            request.AuthDate,
            request.Hash,
            request.Username,
            request.FirstName,
            request.LastName,
            request.PhotoUrl);
    }
}
