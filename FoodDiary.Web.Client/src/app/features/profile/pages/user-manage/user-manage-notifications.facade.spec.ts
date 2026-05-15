import { signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { FrontendObservabilityService } from '../../../../services/frontend-observability.service';
import { LocalizationService } from '../../../../services/localization.service';
import { NotificationService, type WebPushSubscriptionItem } from '../../../../services/notification.service';
import { PushNotificationService } from '../../../../services/push-notification.service';
import type { User } from '../../../../shared/models/user.data';
import { ProfileManageFacade } from '../../lib/profile-manage.facade';
import { UserManageNotificationsFacade } from './user-manage-notifications.facade';

const FIRST_REMINDER_HOURS = 6;
const FOLLOW_UP_REMINDER_HOURS = 14;
const MANUAL_FIRST_REMINDER_HOURS = 5;
const MANUAL_FOLLOW_UP_REMINDER_HOURS = 13;
const TEST_NOTIFICATION_DELAY_SECONDS = 20;

describe('UserManageNotificationsFacade sync', () => {
    it('syncs reminder state from user and tracks notifications view once', () => {
        const { facade, observability } = setup();
        const service = TestBed.inject(UserManageNotificationsFacade);

        service.syncFromUser(
            createUser({
                fastingCheckInReminderHours: FIRST_REMINDER_HOURS,
                fastingCheckInFollowUpReminderHours: FOLLOW_UP_REMINDER_HOURS,
            }),
        );
        service.syncFromUser(createUser());

        expect(service.fastingCheckInReminderHours()).toBe(FIRST_REMINDER_HOURS);
        expect(service.fastingCheckInFollowUpReminderHours()).toBe(FOLLOW_UP_REMINDER_HOURS);
        expect(observability.recordNotificationSettingsViewed).toHaveBeenCalledTimes(1);
        expect(facade.updateNotificationPreferencesAsync).not.toHaveBeenCalled();
    });
});

describe('UserManageNotificationsFacade push preferences', () => {
    it('enables push notifications and refreshes subscriptions after successful browser subscription', async () => {
        const { facade, pushNotifications, toast, observability } = setup({
            user: createUser({ pushNotificationsEnabled: false }),
            updateResult: createUser({ pushNotificationsEnabled: true }),
            ensureSubscriptionResult: 'subscribed',
        });
        const service = TestBed.inject(UserManageNotificationsFacade);

        await service.togglePushNotificationsAsync();

        expect(facade.updateNotificationPreferencesAsync).toHaveBeenCalledWith({ pushNotificationsEnabled: true });
        expect(pushNotifications.ensureSubscriptionAsync).toHaveBeenCalledTimes(1);
        expect(facade.refreshWebPushSubscriptions).toHaveBeenCalledTimes(1);
        expect(toast.success).toHaveBeenCalledWith('DASHBOARD.ACTIONS.PUSH_ENABLED');
        expect(observability.recordNotificationSubscriptionEvent).toHaveBeenCalledWith('subscription.ensure', 'success', {
            result: 'subscribed',
        });
    });

    it('disables push notifications without touching browser subscription', async () => {
        const { facade, pushNotifications, toast, observability } = setup({
            user: createUser({ pushNotificationsEnabled: true }),
            updateResult: createUser({ pushNotificationsEnabled: false }),
        });
        const service = TestBed.inject(UserManageNotificationsFacade);

        await service.togglePushNotificationsAsync();

        expect(facade.updateNotificationPreferencesAsync).toHaveBeenCalledWith({ pushNotificationsEnabled: false });
        expect(pushNotifications.ensureSubscriptionAsync).not.toHaveBeenCalled();
        expect(observability.recordNotificationPreferenceChanged).toHaveBeenCalledWith('push', false, {
            permission: service.notificationPermission(),
        });
        expect(toast.info).toHaveBeenCalledWith('DASHBOARD.ACTIONS.PUSH_DISABLED');
    });

    it.each([
        ['unsupported', 'USER_MANAGE.NOTIFICATIONS_UNSUPPORTED_HINT'],
        ['blocked', 'USER_MANAGE.NOTIFICATIONS_BLOCKED_HINT'],
        ['unavailable', 'USER_MANAGE.NOTIFICATIONS_UNAVAILABLE_HINT'],
    ] as const)('shows informational toast when browser subscription result is %s', async (result, messageKey) => {
        const { facade, toast, observability } = setup({
            user: createUser({ pushNotificationsEnabled: false }),
            updateResult: createUser({ pushNotificationsEnabled: true }),
            ensureSubscriptionResult: result,
        });
        const service = TestBed.inject(UserManageNotificationsFacade);

        await service.togglePushNotificationsAsync();

        expect(facade.refreshWebPushSubscriptions).not.toHaveBeenCalled();
        expect(observability.recordNotificationSubscriptionEvent).toHaveBeenCalledWith('subscription.ensure', result, { result });
        expect(toast.info).toHaveBeenCalledWith(messageKey);
    });
});

describe('UserManageNotificationsFacade connected devices', () => {
    it('removes current device through push subscription service', async () => {
        const { facade, pushNotifications, toast, observability } = setup();
        const service = TestBed.inject(UserManageNotificationsFacade);
        const subscription = createSubscription('https://push.example/current');
        pushNotifications.currentSubscriptionEndpoint.set(subscription.endpoint);

        await service.removeConnectedDeviceAsync(subscription);

        expect(pushNotifications.removeSubscriptionAsync).toHaveBeenCalledWith(subscription.endpoint);
        expect(facade.removeWebPushSubscriptionAsync).not.toHaveBeenCalled();
        expect(facade.refreshWebPushSubscriptions).toHaveBeenCalledTimes(1);
        expect(observability.recordNotificationSubscriptionEvent).toHaveBeenCalledWith('subscription.remove', 'success', {
            currentDevice: true,
        });
        expect(toast.info).toHaveBeenCalledWith('USER_MANAGE.NOTIFICATIONS_DEVICE_REMOVED_TOAST');
    });

    it('reports an error when removing connected device fails', async () => {
        const { facade, toast, observability } = setup({ removeWebPushSubscriptionResult: false });
        const service = TestBed.inject(UserManageNotificationsFacade);
        const subscription = createSubscription('https://push.example/other');

        await service.removeConnectedDeviceAsync(subscription);

        expect(facade.removeWebPushSubscriptionAsync).toHaveBeenCalledWith(subscription.endpoint);
        expect(facade.refreshWebPushSubscriptions).not.toHaveBeenCalled();
        expect(observability.recordNotificationSubscriptionEvent).toHaveBeenCalledWith('subscription.remove', 'failed', {
            currentDevice: false,
        });
        expect(toast.error).toHaveBeenCalledWith('USER_MANAGE.NOTIFICATIONS_DEVICE_REMOVE_ERROR');
    });
});

describe('UserManageNotificationsFacade fasting reminders', () => {
    it('rejects invalid fasting reminder order before saving preferences', async () => {
        const { facade, toast } = setup();
        const service = TestBed.inject(UserManageNotificationsFacade);
        service.fastingCheckInReminderHours.set(FOLLOW_UP_REMINDER_HOURS);
        service.fastingCheckInFollowUpReminderHours.set(FIRST_REMINDER_HOURS);

        await service.saveFastingReminderHoursAsync();

        expect(facade.updateNotificationPreferencesAsync).not.toHaveBeenCalled();
        expect(toast.error).toHaveBeenCalledWith('USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_ERROR');
    });

    it('saves valid fasting reminder hours and records manual timing', async () => {
        const { facade, toast, observability } = setup();
        const service = TestBed.inject(UserManageNotificationsFacade);
        service.fastingCheckInReminderHours.set(MANUAL_FIRST_REMINDER_HOURS);
        service.fastingCheckInFollowUpReminderHours.set(MANUAL_FOLLOW_UP_REMINDER_HOURS);

        await service.saveFastingReminderHoursAsync();

        expect(facade.updateNotificationPreferencesAsync).toHaveBeenCalledWith({
            fastingCheckInReminderHours: MANUAL_FIRST_REMINDER_HOURS,
            fastingCheckInFollowUpReminderHours: MANUAL_FOLLOW_UP_REMINDER_HOURS,
        });
        expect(observability.recordFastingReminderTimingSaved).toHaveBeenCalledWith({
            firstReminderHours: MANUAL_FIRST_REMINDER_HOURS,
            followUpReminderHours: MANUAL_FOLLOW_UP_REMINDER_HOURS,
            source: 'manual',
            presetId: undefined,
        });
        expect(toast.info).toHaveBeenCalledWith('USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_SAVED');
    });
});

describe('UserManageNotificationsFacade test notifications', () => {
    it('records test notification schedule success and failure', () => {
        const successSetup = setup();
        const successService = TestBed.inject(UserManageNotificationsFacade);

        successService.scheduleTestNotification();

        expect(successSetup.observability.recordNotificationSubscriptionEvent).toHaveBeenCalledWith('test-push.schedule', 'success', {
            type: 'FastingCompleted',
            delaySeconds: TEST_NOTIFICATION_DELAY_SECONDS,
        });
        expect(successSetup.toast.info).toHaveBeenCalledWith('DASHBOARD.ACTIONS.TEST_PUSH_SCHEDULED');
        expect(successService.isSchedulingTestNotification()).toBe(false);

        const failureSetup = setup({ scheduleTestNotificationFails: true });
        const failureService = TestBed.inject(UserManageNotificationsFacade);

        failureService.scheduleTestNotification();

        expect(failureSetup.observability.recordNotificationSubscriptionEvent).toHaveBeenCalledWith('test-push.schedule', 'failed', {
            type: 'FastingCompleted',
            delaySeconds: TEST_NOTIFICATION_DELAY_SECONDS,
        });
        expect(failureSetup.toast.error).toHaveBeenCalledWith('DASHBOARD.ACTIONS.TEST_PUSH_ERROR');
        expect(failureService.isSchedulingTestNotification()).toBe(false);
    });
});

type EnsureSubscriptionResult = Awaited<ReturnType<PushNotificationService['ensureSubscriptionAsync']>>;

type TestSetupOptions = {
    user?: User;
    updateResult?: User | null;
    ensureSubscriptionResult?: EnsureSubscriptionResult;
    removeWebPushSubscriptionResult?: boolean;
    scheduleTestNotificationFails?: boolean;
};

type ProfileManageFacadeMock = {
    user: WritableSignal<User | null>;
    isUpdatingNotifications: ReturnType<typeof signal<boolean>>;
    webPushSubscriptions: ReturnType<typeof signal<WebPushSubscriptionItem[]>>;
    isLoadingWebPushSubscriptions: ReturnType<typeof signal<boolean>>;
    removingWebPushSubscriptionEndpoint: ReturnType<typeof signal<string | null>>;
    updateNotificationPreferencesAsync: ReturnType<typeof vi.fn>;
    refreshWebPushSubscriptions: ReturnType<typeof vi.fn>;
    removeWebPushSubscriptionAsync: ReturnType<typeof vi.fn>;
};

function setup(options: TestSetupOptions = {}): {
    facade: ProfileManageFacadeMock;
    pushNotifications: ReturnType<typeof createPushNotificationServiceMock>;
    toast: ReturnType<typeof createToastServiceMock>;
    observability: ReturnType<typeof createFrontendObservabilityServiceMock>;
} {
    TestBed.resetTestingModule();

    const facade = createProfileManageFacadeMock(options);
    const pushNotifications = createPushNotificationServiceMock(options.ensureSubscriptionResult ?? 'subscribed');
    const toast = createToastServiceMock();
    const observability = createFrontendObservabilityServiceMock();
    const notificationService = createNotificationServiceMock(options.scheduleTestNotificationFails === true);

    TestBed.configureTestingModule({
        imports: [TranslateModule.forRoot()],
        providers: [
            UserManageNotificationsFacade,
            { provide: ProfileManageFacade, useValue: facade },
            { provide: PushNotificationService, useValue: pushNotifications },
            { provide: NotificationService, useValue: notificationService },
            { provide: FdUiToastService, useValue: toast },
            { provide: FrontendObservabilityService, useValue: observability },
            { provide: LocalizationService, useValue: { getCurrentLanguage: vi.fn(() => 'en') } },
        ],
    });

    const translateService = TestBed.inject(TranslateService);
    vi.spyOn(translateService, 'instant').mockImplementation((key: string) => key);
    translateService.setFallbackLang('en');
    translateService.use('en');

    return { facade, pushNotifications, toast, observability };
}

function createProfileManageFacadeMock(options: TestSetupOptions): ProfileManageFacadeMock {
    return {
        user: signal<User | null>(options.user ?? createUser()),
        isUpdatingNotifications: signal(false),
        webPushSubscriptions: signal([]),
        isLoadingWebPushSubscriptions: signal(false),
        removingWebPushSubscriptionEndpoint: signal<string | null>(null),
        updateNotificationPreferencesAsync: vi.fn().mockResolvedValue(options.updateResult ?? options.user ?? createUser()),
        refreshWebPushSubscriptions: vi.fn(),
        removeWebPushSubscriptionAsync: vi.fn().mockResolvedValue(options.removeWebPushSubscriptionResult ?? true),
    };
}

function createPushNotificationServiceMock(result: EnsureSubscriptionResult): {
    isSupported: ReturnType<typeof signal<boolean>>;
    isSubscribed: ReturnType<typeof signal<boolean>>;
    isBusy: ReturnType<typeof signal<boolean>>;
    currentSubscriptionEndpoint: ReturnType<typeof signal<string | null>>;
    ensureSubscriptionAsync: ReturnType<typeof vi.fn>;
    removeSubscriptionAsync: ReturnType<typeof vi.fn>;
} {
    return {
        isSupported: signal(true),
        isSubscribed: signal(false),
        isBusy: signal(false),
        currentSubscriptionEndpoint: signal<string | null>(null),
        ensureSubscriptionAsync: vi.fn().mockResolvedValue(result),
        removeSubscriptionAsync: vi.fn().mockResolvedValue(true),
    };
}

function createNotificationServiceMock(shouldFail: boolean): {
    notificationsChangedVersion: ReturnType<typeof signal<number>>;
    scheduleTestNotification: ReturnType<typeof vi.fn>;
} {
    return {
        notificationsChangedVersion: signal(0),
        scheduleTestNotification: vi.fn().mockReturnValue(shouldFail ? throwError(() => new Error('Schedule failed')) : of(undefined)),
    };
}

function createToastServiceMock(): {
    success: ReturnType<typeof vi.fn>;
    info: ReturnType<typeof vi.fn>;
    error: ReturnType<typeof vi.fn>;
} {
    return {
        success: vi.fn(),
        info: vi.fn(),
        error: vi.fn(),
    };
}

function createFrontendObservabilityServiceMock(): {
    recordNotificationSettingsViewed: ReturnType<typeof vi.fn>;
    recordNotificationPreferenceChanged: ReturnType<typeof vi.fn>;
    recordNotificationSubscriptionEvent: ReturnType<typeof vi.fn>;
    recordFastingReminderPresetSelected: ReturnType<typeof vi.fn>;
    recordFastingReminderTimingSaved: ReturnType<typeof vi.fn>;
} {
    return {
        recordNotificationSettingsViewed: vi.fn(),
        recordNotificationPreferenceChanged: vi.fn(),
        recordNotificationSubscriptionEvent: vi.fn(),
        recordFastingReminderPresetSelected: vi.fn(),
        recordFastingReminderTimingSaved: vi.fn(),
    };
}

function createUser(overrides: Partial<User> = {}): User {
    return {
        id: 'user-1',
        email: 'user@example.com',
        username: 'alexi',
        firstName: 'Alex',
        lastName: 'User',
        hasPassword: true,
        pushNotificationsEnabled: true,
        fastingPushNotificationsEnabled: true,
        socialPushNotificationsEnabled: true,
        fastingCheckInReminderHours: FIRST_REMINDER_HOURS,
        fastingCheckInFollowUpReminderHours: FOLLOW_UP_REMINDER_HOURS,
        isActive: true,
        isEmailConfirmed: true,
        ...overrides,
    };
}

function createSubscription(endpoint: string): WebPushSubscriptionItem {
    return {
        endpoint,
        endpointHost: 'push.example',
        expirationTimeUtc: null,
        locale: 'en',
        userAgent: 'Mozilla/5.0 Chrome/120 Windows',
        createdAtUtc: '2026-05-15T08:00:00Z',
        updatedAtUtc: null,
    };
}
