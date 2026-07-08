using FoodDiary.Results;
using FoodDiary.Application.Authentication.Commands.AdminSsoExchange;
using FoodDiary.Application.Authentication.Commands.AdminSsoStart;
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
public sealed class AdminSsoControllerTests {
    [Fact]
    public async Task AdminSsoStart_SendsStartCommandAndReturnsResponse() {
        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(5);
        IRequest<Result<AdminSsoStartModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(
            Result.Success(new AdminSsoStartModel("sso-code", expiresAtUtc)),
            request => sentRequest = request);
        AdminSsoController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.AdminSsoStart(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        AdminSsoStartHttpResponse response = Assert.IsType<AdminSsoStartHttpResponse>(ok.Value);
        Assert.Equal("sso-code", response.Code);
        AdminSsoStartCommand command = Assert.IsType<AdminSsoStartCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
    }

    [Fact]
    public async Task AdminSsoExchange_SendsExchangeCommandAndReturnsAuthenticationResponse() {
        IRequest<Result<AuthenticationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(CreateAuthenticationModel()), request => sentRequest = request);
        AdminSsoController controller = CreateController(sender);
        var request = new AdminSsoExchangeHttpRequest("exchange-code");

        IActionResult result = await controller.AdminSsoExchange(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        AuthenticationHttpResponse response = Assert.IsType<AuthenticationHttpResponse>(ok.Value);
        Assert.Equal("access-token", response.AccessToken);
        AdminSsoExchangeCommand command = Assert.IsType<AdminSsoExchangeCommand>(sentRequest);
        Assert.Equal("exchange-code", command.Code);
        Assert.Equal("admin-sso", command.ClientContext!.AuthProvider);
    }

    private static AdminSsoController CreateController(ISender sender) =>
        new(sender, NullLogger<AdminSsoController>.Instance) {
            ControllerContext = new ControllerContext {
                HttpContext = CreateHttpContext(),
            },
        };

    private static DefaultHttpContext CreateHttpContext() {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.20");
        httpContext.Request.Headers.UserAgent = "AdminSsoTests/1.0";
        return httpContext;
    }

    private static AuthenticationModel CreateAuthenticationModel() =>
        new("access-token", "refresh-token", new UserModel(
            Guid.NewGuid(),
            "admin@example.com",
            HasPassword: true,
            Username: "admin",
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
            AiConsentAcceptedAt: null));
}
