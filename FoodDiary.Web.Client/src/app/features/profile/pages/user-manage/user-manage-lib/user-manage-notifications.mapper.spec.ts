import { describe, expect, it } from 'vitest';

import type { WebPushSubscriptionItem } from '../../../../../shared/notifications/notification.service';
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

describe('Connected device display for saved browser metadata', () => {
    it.each([
        {
            label: 'Opera / Windows',
            userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/123.0.0.0 Safari/537.36 OPR/109.0.0.0',
        },
        {
            label: 'Opera / Linux',
            userAgent: 'Opera/9.80 (X11; Linux x86_64; U; en) Presto/2.12.388 Version/12.16',
        },
        {
            label: 'Safari / iOS',
            userAgent: 'Mozilla/5.0 (iPad; CPU OS 16_6 like Mac OS X) AppleWebKit/605.1.15 Version/16.6 Mobile/15E148 Safari/604.1',
        },
    ])('identifies $label from an existing subscription', ({ label, userAgent }) => {
        const savedDevice = { ...SUBSCRIPTION, userAgent };

        const devices = buildConnectedDeviceItems(
            [savedDevice],
            null,
            () => null,
            key => key,
        );

        expect(devices).toEqual([{ subscription: savedDevice, label, meta: 'push.example.test | RU', isCurrent: false }]);
    });

    it('uses the localized generic label and creation date for a device with absent optional metadata', () => {
        const savedDevice = { ...SUBSCRIPTION, userAgent: null, updatedAtUtc: null, locale: null };

        const devices = buildConnectedDeviceItems(
            [savedDevice],
            SUBSCRIPTION.endpoint,
            value => `date:${value}`,
            key => (key === 'USER_MANAGE.NOTIFICATIONS_DEVICE_GENERIC' ? 'Неизвестное устройство' : key),
        );

        expect(devices).toEqual([
            {
                subscription: savedDevice,
                label: 'Неизвестное устройство',
                meta: `push.example.test | date:${SUBSCRIPTION.createdAtUtc}`,
                isCurrent: true,
            },
        ]);
    });

    it.each([
        { busy: true, saving: true, status: 'USER_MANAGE.NOTIFICATIONS_STATUS_DEVICE_SYNCING' },
        { busy: false, saving: true, status: 'USER_MANAGE.NOTIFICATIONS_STATUS_SAVING' },
        { busy: false, saving: false, status: null },
    ])('uses notification status $status while device sync is $busy and settings save is $saving', ({ busy, saving, status }) => {
        expect(
            buildNotificationsStatusKey({
                isSchedulingTestNotification: false,
                isRemovingConnectedDevice: false,
                isPushNotificationsBusy: busy,
                isUpdatingNotifications: saving,
            }),
        ).toBe(status);
    });
});
