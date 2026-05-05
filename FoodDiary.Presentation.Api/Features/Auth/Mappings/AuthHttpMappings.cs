using FoodDiary.Application.Authentication.Commands.AdminSsoExchange;
using FoodDiary.Application.Authentication.Commands.AdminSsoStart;
using FoodDiary.Application.Authentication.Commands.ConfirmPasswordReset;
using FoodDiary.Application.Authentication.Commands.GoogleLogin;
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
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Features.Auth.Mappings;

public static class AuthHttpMappings {
    public static RegisterCommand ToCommand(this RegisterHttpRequest request) {
        return new RegisterCommand(
            Email: request.Email,
            Password: request.Password,
            Language: request.Language,
            ClientOrigin: request.ClientOrigin);
    }

    public static RegisterCommand ToCommand(this RegisterHttpRequest request, HttpContext httpContext) {
        return new RegisterCommand(
            Email: request.Email,
            Password: request.Password,
            Language: request.Language,
            ClientOrigin: request.ClientOrigin,
            ClientContext: httpContext.ToAuthenticationClientContext("password-register"));
    }

    public static RestoreAccountCommand ToCommand(this RestoreAccountHttpRequest request) {
        return new RestoreAccountCommand(
            Email: request.Email,
            Password: request.Password);
    }

    public static RestoreAccountCommand ToCommand(this RestoreAccountHttpRequest request, HttpContext httpContext) {
        return new RestoreAccountCommand(
            Email: request.Email,
            Password: request.Password,
            ClientContext: httpContext.ToAuthenticationClientContext("password-restore"));
    }

    public static LoginCommand ToCommand(this LoginHttpRequest request) {
        return new LoginCommand(
            Email: request.Email,
            Password: request.Password);
    }

    public static LoginCommand ToCommand(this LoginHttpRequest request, HttpContext httpContext) {
        return new LoginCommand(
            Email: request.Email,
            Password: request.Password,
            ClientContext: httpContext.ToAuthenticationClientContext("password"));
    }

    public static GoogleLoginCommand ToCommand(this GoogleLoginHttpRequest request) {
        return new GoogleLoginCommand(Credential: request.Credential);
    }

    public static GoogleLoginCommand ToCommand(this GoogleLoginHttpRequest request, HttpContext httpContext) {
        return new GoogleLoginCommand(
            Credential: request.Credential,
            ClientContext: httpContext.ToAuthenticationClientContext("google"));
    }

    public static RefreshTokenCommand ToCommand(this RefreshTokenHttpRequest request) {
        return new RefreshTokenCommand(RefreshToken: request.RefreshToken);
    }

    public static TelegramVerifyCommand ToCommand(this TelegramAuthHttpRequest request) {
        return new TelegramVerifyCommand(InitData: request.InitData);
    }

    public static TelegramVerifyCommand ToCommand(this TelegramAuthHttpRequest request, HttpContext httpContext) {
        return new TelegramVerifyCommand(
            InitData: request.InitData,
            ClientContext: httpContext.ToAuthenticationClientContext("telegram-mini-app"));
    }

    public static LinkTelegramCommand ToLinkCommand(this TelegramAuthHttpRequest request, Guid userId) {
        return new LinkTelegramCommand(
            UserId: userId,
            InitData: request.InitData);
    }

    public static TelegramBotAuthCommand ToCommand(this TelegramBotAuthHttpRequest request) {
        return new TelegramBotAuthCommand(TelegramUserId: request.TelegramUserId);
    }

    public static TelegramBotAuthCommand ToCommand(this TelegramBotAuthHttpRequest request, HttpContext httpContext) {
        return new TelegramBotAuthCommand(
            TelegramUserId: request.TelegramUserId,
            ClientContext: httpContext.ToAuthenticationClientContext("telegram-bot"));
    }

    public static AdminSsoExchangeCommand ToCommand(this AdminSsoExchangeHttpRequest request) {
        return new AdminSsoExchangeCommand(Code: request.Code);
    }

    public static AdminSsoExchangeCommand ToCommand(this AdminSsoExchangeHttpRequest request, HttpContext httpContext) {
        return new AdminSsoExchangeCommand(
            Code: request.Code,
            ClientContext: httpContext.ToAuthenticationClientContext("admin-sso"));
    }

    public static ResendEmailVerificationCommand ToResendVerificationCommand(this Guid userId, string? clientOrigin = null) {
        return new ResendEmailVerificationCommand(
            UserId: userId,
            ClientOrigin: clientOrigin);
    }

    public static AdminSsoStartCommand ToAdminSsoStartCommand(this Guid userId) {
        return new AdminSsoStartCommand(UserId: userId);
    }

    public static VerifyEmailCommand ToCommand(this VerifyEmailHttpRequest request) {
        return new VerifyEmailCommand(
            UserId: request.UserId,
            Token: request.Token);
    }

    public static RequestPasswordResetCommand ToCommand(this RequestPasswordResetHttpRequest request) {
        return new RequestPasswordResetCommand(
            Email: request.Email,
            ClientOrigin: request.ClientOrigin);
    }

    public static ConfirmPasswordResetCommand ToCommand(this ConfirmPasswordResetHttpRequest request) {
        return new ConfirmPasswordResetCommand(
            UserId: request.UserId,
            Token: request.Token,
            NewPassword: request.NewPassword);
    }

    public static TelegramLoginWidgetCommand ToCommand(this TelegramLoginWidgetHttpRequest request) {
        return new TelegramLoginWidgetCommand(
            Id: request.Id,
            AuthDate: request.AuthDate,
            Hash: request.Hash,
            Username: request.Username,
            FirstName: request.FirstName,
            LastName: request.LastName,
            PhotoUrl: request.PhotoUrl);
    }

    public static TelegramLoginWidgetCommand ToCommand(this TelegramLoginWidgetHttpRequest request, HttpContext httpContext) {
        return new TelegramLoginWidgetCommand(
            Id: request.Id,
            AuthDate: request.AuthDate,
            Hash: request.Hash,
            Username: request.Username,
            FirstName: request.FirstName,
            LastName: request.LastName,
            PhotoUrl: request.PhotoUrl,
            ClientContext: httpContext.ToAuthenticationClientContext("telegram-login-widget"));
    }

    private static AuthenticationClientContext ToAuthenticationClientContext(this HttpContext httpContext, string authProvider) {
        return new AuthenticationClientContext(
            authProvider,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers["User-Agent"].ToString());
    }
}
