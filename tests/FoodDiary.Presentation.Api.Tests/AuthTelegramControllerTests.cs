using FoodDiary.Results;
using FoodDiary.Application.Authentication.Commands.LinkTelegram;
using FoodDiary.Application.Authentication.Commands.TelegramBotAuth;
using FoodDiary.Application.Authentication.Commands.TelegramLoginWidget;
using FoodDiary.Application.Authentication.Commands.TelegramVerify;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Auth;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Features.Users.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuthTelegramControllerTests {
    [Fact]
    public async Task TelegramVerify_SendsVerifyCommandAndReturnsAuthenticationResponse() {
        IRequest<Result<AuthenticationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(CreateAuthenticationModel()), request => sentRequest = request);
        AuthTelegramController controller = CreateController(sender);
        var request = new TelegramAuthHttpRequest("query_id=1&hash=abc");

        IActionResult result = await controller.TelegramVerify(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        AuthenticationHttpResponse response = Assert.IsType<AuthenticationHttpResponse>(ok.Value);
        Assert.Equal("access-token", response.AccessToken);
        TelegramVerifyCommand command = Assert.IsType<TelegramVerifyCommand>(sentRequest);
        Assert.Equal(request.InitData, command.InitData);
        Assert.Equal("telegram-mini-app", command.ClientContext!.AuthProvider);
    }

    [Fact]
    public async Task TelegramLoginWidget_SendsWidgetCommandAndReturnsAuthenticationResponse() {
        IRequest<Result<AuthenticationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(CreateAuthenticationModel()), request => sentRequest = request);
        AuthTelegramController controller = CreateController(sender);
        var request = new TelegramLoginWidgetHttpRequest(
            Id: 123,
            AuthDate: 456,
            Hash: "hash",
            Username: "alex",
            FirstName: "Alex",
            LastName: "Doe",
            PhotoUrl: "https://t.me/photo.png");

        IActionResult result = await controller.TelegramLoginWidget(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        AuthenticationHttpResponse response = Assert.IsType<AuthenticationHttpResponse>(ok.Value);
        Assert.Equal("access-token", response.AccessToken);
        TelegramLoginWidgetCommand command = Assert.IsType<TelegramLoginWidgetCommand>(sentRequest);
        Assert.Equal(123, command.Id);
        Assert.Equal("alex", command.Username);
        Assert.Equal("telegram-login-widget", command.ClientContext!.AuthProvider);
    }

    [Fact]
    public async Task LinkTelegram_SendsLinkCommandAndReturnsUserResponse() {
        UserModel model = CreateUserModel();
        IRequest<Result<UserModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        AuthTelegramController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new TelegramAuthHttpRequest("query_id=2&hash=def");

        IActionResult result = await controller.LinkTelegram(userId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        UserHttpResponse response = Assert.IsType<UserHttpResponse>(ok.Value);
        Assert.Equal(model.Id, response.Id);
        LinkTelegramCommand command = Assert.IsType<LinkTelegramCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(request.InitData, command.InitData);
    }

    [Fact]
    public async Task TelegramBotAuth_SendsBotAuthCommandAndReturnsAuthenticationResponse() {
        IRequest<Result<AuthenticationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(CreateAuthenticationModel()), request => sentRequest = request);
        AuthTelegramController controller = CreateController(sender);
        var request = new TelegramBotAuthHttpRequest(TelegramUserId: 987);

        IActionResult result = await controller.TelegramBotAuth(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        AuthenticationHttpResponse response = Assert.IsType<AuthenticationHttpResponse>(ok.Value);
        Assert.Equal("access-token", response.AccessToken);
        TelegramBotAuthCommand command = Assert.IsType<TelegramBotAuthCommand>(sentRequest);
        Assert.Equal(987, command.TelegramUserId);
        Assert.Equal("telegram-bot", command.ClientContext!.AuthProvider);
    }

    private static AuthTelegramController CreateController(ISender sender) =>
        new(sender, NullLogger<AuthTelegramController>.Instance) {
            ControllerContext = new ControllerContext {
                HttpContext = CreateHttpContext(),
            },
        };

    private static DefaultHttpContext CreateHttpContext() {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.30");
        httpContext.Request.Headers.UserAgent = "TelegramTests/1.0";
        return httpContext;
    }

    private static AuthenticationModel CreateAuthenticationModel() =>
        new("access-token", "refresh-token", CreateUserModel());

    private static UserModel CreateUserModel() =>
        new(
            Guid.NewGuid(),
            "telegram@example.com",
            HasPassword: true,
            Username: "telegram",
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
