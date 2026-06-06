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
    extension(RegisterHttpRequest request) {
        public RegisterCommand ToCommand() {
            return new RegisterCommand(
                Email: request.Email,
                Password: request.Password,
                Language: request.Language,
                ClientOrigin: request.ClientOrigin);
        }
        public RegisterCommand ToCommand(HttpContext httpContext) {
            return new RegisterCommand(
                Email: request.Email,
                Password: request.Password,
                Language: request.Language,
                ClientOrigin: request.ClientOrigin,
                ClientContext: httpContext.ToAuthenticationClientContext("password-register"));
        }
    }

    extension(RestoreAccountHttpRequest request) {
        public RestoreAccountCommand ToCommand() {
            return new RestoreAccountCommand(
                Email: request.Email,
                Password: request.Password,
                RememberMe: request.RememberMe);
        }
        public RestoreAccountCommand ToCommand(HttpContext httpContext) {
            return new RestoreAccountCommand(
                Email: request.Email,
                Password: request.Password,
                RememberMe: request.RememberMe,
                ClientContext: httpContext.ToAuthenticationClientContext("password-restore"));
        }
    }

    extension(LoginHttpRequest request) {
        public LoginCommand ToCommand() {
            return new LoginCommand(
                Email: request.Email,
                Password: request.Password,
                RememberMe: request.RememberMe);
        }
        public LoginCommand ToCommand(HttpContext httpContext) {
            return new LoginCommand(
                Email: request.Email,
                Password: request.Password,
                RememberMe: request.RememberMe,
                ClientContext: httpContext.ToAuthenticationClientContext("password"));
        }
    }

    extension(GoogleLoginHttpRequest request) {
        public GoogleLoginCommand ToCommand() {
            return new GoogleLoginCommand(
                Credential: request.Credential,
                RememberMe: request.RememberMe);
        }
        public GoogleLoginCommand ToCommand(HttpContext httpContext) {
            return new GoogleLoginCommand(
                Credential: request.Credential,
                RememberMe: request.RememberMe,
                ClientContext: httpContext.ToAuthenticationClientContext("google"));
        }
    }

    public static RefreshTokenCommand ToCommand(this RefreshTokenHttpRequest request) {
        return new RefreshTokenCommand(RefreshToken: request.RefreshToken);
    }

    extension(TelegramAuthHttpRequest request) {
        public TelegramVerifyCommand ToCommand() {
            return new TelegramVerifyCommand(InitData: request.InitData);
        }
        public TelegramVerifyCommand ToCommand(HttpContext httpContext) {
            return new TelegramVerifyCommand(
                InitData: request.InitData,
                ClientContext: httpContext.ToAuthenticationClientContext("telegram-mini-app"));
        }
        public LinkTelegramCommand ToLinkCommand(Guid userId) {
            return new LinkTelegramCommand(
                UserId: userId,
                InitData: request.InitData);
        }
    }

    extension(TelegramBotAuthHttpRequest request) {
        public TelegramBotAuthCommand ToCommand() {
            return new TelegramBotAuthCommand(TelegramUserId: request.TelegramUserId);
        }
        public TelegramBotAuthCommand ToCommand(HttpContext httpContext) {
            return new TelegramBotAuthCommand(
                TelegramUserId: request.TelegramUserId,
                ClientContext: httpContext.ToAuthenticationClientContext("telegram-bot"));
        }
    }

    extension(AdminSsoExchangeHttpRequest request) {
        public AdminSsoExchangeCommand ToCommand() {
            return new AdminSsoExchangeCommand(Code: request.Code);
        }
        public AdminSsoExchangeCommand ToCommand(HttpContext httpContext) {
            return new AdminSsoExchangeCommand(
                Code: request.Code,
                ClientContext: httpContext.ToAuthenticationClientContext("admin-sso"));
        }
    }

    extension(Guid userId) {
        public ResendEmailVerificationCommand ToResendVerificationCommand(string? clientOrigin = null) {
            return new ResendEmailVerificationCommand(
                UserId: userId,
                ClientOrigin: clientOrigin);
        }
        public AdminSsoStartCommand ToAdminSsoStartCommand() {
            return new AdminSsoStartCommand(UserId: userId);
        }
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

    extension(TelegramLoginWidgetHttpRequest request) {
        public TelegramLoginWidgetCommand ToCommand() {
            return new TelegramLoginWidgetCommand(
                Id: request.Id,
                AuthDate: request.AuthDate,
                Hash: request.Hash,
                Username: request.Username,
                FirstName: request.FirstName,
                LastName: request.LastName,
                PhotoUrl: request.PhotoUrl);
        }
        public TelegramLoginWidgetCommand ToCommand(HttpContext httpContext) {
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
    }

    private static AuthenticationClientContext ToAuthenticationClientContext(this HttpContext httpContext, string authProvider) {
        return new AuthenticationClientContext(
            authProvider,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString());
    }
}
