using FoodDiary.Results;
using FoodDiary.Application.Authentication.Commands.GoogleLogin;
using FoodDiary.Application.Authentication.Commands.Login;
using FoodDiary.Application.Authentication.Commands.RefreshToken;
using FoodDiary.Application.Authentication.Commands.Register;
using FoodDiary.Application.Authentication.Commands.ResendEmailVerification;
using FoodDiary.Application.Authentication.Commands.RestoreAccount;
using FoodDiary.Application.Authentication.Commands.VerifyEmail;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Auth;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuthSessionControllerTests {
    [Fact]
    public async Task Register_SendsRegisterCommandAndReturnsAuthenticationResponse() {
        AuthenticationModel model = CreateAuthenticationModel();
        IRequest<Result<AuthenticationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        AuthSessionController controller = CreateController(sender);
        var request = new RegisterHttpRequest("alex@example.com", "password", "en", "https://fooddiary.club");

        IActionResult result = await controller.Register(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        AuthenticationHttpResponse response = Assert.IsType<AuthenticationHttpResponse>(ok.Value);
        Assert.Equal("access-token", response.AccessToken);
        RegisterCommand command = Assert.IsType<RegisterCommand>(sentRequest);
        Assert.Equal("alex@example.com", command.Email);
        Assert.Equal("password-register", command.ClientContext!.AuthProvider);
    }

    [Fact]
    public async Task Login_SendsLoginCommandAndReturnsAuthenticationResponse() {
        AuthenticationModel model = CreateAuthenticationModel();
        IRequest<Result<AuthenticationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        AuthSessionController controller = CreateController(sender);
        var request = new LoginHttpRequest("alex@example.com", "password", RememberMe: true);

        IActionResult result = await controller.Login(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        AuthenticationHttpResponse response = Assert.IsType<AuthenticationHttpResponse>(ok.Value);
        Assert.Equal("access-token", response.AccessToken);
        LoginCommand command = Assert.IsType<LoginCommand>(sentRequest);
        Assert.Equal("alex@example.com", command.Email);
        Assert.True(command.RememberMe);
        Assert.Equal("password", command.ClientContext!.AuthProvider);
    }

    [Fact]
    public async Task GoogleLogin_SendsCommandAndReturnsAuthenticationResponse() {
        AuthenticationModel model = CreateAuthenticationModel();
        IRequest<Result<AuthenticationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        AuthSessionController controller = CreateController(sender);
        var request = new GoogleLoginHttpRequest("google-credential", RememberMe: true);

        IActionResult result = await controller.GoogleLogin(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        AuthenticationHttpResponse response = Assert.IsType<AuthenticationHttpResponse>(ok.Value);
        Assert.Equal("access-token", response.AccessToken);
        GoogleLoginCommand command = Assert.IsType<GoogleLoginCommand>(sentRequest);
        Assert.Equal("google-credential", command.Credential);
        Assert.True(command.RememberMe);
        Assert.Equal("google", command.ClientContext!.AuthProvider);
    }

    [Fact]
    public async Task VerifyEmail_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        AuthSessionController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new VerifyEmailHttpRequest(userId, "verification-token");

        IActionResult result = await controller.VerifyEmail(request);

        Assert.IsType<NoContentResult>(result);
        VerifyEmailCommand command = Assert.IsType<VerifyEmailCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("verification-token", command.Token);
    }

    [Fact]
    public async Task Refresh_SendsRefreshCommandAndReturnsAuthenticationResponse() {
        AuthenticationModel model = CreateAuthenticationModel();
        IRequest<Result<AuthenticationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        AuthSessionController controller = CreateController(sender);
        var request = new RefreshTokenHttpRequest("refresh-token");

        IActionResult result = await controller.Refresh(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        AuthenticationHttpResponse response = Assert.IsType<AuthenticationHttpResponse>(ok.Value);
        Assert.Equal("access-token", response.AccessToken);
        RefreshTokenCommand command = Assert.IsType<RefreshTokenCommand>(sentRequest);
        Assert.Equal("refresh-token", command.RefreshToken);
    }

    [Fact]
    public async Task RestoreAccount_SendsRestoreCommandAndReturnsAuthenticationResponse() {
        AuthenticationModel model = CreateAuthenticationModel();
        IRequest<Result<AuthenticationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        AuthSessionController controller = CreateController(sender);
        var request = new RestoreAccountHttpRequest("alex@example.com", "password", RememberMe: true);

        IActionResult result = await controller.RestoreAccount(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        AuthenticationHttpResponse response = Assert.IsType<AuthenticationHttpResponse>(ok.Value);
        Assert.Equal("access-token", response.AccessToken);
        RestoreAccountCommand command = Assert.IsType<RestoreAccountCommand>(sentRequest);
        Assert.Equal("alex@example.com", command.Email);
        Assert.True(command.RememberMe);
        Assert.Equal("password-restore", command.ClientContext!.AuthProvider);
    }

    [Fact]
    public async Task ResendVerifyEmail_WithRequest_SendsClientOriginAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        AuthSessionController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new ResendEmailVerificationHttpRequest("https://fooddiary.club");

        IActionResult result = await controller.ResendVerifyEmail(userId, request);

        Assert.IsType<NoContentResult>(result);
        ResendEmailVerificationCommand command = Assert.IsType<ResendEmailVerificationCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("https://fooddiary.club", command.ClientOrigin);
    }

    [Fact]
    public async Task ResendVerifyEmail_WithNullRequest_SendsNullClientOrigin() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        AuthSessionController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.ResendVerifyEmail(userId);

        Assert.IsType<NoContentResult>(result);
        ResendEmailVerificationCommand command = Assert.IsType<ResendEmailVerificationCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Null(command.ClientOrigin);
    }

    private static AuthSessionController CreateController(ISender sender) =>
        new(sender, NullLogger<AuthSessionController>.Instance) {
            ControllerContext = new ControllerContext {
                HttpContext = CreateHttpContext(),
            },
        };

    private static DefaultHttpContext CreateHttpContext() {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        httpContext.Request.Headers.UserAgent = "AuthSessionTests/1.0";
        return httpContext;
    }

    private static AuthenticationModel CreateAuthenticationModel() =>
        new("access-token", "refresh-token", CreateUserModel());

    private static UserModel CreateUserModel() =>
        new(
            Guid.NewGuid(),
            "alex@example.com",
            HasPassword: true,
            Username: "alex",
            FirstName: null,
            LastName: null,
            BirthDate: null,
            Gender: null,
            Weight: null,
            DesiredWeight: null,
            DesiredWaist: null,
            Height: null,
            ActivityLevel: "Sedentary",
            DailyCalorieTarget: null,
            ProteinTarget: null,
            FatTarget: null,
            CarbTarget: null,
            FiberTarget: null,
            StepGoal: null,
            WaterGoal: null,
            HydrationGoal: null,
            Language: "en",
            Theme: "default",
            UiStyle: "default",
            PushNotificationsEnabled: false,
            FastingPushNotificationsEnabled: false,
            SocialPushNotificationsEnabled: false,
            FastingCheckInReminderHours: 8,
            FastingCheckInFollowUpReminderHours: 2,
            ProfileImage: null,
            ProfileImageAssetId: null,
            DashboardLayout: null,
            IsActive: true,
            IsEmailConfirmed: true,
            LastLoginAtUtc: null,
            AiConsentAcceptedAt: null);
}
