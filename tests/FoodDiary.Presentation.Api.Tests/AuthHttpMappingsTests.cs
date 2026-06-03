using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuthHttpMappingsTests {
    [Fact]
    public void RegisterRequest_ToCommand_MapsAllFields() {
        var request = new RegisterHttpRequest(
            Email: "alex@example.com",
            Password: "P@ssw0rd!",
            Language: "ru",
            ClientOrigin: "https://Ð´Ð½ÐµÐ²Ð½Ð¸ÐºÐµÐ´Ñ‹.Ñ€Ñ„");

        var command = request.ToCommand();

        Assert.Equal(request.Email, command.Email);
        Assert.Equal(request.Password, command.Password);
        Assert.Equal(request.Language, command.Language);
        Assert.Equal(request.ClientOrigin, command.ClientOrigin);
    }

    [Fact]
    public void RegisterRequest_ToCommand_WithHttpContext_MapsClientContext() {
        var request = new RegisterHttpRequest(
            Email: "alex@example.com",
            Password: "P@ssw0rd!",
            Language: "ru",
            ClientOrigin: "https://fooddiary.club");
        var httpContext = CreateHttpContext("203.0.113.10", "FoodDiaryTest/1.0");

        var command = request.ToCommand(httpContext);

        Assert.Equal(request.Email, command.Email);
        Assert.Equal("password-register", command.ClientContext!.AuthProvider);
        Assert.Equal("203.0.113.10", command.ClientContext.IpAddress);
        Assert.Equal("FoodDiaryTest/1.0", command.ClientContext.UserAgent);
    }

    [Fact]
    public void RestoreAccountRequest_ToCommand_MapsAllFields() {
        var request = new RestoreAccountHttpRequest("alex@example.com", "P@ssw0rd!");

        var command = request.ToCommand();

        Assert.Equal(request.Email, command.Email);
        Assert.Equal(request.Password, command.Password);
    }

    [Fact]
    public void RestoreAccountRequest_ToCommand_WithHttpContext_MapsClientContext() {
        var request = new RestoreAccountHttpRequest("alex@example.com", "P@ssw0rd!");
        var httpContext = CreateHttpContext("203.0.113.11", "RestoreAgent/1.0");

        var command = request.ToCommand(httpContext);

        Assert.Equal(request.Email, command.Email);
        Assert.Equal("password-restore", command.ClientContext!.AuthProvider);
        Assert.Equal("203.0.113.11", command.ClientContext.IpAddress);
        Assert.Equal("RestoreAgent/1.0", command.ClientContext.UserAgent);
    }

    [Fact]
    public void LoginRequest_ToCommand_MapsAllFields() {
        var request = new LoginHttpRequest("alex@example.com", "P@ssw0rd!");

        var command = request.ToCommand();

        Assert.Equal(request.Email, command.Email);
        Assert.Equal(request.Password, command.Password);
    }

    [Fact]
    public void LoginRequest_ToCommand_WithHttpContext_MapsClientContext() {
        var request = new LoginHttpRequest("alex@example.com", "P@ssw0rd!");
        var httpContext = CreateHttpContext("203.0.113.12", "LoginAgent/1.0");

        var command = request.ToCommand(httpContext);

        Assert.Equal(request.Email, command.Email);
        Assert.Equal("password", command.ClientContext!.AuthProvider);
        Assert.Equal("203.0.113.12", command.ClientContext.IpAddress);
        Assert.Equal("LoginAgent/1.0", command.ClientContext.UserAgent);
    }

    [Fact]
    public void GoogleLoginRequest_ToCommand_MapsCredential() {
        var request = new GoogleLoginHttpRequest("google-credential");

        var command = request.ToCommand();

        Assert.Equal(request.Credential, command.Credential);
    }

    [Fact]
    public void GoogleLoginRequest_ToCommand_WithHttpContext_MapsClientContext() {
        var request = new GoogleLoginHttpRequest("google-credential");
        var httpContext = CreateHttpContext("203.0.113.13", "GoogleAgent/1.0");

        var command = request.ToCommand(httpContext);

        Assert.Equal(request.Credential, command.Credential);
        Assert.Equal("google", command.ClientContext!.AuthProvider);
        Assert.Equal("203.0.113.13", command.ClientContext.IpAddress);
        Assert.Equal("GoogleAgent/1.0", command.ClientContext.UserAgent);
    }

    [Fact]
    public void RefreshTokenRequest_ToCommand_MapsRefreshToken() {
        var request = new RefreshTokenHttpRequest("refresh-token");

        var command = request.ToCommand();

        Assert.Equal(request.RefreshToken, command.RefreshToken);
    }

    [Fact]
    public void TelegramAuthRequest_ToCommand_MapsInitData() {
        var request = new TelegramAuthHttpRequest(InitData: "query_id=123&hash=abc");

        var command = request.ToCommand();

        Assert.Equal(request.InitData, command.InitData);
    }

    [Fact]
    public void TelegramAuthRequest_ToCommand_WithHttpContext_MapsClientContext() {
        var request = new TelegramAuthHttpRequest(InitData: "query_id=123&hash=abc");
        var httpContext = CreateHttpContext("203.0.113.14", "TelegramMiniApp/1.0");

        var command = request.ToCommand(httpContext);

        Assert.Equal(request.InitData, command.InitData);
        Assert.Equal("telegram-mini-app", command.ClientContext!.AuthProvider);
        Assert.Equal("203.0.113.14", command.ClientContext.IpAddress);
        Assert.Equal("TelegramMiniApp/1.0", command.ClientContext.UserAgent);
    }

    [Fact]
    public void TelegramAuthRequest_ToLinkCommand_MapsUserIdAndInitData() {
        var userId = Guid.NewGuid();
        var request = new TelegramAuthHttpRequest(InitData: "query_id=123&hash=abc");

        var command = request.ToLinkCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(request.InitData, command.InitData);
    }

    [Fact]
    public void TelegramLoginWidgetRequest_ToCommand_MapsAllFields() {
        var request = new TelegramLoginWidgetHttpRequest(
            Id: 42,
            AuthDate: 1_717_171_717,
            Hash: "hash-value",
            Username: "alex",
            FirstName: "Alex",
            LastName: "Doe",
            PhotoUrl: "https://cdn.example/avatar.png");

        var command = request.ToCommand();

        Assert.Equal(request.Id, command.Id);
        Assert.Equal(request.AuthDate, command.AuthDate);
        Assert.Equal(request.Hash, command.Hash);
        Assert.Equal(request.Username, command.Username);
        Assert.Equal(request.FirstName, command.FirstName);
        Assert.Equal(request.LastName, command.LastName);
        Assert.Equal(request.PhotoUrl, command.PhotoUrl);
    }

    [Fact]
    public void TelegramLoginWidgetRequest_ToCommand_WithHttpContext_MapsClientContext() {
        var request = new TelegramLoginWidgetHttpRequest(
            Id: 42,
            AuthDate: 1_717_171_717,
            Hash: "hash-value",
            Username: "alex",
            FirstName: "Alex",
            LastName: "Doe",
            PhotoUrl: "https://cdn.example/avatar.png");
        var httpContext = CreateHttpContext("203.0.113.15", "TelegramWidget/1.0");

        var command = request.ToCommand(httpContext);

        Assert.Equal(request.Id, command.Id);
        Assert.Equal("telegram-login-widget", command.ClientContext!.AuthProvider);
        Assert.Equal("203.0.113.15", command.ClientContext.IpAddress);
        Assert.Equal("TelegramWidget/1.0", command.ClientContext.UserAgent);
    }

    [Fact]
    public void TelegramBotAuthRequest_ToCommand_MapsTelegramUserId() {
        var request = new TelegramBotAuthHttpRequest(TelegramUserId: 123456);

        var command = request.ToCommand();

        Assert.Equal(request.TelegramUserId, command.TelegramUserId);
    }

    [Fact]
    public void TelegramBotAuthRequest_ToCommand_WithHttpContext_MapsClientContext() {
        var request = new TelegramBotAuthHttpRequest(TelegramUserId: 123456);
        var httpContext = CreateHttpContext("203.0.113.16", "TelegramBot/1.0");

        var command = request.ToCommand(httpContext);

        Assert.Equal(request.TelegramUserId, command.TelegramUserId);
        Assert.Equal("telegram-bot", command.ClientContext!.AuthProvider);
        Assert.Equal("203.0.113.16", command.ClientContext.IpAddress);
        Assert.Equal("TelegramBot/1.0", command.ClientContext.UserAgent);
    }

    [Fact]
    public void AdminSsoExchangeRequest_ToCommand_MapsCode() {
        var request = new AdminSsoExchangeHttpRequest("sso-code");

        var command = request.ToCommand();

        Assert.Equal(request.Code, command.Code);
    }

    [Fact]
    public void AdminSsoExchangeRequest_ToCommand_WithHttpContext_MapsClientContext() {
        var request = new AdminSsoExchangeHttpRequest("sso-code");
        var httpContext = CreateHttpContext("203.0.113.17", "AdminSso/1.0");

        var command = request.ToCommand(httpContext);

        Assert.Equal(request.Code, command.Code);
        Assert.Equal("admin-sso", command.ClientContext!.AuthProvider);
        Assert.Equal("203.0.113.17", command.ClientContext.IpAddress);
        Assert.Equal("AdminSso/1.0", command.ClientContext.UserAgent);
    }

    [Fact]
    public void UserId_ToResendVerificationCommand_MapsUserIdAndClientOrigin() {
        var userId = Guid.NewGuid();

        var command = userId.ToResendVerificationCommand("https://fooddiary.club");

        Assert.Equal(userId, command.UserId);
        Assert.Equal("https://fooddiary.club", command.ClientOrigin);
    }

    [Fact]
    public void UserId_ToAdminSsoStartCommand_MapsUserId() {
        var userId = Guid.NewGuid();

        var command = userId.ToAdminSsoStartCommand();

        Assert.Equal(userId, command.UserId);
    }

    [Fact]
    public void VerifyEmailRequest_ToCommand_MapsAllFields() {
        var request = new VerifyEmailHttpRequest(Guid.NewGuid(), "verification-token");

        var command = request.ToCommand();

        Assert.Equal(request.UserId, command.UserId);
        Assert.Equal(request.Token, command.Token);
    }

    [Fact]
    public void ConfirmPasswordResetRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var request = new ConfirmPasswordResetHttpRequest(
            UserId: userId,
            Token: "reset-token",
            NewPassword: "N3wP@ssword");

        var command = request.ToCommand();

        Assert.Equal(request.UserId, command.UserId);
        Assert.Equal(request.Token, command.Token);
        Assert.Equal(request.NewPassword, command.NewPassword);
    }

    [Fact]
    public void RequestPasswordResetRequest_ToCommand_MapsClientOrigin() {
        var request = new RequestPasswordResetHttpRequest(
            Email: "alex@example.com",
            ClientOrigin: "https://fooddiary.club");

        var command = request.ToCommand();

        Assert.Equal(request.Email, command.Email);
        Assert.Equal(request.ClientOrigin, command.ClientOrigin);
    }

    private static HttpContext CreateHttpContext(string ipAddress, string userAgent) {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        httpContext.Request.Headers.UserAgent = userAgent;
        return httpContext;
    }
}
