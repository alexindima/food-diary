using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Notifications.Commands.RemoveWebPushSubscription;
using FoodDiary.Application.Notifications.Commands.UpsertWebPushSubscription;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Notifications.Queries.GetWebPushConfiguration;
using FoodDiary.Application.Notifications.Queries.GetWebPushSubscriptions;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Notifications;
using FoodDiary.Presentation.Api.Features.Notifications.Requests;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class NotificationPushControllerTests {
    [Fact]
    public async Task GetWebPushConfiguration_SendsQueryAndReturnsResponse() {
        var model = new WebPushConfigurationModel(Enabled: true, "public-key");
        IRequest<Result<WebPushConfigurationModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        NotificationPushController controller = CreateController(sender);

        IActionResult result = await controller.GetWebPushConfiguration();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        WebPushConfigurationHttpResponse response = Assert.IsType<WebPushConfigurationHttpResponse>(ok.Value);
        Assert.True(response.Enabled);
        Assert.Equal("public-key", response.PublicKey);
        Assert.IsType<GetWebPushConfigurationQuery>(sentRequest);
    }

    [Fact]
    public async Task UpsertWebPushSubscription_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        NotificationPushController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        DateTime expiration = DateTime.UtcNow.AddDays(7);
        var request = new UpsertWebPushSubscriptionHttpRequest(
            "https://push.example.com/subscriptions/123",
            expiration,
            new UpsertWebPushSubscriptionKeysHttpRequest("p256dh", "auth"),
            "ru",
            "Firefox");

        IActionResult result = await controller.UpsertWebPushSubscription(userId, request);

        Assert.IsType<NoContentResult>(result);
        UpsertWebPushSubscriptionCommand command = Assert.IsType<UpsertWebPushSubscriptionCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("https://push.example.com/subscriptions/123", command.Endpoint);
        Assert.Equal("p256dh", command.P256Dh);
        Assert.Equal("auth", command.Auth);
        Assert.Equal(expiration, command.ExpirationTimeUtc);
        Assert.Equal("ru", command.Locale);
        Assert.Equal("Firefox", command.UserAgent);
    }

    [Fact]
    public async Task GetWebPushSubscriptions_SendsQueryAndReturnsSubscriptions() {
        DateTime createdAtUtc = DateTime.UtcNow.AddDays(-1);
        var model = new WebPushSubscriptionModel(
            "https://push.example.com/subscriptions/123",
            "push.example.com",
            ExpirationTimeUtc: null,
            "ru",
            "Firefox",
            createdAtUtc,
            UpdatedAtUtc: null);
        IRequest<Result<IReadOnlyList<WebPushSubscriptionModel>>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<IReadOnlyList<WebPushSubscriptionModel>>([model]), request => sentRequest = request);
        NotificationPushController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetWebPushSubscriptions(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        List<WebPushSubscriptionHttpResponse> response = Assert.IsType<List<WebPushSubscriptionHttpResponse>>(ok.Value);
        Assert.Single(response);
        Assert.Equal("https://push.example.com/subscriptions/123", response[0].Endpoint);
        GetWebPushSubscriptionsQuery query = Assert.IsType<GetWebPushSubscriptionsQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task RemoveWebPushSubscription_SendsCommandAndReturnsNoContent() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        NotificationPushController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new RemoveWebPushSubscriptionHttpRequest("https://push.example.com/subscriptions/123");

        IActionResult result = await controller.RemoveWebPushSubscription(userId, request);

        Assert.IsType<NoContentResult>(result);
        RemoveWebPushSubscriptionCommand command = Assert.IsType<RemoveWebPushSubscriptionCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("https://push.example.com/subscriptions/123", command.Endpoint);
    }

    private static NotificationPushController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
}
