using System.Text.Json;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;

namespace FoodDiary.Infrastructure.Services;

public sealed class WebPushNotificationSender(
    IWebPushSubscriptionRepository subscriptionRepository,
    INotificationTextRenderer notificationTextRenderer,
    IOptions<WebPushOptions> optionsAccessor,
    ILogger<WebPushNotificationSender> logger)
    : IWebPushNotificationSender, IWebPushConfigurationProvider {
    private readonly WebPushOptions options = optionsAccessor.Value;

    public WebPushClientConfiguration GetClientConfiguration() {
        return new WebPushClientConfiguration(options.Enabled && IsConfigured(), options.PublicKey);
    }

    public async Task SendAsync(Notification notification, CancellationToken cancellationToken = default) {
        if (!options.Enabled || !IsConfigured()) {
            return;
        }

        var subscriptions = await subscriptionRepository.GetByUserAsync(notification.UserId, cancellationToken);
        if (subscriptions.Count == 0) {
            return;
        }

        var client = new WebPushClient();
        var vapidDetails = new VapidDetails(options.Subject, options.PublicKey, options.PrivateKey);
        var invalidSubscriptions = new List<WebPushSubscription>();

        foreach (var subscription in subscriptions) {
            var text = notificationTextRenderer.RenderFromPayload(notification.Type, notification.PayloadJson, subscription.Locale);
            var payload = BuildPayload(notification, text);
            var pushSubscription = new PushSubscription(subscription.Endpoint, subscription.P256Dh, subscription.Auth);

            try {
                cancellationToken.ThrowIfCancellationRequested();
                await client.SendNotificationAsync(pushSubscription, payload, vapidDetails, cancellationToken);
            } catch (WebPushException ex) when (IsExpiredSubscription(ex)) {
                invalidSubscriptions.Add(subscription);
                logger.LogInformation(
                    "Removing expired web push subscription {SubscriptionId} for user {UserId}.",
                    subscription.Id.Value,
                    subscription.UserId.Value);
            } catch (Exception ex) {
                logger.LogWarning(
                    ex,
                    "Failed to send web push notification {NotificationId} to subscription {SubscriptionId}.",
                    notification.Id.Value,
                    subscription.Id.Value);
            }
        }

        if (invalidSubscriptions.Count > 0) {
            await subscriptionRepository.DeleteRangeAsync(invalidSubscriptions, cancellationToken);
        }
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
                    url = options.DefaultUrl,
                    type = notification.Type,
                    referenceId = notification.ReferenceId
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static bool IsExpiredSubscription(WebPushException ex) {
        return ex.StatusCode == System.Net.HttpStatusCode.Gone
               || ex.StatusCode == System.Net.HttpStatusCode.NotFound;
    }
}
