import { getStringProperty } from '../lib/unknown-value.utils';
import { entityId, type NotificationId } from '../models/semantics/entity-id';

export type PushNotificationReference = { notificationId: NotificationId; userId: string };
const MAX_USER_ID_LENGTH = 128;

export function parsePushNotificationReference(value: unknown): PushNotificationReference | null {
    const notificationId = parsePushNotificationId(getStringProperty(value, 'notificationId'));
    const userId = getStringProperty(value, 'userId');
    return notificationId !== null && userId !== undefined && userId.length > 0 && userId.length <= MAX_USER_ID_LENGTH
        ? { notificationId, userId }
        : null;
}

export function parsePushNotificationId(value: unknown): NotificationId | null {
    if (typeof value !== 'string' || !/^(?:[\da-f]{32}|[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12})$/i.test(value)) {
        return null;
    }
    const compact = value.replaceAll('-', '').toLowerCase();
    if (!/^[\da-f]{32}$/.test(compact) || /^0+$/.test(compact)) {
        return null;
    }
    return entityId<'notification'>(compact.replace(/^([\da-f]{8})([\da-f]{4})([\da-f]{4})([\da-f]{4})([\da-f]{12})$/, '$1-$2-$3-$4-$5'));
}
