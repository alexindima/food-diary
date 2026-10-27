using System.Net;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Abstractions.Models;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Notifications.Infrastructure.Options;
using FoodDiary.Modules.Notifications.Infrastructure.Services;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.Extensions.Logging;
using NSubstitute.Core;
using WebPush;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Modules.Notifications.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class WebPushNotificationSenderLoggingTests {
    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(413)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task SendBatchAsync_WhenProviderRejectsDelivery_LogsOnlySafeStatusMetadata(int statusCode) {
        using var response = new HttpResponseMessage((HttpStatusCode)statusCode) {
            Content = new StringContent("private provider response"),
        };
        response.Headers.TryAddWithoutValidation("X-Private-Header", "private header");
        var error = new WebPushException(
            "private exception message with https://fcm.googleapis.com/fcm/send/private-token",
            new PushSubscription("https://fcm.googleapis.com/fcm/send/private-token", "private-p256dh", "private-auth"),
            response);

        await AssertSafeFailureLogAsync(error, statusCode);
    }

    [Theory]
    [InlineData(429)]
    [InlineData(503)]
    [InlineData(null)]
    public async Task SendBatchAsync_WhenHttpRequestFails_LogsAvailableStatusWithoutExceptionDetails(int? statusCode) {
        var error = new HttpRequestException("private transport details", inner: null, (HttpStatusCode?)statusCode);

        await AssertSafeFailureLogAsync(error, statusCode);
    }

    [Fact]
    public async Task SendBatchAsync_WhenFailureHasNoHttpResponse_LogsUnknownStatusWithoutExceptionDetails() {
        await AssertSafeFailureLogAsync(new InvalidOperationException("private failure details"), expectedStatusCode: null);
    }

    private static async Task AssertSafeFailureLogAsync(Exception error, int? expectedStatusCode) {
        var userId = UserId.New();
        var subscription = new WebPushDeliverySubscription(
            Guid.NewGuid(), "https://fcm.googleapis.com/fcm/send/private-token", "private-p256dh", "private-auth", "en");
        IWebPushDeliveryAudienceService audience = Substitute.For<IWebPushDeliveryAudienceService>();
        audience.GetActiveAudienceAsync(userId, NotificationTypes.FastingCompleted, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WebPushDeliverySubscription>>([subscription]));
        INotificationTextRenderer renderer = Substitute.For<INotificationTextRenderer>();
        renderer.RenderFromPayload(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(new NotificationText("private title", "private body"));
        IWebPushClientAdapter client = Substitute.For<IWebPushClientAdapter>();
        client.SendNotificationAsync(Arg.Any<PushSubscription>(), Arg.Any<string>(), Arg.Any<VapidDetails>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(error));
        ILogger<WebPushNotificationSender> logger = Substitute.For<ILogger<WebPushNotificationSender>>();
        var sender = new WebPushNotificationSender(
            audience,
            renderer,
            MsOptions.Create(new WebPushOptions {
                Enabled = true,
                Subject = "mailto:private@example.com",
                PublicKey = "private public key",
                PrivateKey = "private signing key",
            }),
            client,
            TimeProvider.System,
            logger);
        var notification = Notification.Create(userId, NotificationTypes.FastingCompleted, "{\"private\":\"payload\"}");

        WebPushDeliveryOutcome outcome = await sender.SendBatchAsync(notification, new HashSet<Guid>(), CancellationToken.None);

        ICall warning = Assert.Single(logger.ReceivedCalls(),
            call => string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal) && Equals(call.GetArguments()[0], LogLevel.Warning));
        object?[] arguments = warning.GetArguments();
        IReadOnlyList<KeyValuePair<string, object?>> fields = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(arguments[2]);
        var metadata = fields.ToDictionary(field => field.Key, field => field.Value, StringComparer.Ordinal);
        Assert.True(metadata.ContainsKey("HttpStatusCode"), "Push failure log must include a numeric HttpStatusCode field.");
        Assert.Multiple(
            () => Assert.Equal(expectedStatusCode, metadata["HttpStatusCode"]),
            () => Assert.Equal(error.GetType().Name, metadata["ErrorType"]),
            () => Assert.Equal(notification.Id.Value, metadata["NotificationId"]),
            () => Assert.Equal(subscription.Id, metadata["SubscriptionId"]),
            () => Assert.Equal(new[] { "ErrorType", "HttpStatusCode", "NotificationId", "SubscriptionId", "{OriginalFormat}" }, metadata.Keys.Order(StringComparer.Ordinal).ToArray()),
            () => Assert.Null(arguments[3]),
            () => Assert.DoesNotContain("private", arguments[2]!.ToString()!, StringComparison.OrdinalIgnoreCase),
            () => Assert.True(outcome.RequiresRetry),
            () => Assert.Empty(outcome.CompletedSubscriptionIds));
        await audience.DidNotReceive().RemoveInvalidSubscriptionsAsync(
            Arg.Any<UserId>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
    }
}
