import { describe, expect, it } from 'vitest';

import type { WebPushSubscriptionItem } from '../../../../services/notification.service';
import { buildConnectedDeviceItems, buildNotificationsStatusKey, isCurrentConnectedDevice } from './user-manage-notifications.mapper';

const SUBSCRIPTION: WebPushSubscriptionItem = {
    endpoint: 'endpoint-1',
    endpointHost: 'push.example.test',
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/123.0.0.0 Safari/537.36',
    locale: 'ru',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-02T00:00:00Z',
    expirationTimeUtc: null,
};

describe('user manage notifications mapper', () => {
    it('should build connected device view models', () => {
        expect(
            buildConnectedDeviceItems(
                [SUBSCRIPTION],
                'endpoint-1',
                value => `date:${value}`,
                key => key,
            ),
        ).toEqual([
            {
                subscription: SUBSCRIPTION,
                label: 'Chrome / Windows',
                meta: 'push.example.test | RU | date:2026-01-02T00:00:00Z',
                isCurrent: true,
            },
        ]);
    });

    it('should detect current connected device', () => {
        expect(isCurrentConnectedDevice(SUBSCRIPTION, 'endpoint-1')).toBe(true);
        expect(isCurrentConnectedDevice(SUBSCRIPTION, 'endpoint-2')).toBe(false);
    });

    it('should prioritize notification status keys', () => {
        expect(
            buildNotificationsStatusKey({
                isSchedulingTestNotification: true,
                isRemovingConnectedDevice: true,
                isPushNotificationsBusy: true,
                isUpdatingNotifications: true,
            }),
        ).toBe('USER_MANAGE.NOTIFICATIONS_STATUS_TEST_SENDING');
        expect(
            buildNotificationsStatusKey({
                isSchedulingTestNotification: false,
                isRemovingConnectedDevice: true,
                isPushNotificationsBusy: true,
                isUpdatingNotifications: true,
            }),
        ).toBe('USER_MANAGE.NOTIFICATIONS_STATUS_DEVICE_REMOVING');
    });
});
