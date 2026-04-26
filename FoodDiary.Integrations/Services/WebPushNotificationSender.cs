using System.Text.Json;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;

namespace FoodDiary.Integrations.Services;

public sealed class WebPushNotificationSender(
    IWebPushSubscriptionRepository subscriptionRepository,
    IUserRepository userRepository,
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
            logger.LogDebug(
                "Skipping web push notification {NotificationId} because web push is disabled or not configured.",
                notification.Id.Value);
            return;
        }

        var user = await userRepository.GetByIdAsync(notification.UserId, cancellationToken);
        if (user is null) {
            logger.LogDebug(
                "Skipping web push notification {NotificationId} because user {UserId} was not found.",
                notification.Id.Value,
                notification.UserId.Value);
            return;
        }

        if (!user.PushNotificationsEnabled) {
            logger.LogDebug(
                "Skipping web push notification {NotificationId} for user {UserId} because account push notifications are disabled.",
                notification.Id.Value,
                notification.UserId.Value);
            return;
        }

        if (!IsCategoryEnabled(user, notification.Type)) {
            logger.LogDebug(
                "Skipping web push notification {NotificationId} for user {UserId} because category {NotificationType} is disabled.",
                notification.Id.Value,
                notification.UserId.Value,
                notification.Type);
            return;
        }

        var subscriptions = await subscriptionRepository.GetByUserAsync(notification.UserId, cancellationToken);
        if (subscriptions.Count == 0) {
            logger.LogDebug(
                "Skipping web push notification {NotificationId} for user {UserId} because there are no subscriptions.",
                notification.Id.Value,
                notification.UserId.Value);
            return;
        }

        var utcNow = DateTime.UtcNow;
        var expiredSubscriptions = subscriptions
            .Where(subscription => subscription.ExpirationTimeUtc.HasValue && subscription.ExpirationTimeUtc.Value <= utcNow)
            .ToList();

        if (expiredSubscriptions.Count > 0) {
            await subscriptionRepository.DeleteRangeAsync(expiredSubscriptions, cancellationToken);
            logger.LogInformation(
                "Pruned {SubscriptionCount} expired web push subscriptions for user {UserId} before sending notification {NotificationId}.",
                expiredSubscriptions.Count,
                notification.UserId.Value,
                notification.Id.Value);

            subscriptions = subscriptions.Except(expiredSubscriptions).ToList();
        }

        if (subscriptions.Count == 0) {
            logger.LogDebug(
                "Skipping web push notification {NotificationId} for user {UserId} because there are no active subscriptions.",
                notification.Id.Value,
                notification.UserId.Value);
            return;
        }

        var client = new WebPushClient();
        var vapidDetails = new VapidDetails(options.Subject, options.PublicKey, options.PrivateKey);
        var invalidSubscriptions = new List<WebPushSubscription>();
        var deliveredCount = 0;

        foreach (var subscription in subscriptions) {
            var text = notificationTextRenderer.RenderFromPayload(notification.Type, notification.PayloadJson, subscription.Locale);
            var payload = BuildPayload(notification, text);
            var pushSubscription = new PushSubscription(subscription.Endpoint, subscription.P256Dh, subscription.Auth);

            try {
                cancellationToken.ThrowIfCancellationRequested();
                await client.SendNotificationAsync(pushSubscription, payload, vapidDetails, cancellationToken);
                deliveredCount++;
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

        logger.LogDebug(
            "Processed web push notification {NotificationId} for user {UserId}. Delivered={DeliveredCount}, Expired={ExpiredCount}, Attempted={AttemptedCount}.",
            notification.Id.Value,
            notification.UserId.Value,
            deliveredCount,
            invalidSubscriptions.Count,
            subscriptions.Count);
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
                    referenceId = notification.ReferenceId
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private string ResolveUrl(Notification notification) {
        var relativePath = NotificationTargetUrlResolver.Resolve(notification.Type, notification.ReferenceId) ?? options.DefaultUrl;

        if (Uri.TryCreate(options.DefaultUrl, UriKind.Absolute, out var absoluteBase)
            && Uri.TryCreate(absoluteBase, relativePath, out var targetUrl)) {
            return targetUrl.ToString();
        }

        return relativePath;
    }

    private static bool IsExpiredSubscription(WebPushException ex) {
        return ex.StatusCode == System.Net.HttpStatusCode.Gone
               || ex.StatusCode == System.Net.HttpStatusCode.NotFound;
    }

    private static bool IsCategoryEnabled(FoodDiary.Domain.Entities.Users.User user, string notificationType) {
        return notificationType switch {
            NotificationTypes.FastingCompleted => user.FastingPushNotificationsEnabled,
            NotificationTypes.EatingWindowStarted => user.FastingPushNotificationsEnabled,
            NotificationTypes.FastingWindowStarted => user.FastingPushNotificationsEnabled,
            NotificationTypes.FastingCheckInReminder => user.FastingPushNotificationsEnabled,
            NotificationTypes.DietologistInvitationReceived => user.SocialPushNotificationsEnabled,
            NotificationTypes.DietologistInvitationAccepted => user.SocialPushNotificationsEnabled,
            NotificationTypes.DietologistInvitationDeclined => user.SocialPushNotificationsEnabled,
            NotificationTypes.NewRecommendation => user.SocialPushNotificationsEnabled,
            NotificationTypes.NewComment => user.SocialPushNotificationsEnabled,
            _ => true
        };
    }
}
