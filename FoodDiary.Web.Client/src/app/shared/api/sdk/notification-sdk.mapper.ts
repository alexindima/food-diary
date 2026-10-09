import type { NotificationItem, NotificationPreferences, WebPushSubscriptionItem } from '../../models/notification.data';
import { decodeNotificationKind } from '../../models/notification-kind';
import { utcInstant } from '../../models/semantics/date-value';
import { entityId } from '../../models/semantics/entity-id';
import type { NotificationHttpResponse } from './generated/model/notification-http-response';
import type { NotificationPreferencesHttpResponse } from './generated/model/notification-preferences-http-response';
import type { WebPushSubscriptionHttpResponse } from './generated/model/web-push-subscription-http-response';
import { requireSdkFields, sdkNullableFields } from './sdk-response';

export function notificationFromSdk(response: NotificationHttpResponse): NotificationItem {
    const value = sdkNullableFields(requireSdkFields(response, ['id', 'type', 'title', 'isRead', 'createdAtUtc']), [
        'body',
        'targetUrl',
        'referenceId',
    ]);
    return {
        ...value,
        id: entityId<'notification'>(value.id),
        type: decodeNotificationKind(value.type),
        createdAtUtc: utcInstant(value.createdAtUtc),
    };
}

export function notificationPreferencesFromSdk(value: NotificationPreferencesHttpResponse): NotificationPreferences {
    return requireSdkFields(value, [
        'pushNotificationsEnabled',
        'fastingPushNotificationsEnabled',
        'socialPushNotificationsEnabled',
        'fastingCheckInReminderHours',
        'fastingCheckInFollowUpReminderHours',
    ]);
}

export function webPushSubscriptionFromSdk(response: WebPushSubscriptionHttpResponse): WebPushSubscriptionItem {
    return sdkNullableFields(requireSdkFields(response, ['endpoint', 'endpointHost', 'createdAtUtc']), [
        'expirationTimeUtc',
        'locale',
        'userAgent',
        'updatedAtUtc',
    ]);
}
