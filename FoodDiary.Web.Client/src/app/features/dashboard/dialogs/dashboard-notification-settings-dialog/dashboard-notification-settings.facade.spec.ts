import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { FrontendObservabilityService } from '../../../../services/frontend-observability.service';
import { NavigationService } from '../../../../services/navigation.service';
import { BrowserNotificationCapabilityService } from '../../../../shared/notifications/browser-notification-capability.service';
import { type NotificationPreferences, NotificationService } from '../../../../shared/notifications/notification.service';
import { type PushNotificationEnableResult, PushNotificationService } from '../../../../shared/notifications/push-notification.service';
import { DashboardNotificationSettingsFacade } from './dashboard-notification-settings.facade';

const PREFERENCES: NotificationPreferences = {
    pushNotificationsEnabled: true,
    fastingPushNotificationsEnabled: true,
    socialPushNotificationsEnabled: false,
    fastingCheckInReminderHours: 12,
    fastingCheckInFollowUpReminderHours: 20,
};

function setup(permission: NotificationPermission | 'unsupported' = 'default'): {
    facade: DashboardNotificationSettingsFacade;
    notifications: { getNotificationPreferences: ReturnType<typeof vi.fn>; updateNotificationPreferences: ReturnType<typeof vi.fn> };
    push: {
        isSupported: ReturnType<typeof signal<boolean>>;
        isSubscribed: ReturnType<typeof signal<boolean>>;
        isBusy: ReturnType<typeof signal<boolean>>;
        ensureSubscriptionAsync: ReturnType<typeof vi.fn>;
    };
    toast: { info: ReturnType<typeof vi.fn>; success: ReturnType<typeof vi.fn> };
    telemetry: {
        recordNotificationSettingsViewed: ReturnType<typeof vi.fn>;
        recordNotificationPreferenceChanged: ReturnType<typeof vi.fn>;
        recordNotificationSubscriptionEvent: ReturnType<typeof vi.fn>;
    };
    navigation: { navigateToProfileAsync: ReturnType<typeof vi.fn> };
} {
    TestBed.resetTestingModule();
    const notifications = {
        getNotificationPreferences: vi.fn(() => of(PREFERENCES)),
        updateNotificationPreferences: vi.fn((_update: unknown) => of(PREFERENCES)),
    };
    const push = {
        isSupported: signal(true),
        isSubscribed: signal(false),
        isBusy: signal(false),
        ensureSubscriptionAsync: vi.fn<() => Promise<PushNotificationEnableResult>>().mockResolvedValue('subscribed'),
    };
    const toast = { info: vi.fn(), success: vi.fn() };
    const telemetry = {
        recordNotificationSettingsViewed: vi.fn(),
        recordNotificationPreferenceChanged: vi.fn(),
        recordNotificationSubscriptionEvent: vi.fn(),
    };
    const navigation = { navigateToProfileAsync: vi.fn<() => Promise<boolean>>().mockResolvedValue(true) };
    TestBed.configureTestingModule({
        providers: [
            DashboardNotificationSettingsFacade,
            { provide: NotificationService, useValue: notifications },
            { provide: PushNotificationService, useValue: push },
            {
                provide: BrowserNotificationCapabilityService,
                useValue: { getPermission: (): NotificationPermission | 'unsupported' => permission },
            },
            { provide: TranslateService, useValue: { instant: (key: string): string => key } },
            { provide: FdUiToastService, useValue: toast },
            { provide: FrontendObservabilityService, useValue: telemetry },
            { provide: NavigationService, useValue: navigation },
        ],
    });
    return { facade: TestBed.inject(DashboardNotificationSettingsFacade), notifications, push, toast, telemetry, navigation };
}

describe('Dashboard notification status', () => {
    it.each(
        (
            [
                ['default', false, false, true, 'UNSUPPORTED', 'UNSUPPORTED'],
                ['denied', true, false, true, 'BLOCKED', 'BLOCKED'],
                ['granted', true, true, true, 'ENABLED', 'ENABLED'],
                ['default', true, false, false, 'DEVICE_IDLE', 'DISABLED'],
                ['default', true, false, true, 'SETUP_REQUIRED', 'SETUP_REQUIRED'],
            ] as const
        ).map(([permission, supported, subscribed, enabled, status, hint]) => ({
            permission,
            supported,
            subscribed,
            enabled,
            status,
            hint,
        })),
    )(
        'represents permission $permission, supported $supported, subscribed $subscribed, enabled $enabled',
        ({ permission, supported, subscribed, enabled, status, hint }) => {
            const { facade, push } = setup(permission);
            facade.load();
            push.isSupported.set(supported);
            push.isSubscribed.set(subscribed);
            facade.pushNotificationsEnabled.set(enabled);
            expect(facade.pushNotificationsDeviceStatusKey()).toBe(`USER_MANAGE.NOTIFICATIONS_STATUS_${status}`);
            expect(facade.pushNotificationsHintKey()).toBe(`USER_MANAGE.NOTIFICATIONS_${hint}_HINT`);
            expect(facade.pushNotificationsAccountStatusKey()).toBe(
                `USER_MANAGE.NOTIFICATIONS_ACCOUNT_STATUS_${enabled ? 'ENABLED' : 'DISABLED'}`,
            );
        },
    );
});

