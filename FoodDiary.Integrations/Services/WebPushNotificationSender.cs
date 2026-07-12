using System.Text.Json;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;

namespace FoodDiary.Integrations.Services;

public sealed class WebPushNotificationSender(
    IWebPushDeliveryAudienceService deliveryAudienceService,
    INotificationTextRenderer notificationTextRenderer,
    IOptions<WebPushOptions> optionsAccessor,
    IWebPushClientAdapter webPushClient,
    TimeProvider timeProvider,
    ILogger<WebPushNotificationSender> logger)
    : IWebPushNotificationSender, IWebPushConfigurationProvider {
    private readonly WebPushOptions options = optionsAccessor.Value;

    public WebPushClientConfiguration GetClientConfiguration() {
        return new WebPushClientConfiguration(options.Enabled && IsConfigured(), options.PublicKey);
    }

    public async Task SendAsync(Notification notification, CancellationToken cancellationToken = default) {
        if (ShouldSkipForConfiguration(notification)) {
            return;
        }

        IReadOnlyList<WebPushDeliverySubscription> subscriptions = await deliveryAudienceService
            .GetActiveAudienceAsync(notification.UserId, notification.Type, timeProvider.GetUtcNow().UtcDateTime, cancellationToken)
            .ConfigureAwait(false);
        if (subscriptions.Count == 0) {
            return;
        }

        (int deliveredCount, List<WebPushDeliverySubscription>? invalidSubscriptions) = await SendToSubscriptionsAsync(notification, subscriptions, cancellationToken).ConfigureAwait(false);
        if (invalidSubscriptions.Count > 0) {
            await deliveryAudienceService.RemoveInvalidSubscriptionsAsync(
                notification.UserId,
                [.. invalidSubscriptions.Select(static subscription => subscription.Id)],
                cancellationToken).ConfigureAwait(false);
        }

        logger.LogDebug(
            "Processed web push notification {NotificationId} for user {UserId}. Delivered={DeliveredCount}, Expired={ExpiredCount}, Attempted={AttemptedCount}.",
            notification.Id.Value,
            notification.UserId.Value,
            deliveredCount,
            invalidSubscriptions.Count,
            subscriptions.Count);
    }

    private bool ShouldSkipForConfiguration(Notification notification) {
        if (options.Enabled && IsConfigured()) {
            return false;
        }

        logger.LogDebug(
            "Skipping web push notification {NotificationId} because web push is disabled or not configured.",
            notification.Id.Value);
        return true;
    }

    private async Task<(int DeliveredCount, List<WebPushDeliverySubscription> InvalidSubscriptions)> SendToSubscriptionsAsync(
        Notification notification,
        IReadOnlyCollection<WebPushDeliverySubscription> subscriptions,
        CancellationToken cancellationToken) {
        var vapidDetails = new VapidDetails(options.Subject, options.PublicKey, options.PrivateKey);
        var invalidSubscriptions = new List<WebPushDeliverySubscription>();
        int deliveredCount = 0;

        foreach (WebPushDeliverySubscription subscription in subscriptions) {
            NotificationText text = notificationTextRenderer.RenderFromPayload(notification.Type, notification.PayloadJson, subscription.Locale);
            string payload = BuildPayload(notification, text);
            var pushSubscription = new PushSubscription(subscription.Endpoint, subscription.P256Dh, subscription.Auth);

            try {
                cancellationToken.ThrowIfCancellationRequested();
                await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails, cancellationToken).ConfigureAwait(false);
                deliveredCount++;
            } catch (WebPushException ex) when (IsExpiredSubscription(ex)) {
                invalidSubscriptions.Add(subscription);
                logger.LogInformation(
                    "Removing expired web push subscription {SubscriptionId} for user {UserId}.",
                    subscription.Id,
                    notification.UserId.Value);
            } catch (Exception ex) {
                logger.LogWarning(
                    ex,
                    "Failed to send web push notification {NotificationId} to subscription {SubscriptionId}.",
                    notification.Id.Value,
                    subscription.Id);
            }
        }

        return (deliveredCount, invalidSubscriptions);
    }

    private bool IsConfigured() {
        return !string.IsNullOrWhiteSpace(options.Subject)
               && !string.IsNullOrWhiteSpace(options.PublicKey)
               && !string.IsNullOrWhiteSpace(options.PrivateKey);
    }

    private string BuildPayload(Notification notification, NotificationText text) {
        var payload = new {
            notification = new {
                title = text.Title,
                body = text.Body,
                icon = "/assets/pwa/icon-192x192.png",
                badge = "/assets/pwa/icon-96x96.png",
                data = new {
                    targetUrl = ResolveUrl(notification),
                    url = ResolveUrl(notification),
                    type = notification.Type,
                    referenceId = notification.ReferenceId,
                },
            },
        };

        return JsonSerializer.Serialize(payload);
    }

    private string ResolveUrl(Notification notification) {
        string relativePath = NotificationTargetUrlResolver.Resolve(notification.Type, notification.ReferenceId) ?? options.DefaultUrl;

        if (Uri.TryCreate(options.DefaultUrl, UriKind.Absolute, out Uri? absoluteBase)
            && IsAbsoluteHttpUrl(absoluteBase)
            && Uri.TryCreate(absoluteBase, relativePath, out Uri? targetUrl)) {
            return targetUrl.ToString();
        }

        return relativePath;
    }

    private static bool IsAbsoluteHttpUrl(Uri uri) =>
        uri.IsAbsoluteUri &&
        (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));

    private static bool IsExpiredSubscription(WebPushException ex) {
        return ex.StatusCode == System.Net.HttpStatusCode.Gone
               || ex.StatusCode == System.Net.HttpStatusCode.NotFound;
    }

}
