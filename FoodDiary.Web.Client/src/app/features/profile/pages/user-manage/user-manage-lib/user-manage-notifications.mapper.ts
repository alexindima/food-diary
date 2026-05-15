import type { WebPushSubscriptionItem } from '../../../../../services/notification.service';
import type { ConnectedDeviceViewModel } from './user-manage.types';

type DeviceLabelMatcher = {
    label: string;
    matches: (userAgent: string) => boolean;
};

const BROWSER_LABEL_MATCHERS: readonly DeviceLabelMatcher[] = [
    { label: 'Edge', matches: userAgent => userAgent.includes('edg/') },
    { label: 'Opera', matches: userAgent => userAgent.includes('opr/') || userAgent.includes('opera') },
    { label: 'Chrome', matches: userAgent => userAgent.includes('chrome/') && !userAgent.includes('edg/') && !userAgent.includes('opr/') },
    { label: 'Firefox', matches: userAgent => userAgent.includes('firefox/') },
    { label: 'Safari', matches: userAgent => userAgent.includes('safari/') && !userAgent.includes('chrome/') },
];

const PLATFORM_LABEL_MATCHERS: readonly DeviceLabelMatcher[] = [
    { label: 'iOS', matches: userAgent => userAgent.includes('iphone') || userAgent.includes('ipad') || userAgent.includes('ios') },
    { label: 'Android', matches: userAgent => userAgent.includes('android') },
    { label: 'Windows', matches: userAgent => userAgent.includes('windows') },
    { label: 'macOS', matches: userAgent => userAgent.includes('mac os') || userAgent.includes('macintosh') },
    { label: 'Linux', matches: userAgent => userAgent.includes('linux') },
];

export type NotificationStatusState = {
    isSchedulingTestNotification: boolean;
    isRemovingConnectedDevice: boolean;
    isPushNotificationsBusy: boolean;
    isUpdatingNotifications: boolean;
};

export function buildConnectedDeviceItems(
    subscriptions: readonly WebPushSubscriptionItem[],
    currentEndpoint: string | null,
    formatDateTime: (value: string | null) => string | null,
    translate: (key: string) => string,
): ConnectedDeviceViewModel[] {
    return subscriptions.map(subscription => ({
        subscription,
        label: buildConnectedDeviceLabel(subscription.userAgent, translate),
        meta: buildConnectedDeviceMeta(subscription, formatDateTime),
        isCurrent: isCurrentConnectedDevice(subscription, currentEndpoint),
    }));
}

export function isCurrentConnectedDevice(subscription: WebPushSubscriptionItem, currentEndpoint: string | null): boolean {
    return subscription.endpoint.length > 0 && subscription.endpoint === currentEndpoint;
}

export function buildNotificationsStatusKey(state: NotificationStatusState): string | null {
    if (state.isSchedulingTestNotification) {
        return 'USER_MANAGE.NOTIFICATIONS_STATUS_TEST_SENDING';
    }

    if (state.isRemovingConnectedDevice) {
        return 'USER_MANAGE.NOTIFICATIONS_STATUS_DEVICE_REMOVING';
    }

    if (state.isPushNotificationsBusy) {
        return 'USER_MANAGE.NOTIFICATIONS_STATUS_DEVICE_SYNCING';
    }

    if (state.isUpdatingNotifications) {
        return 'USER_MANAGE.NOTIFICATIONS_STATUS_SAVING';
    }

    return null;
}

function buildConnectedDeviceLabel(userAgent: string | null, translate: (key: string) => string): string {
    const browser = getDeviceLabel(userAgent, BROWSER_LABEL_MATCHERS) ?? translate('USER_MANAGE.NOTIFICATIONS_DEVICE_GENERIC');
    const platform = getDeviceLabel(userAgent, PLATFORM_LABEL_MATCHERS);
    return platform !== null ? `${browser} / ${platform}` : browser;
}

function buildConnectedDeviceMeta(subscription: WebPushSubscriptionItem, formatDateTime: (value: string | null) => string | null): string {
    const segments = [
        subscription.endpointHost,
        subscription.locale?.toUpperCase() ?? null,
        formatDateTime(subscription.updatedAtUtc ?? subscription.createdAtUtc),
    ].filter((value): value is string => value !== null && value.length > 0);

    return segments.join(' | ');
}

function getDeviceLabel(userAgent: string | null, matchers: readonly DeviceLabelMatcher[]): string | null {
    const normalized = userAgent?.toLowerCase() ?? '';
    return matchers.find(matcher => matcher.matches(normalized))?.label ?? null;
}