describe('Dashboard notification writes (1)', () => {
    it('disables account push without registering a device', () => {
        const { facade, notifications, push, toast } = setup();
        facade.load();
        notifications.updateNotificationPreferences.mockReturnValueOnce(of({ ...PREFERENCES, pushNotificationsEnabled: false }));
        facade.togglePushNotifications();
        expect(notifications.updateNotificationPreferences).toHaveBeenCalledWith({ pushNotificationsEnabled: false });
        expect(facade.pushNotificationsEnabled()).toBe(false);
        expect(push.ensureSubscriptionAsync).not.toHaveBeenCalled();
        expect(toast.info).toHaveBeenCalledWith('DASHBOARD.ACTIONS.PUSH_DISABLED');
    });
    it.each(['fasting', 'social'] as const)('toggles %s both ways and ignores duplicate requests', category => {
        const { facade, notifications } = setup();
        facade.load();
        const property = category === 'fasting' ? 'fastingPushNotificationsEnabled' : 'socialPushNotificationsEnabled';
        const toggle = (): void => {
            category === 'fasting' ? facade.toggleFastingPushNotifications() : facade.toggleSocialPushNotifications();
        };
        const next = !PREFERENCES[property];
        const pending = new Subject<NotificationPreferences>();
        notifications.updateNotificationPreferences.mockReturnValueOnce(pending);
        toggle();
        toggle();
        facade.togglePushNotifications();
        expect(notifications.updateNotificationPreferences).toHaveBeenCalledTimes(1);
        expect(notifications.updateNotificationPreferences).toHaveBeenCalledWith({ [property]: next });
        expect(facade.isUpdating()).toBe(true);
        pending.next({ ...PREFERENCES, [property]: next });
        pending.complete();
        expect(facade.isUpdating()).toBe(false);
        toggle();
        expect(notifications.updateNotificationPreferences).toHaveBeenLastCalledWith({ [property]: !next });
    });
});

describe('Dashboard notification writes (2)', () => {
    it('does not write when device registration is already busy', () => {
        const { facade, push, notifications } = setup();
        push.isBusy.set(true);
        facade.togglePushNotifications();
        expect(notifications.updateNotificationPreferences).not.toHaveBeenCalled();
    });
    it('preserves preferences on failure and clears the error on retry', () => {
        const { facade, notifications, toast } = setup();
        facade.load();
        notifications.updateNotificationPreferences.mockReturnValueOnce(throwError(() => new Error('offline')));
        facade.toggleFastingPushNotifications();
        expect(facade.fastingPushNotificationsEnabled()).toBe(true);
        expect(facade.submitError()).toBe('DASHBOARD.NOTIFICATIONS.ERROR');
        expect(facade.isUpdating()).toBe(false);
        expect(toast.info).not.toHaveBeenCalled();
        notifications.updateNotificationPreferences.mockReturnValueOnce(of({ ...PREFERENCES, fastingPushNotificationsEnabled: false }));
        facade.toggleFastingPushNotifications();
        expect(facade.submitError()).toBeNull();
        expect(facade.fastingPushNotificationsEnabled()).toBe(false);
    });
});

describe('Dashboard notification writes (3)', () => {
    it.each([
        ['subscribed', 'success', 'DASHBOARD.ACTIONS.PUSH_ENABLED'],
        ['already-subscribed', 'success', 'DASHBOARD.ACTIONS.PUSH_ENABLED'],
        ['unsupported', 'info', 'USER_MANAGE.NOTIFICATIONS_UNSUPPORTED_HINT'],
        ['blocked', 'info', 'USER_MANAGE.NOTIFICATIONS_BLOCKED_HINT'],
        ['unavailable', 'info', 'USER_MANAGE.NOTIFICATIONS_UNAVAILABLE_HINT'],
    ] as const)('reports device result %s accurately', async (result, toastType, key) => {
        const { facade, push, toast, telemetry } = setup();
        push.ensureSubscriptionAsync.mockResolvedValueOnce(result);
        facade.togglePushNotifications();
        await vi.waitFor(() => {
            expect(toast[toastType]).toHaveBeenCalledWith(key);
        });
        expect(toast[toastType]).toHaveBeenCalledWith(key);
        expect(telemetry.recordNotificationSubscriptionEvent).toHaveBeenCalledWith(
            'subscription.ensure',
            toastType === 'success' ? 'success' : result,
            expect.objectContaining({ result }),
        );
    });
    it('cancels a pending settings update on destruction', () => {
        const { facade, notifications, toast } = setup();
        const pending = new Subject<NotificationPreferences>();
        notifications.updateNotificationPreferences.mockReturnValueOnce(pending);
        facade.toggleSocialPushNotifications();
        TestBed.resetTestingModule();
        expect(pending.observed).toBe(false);
        expect(facade.isUpdating()).toBe(false);
        expect(toast.info).not.toHaveBeenCalled();
    });
});

describe('Dashboard notification writes (4)', () => {
    it('prevents duplicate profile navigation and releases busy state after rejection', async () => {
        const { facade, navigation } = setup();
        let rejectNavigation: (reason: Error) => void = () => {};
        navigation.navigateToProfileAsync.mockReturnValueOnce(
            new Promise<boolean>((_resolve, reject) => {
                rejectNavigation = reject;
            }),
        );
        const first = facade.openAdvancedSettingsAsync();
        await facade.openAdvancedSettingsAsync();
        expect(navigation.navigateToProfileAsync).toHaveBeenCalledTimes(1);
        expect(facade.isOpeningProfile()).toBe(true);
        rejectNavigation(new Error('navigation failed'));
        await expect(first).rejects.toThrow('navigation failed');
        expect(facade.isOpeningProfile()).toBe(false);
    });
});
