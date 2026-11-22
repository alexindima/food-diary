import { ChangeDetectionStrategy, Component, type Provider, signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, type Routes } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { SwPush } from '@angular/service-worker';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { defer, firstValueFrom, type Observable, of, ReplaySubject, Subject } from 'rxjs';
import { describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { unsavedChangesGuard } from '../../../../guards/unsaved-changes.guard';
import { AuthService } from '../../../../services/auth.service';
import { FrontendObservabilityService } from '../../../../services/frontend-observability.service';
import { type UserProfileOverview, UserService } from '../../../../shared/api/user.service';
import { TelegramBackupEmailFlowService } from '../../../../shared/auth/telegram-backup-email-flow.service';
import { TelegramUnlinkFlowService } from '../../../../shared/auth/telegram-unlink-flow.service';
import { TelegramWebAppService } from '../../../../shared/auth/telegram-web-app.service';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { BrowserNotificationCapabilityService } from '../../../../shared/notifications/browser-notification-capability.service';
import {
    type NotificationPreferences,
    NotificationService,
    type ScheduledNotificationResponse,
    type WebPushSubscriptionItem,
} from '../../../../shared/notifications/notification.service';
import { PushNotificationService } from '../../../../shared/notifications/push-notification.service';
import { ThemeService } from '../../../../shared/theme/theme.service';
import { DIETOLOGIST_RELATIONSHIP_ACTIONS } from '../../../dietologist/contracts/relationship-actions';
import { ProfileMeasurementsService } from '../../api/profile-measurements.service';
import { TEST_NOTIFICATION_DELAY_SECONDS } from '../../lib/user-manage.config';
import { UserManageComponent } from './user-manage';

const initialPreferences: NotificationPreferences = {
    pushNotificationsEnabled: true,
    fastingPushNotificationsEnabled: true,
    socialPushNotificationsEnabled: true,
    fastingCheckInReminderHours: 12,
    fastingCheckInFollowUpReminderHours: 20,
};
const localDevice: WebPushSubscriptionItem = {
    endpoint: 'https://push.example/device/current',
    endpointHost: 'push.example',
    locale: 'ru',
    userAgent: 'Chrome/120 Windows',
    expirationTimeUtc: null,
    createdAtUtc: '2026-05-15T08:00:00Z',
    updatedAtUtc: null,
};
const remoteDevice: WebPushSubscriptionItem = {
    ...localDevice,
    endpoint: 'https://push.example/device/remote',
    locale: 'en',
    userAgent: 'Firefox/100 Linux',
};
const knownDevices = [localDevice, remoteDevice];

type ProfileNotificationBoundaries = {
    account: ReturnType<typeof createAccountMocks>;
    browser: ReturnType<typeof createBrowserMocks>;
    toast: { info: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; success: ReturnType<typeof vi.fn> };
    recordSubscription: Mock<FrontendObservabilityService['recordNotificationSubscriptionEvent']>;
};

@Component({ template: '<h1>Diary opened</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class NotificationDepartureComponent {}

function setup(
    preferences = initialPreferences,
    devices = knownDevices,
    providers: Provider[] = [],
): ProfileNotificationBoundaries & { fixture: ComponentFixture<UserManageComponent> } {
    const boundaries = configureProfileNotifications(preferences, [], devices, providers);
    const fixture = TestBed.createComponent(UserManageComponent);
    fixture.detectChanges();
    openNotificationSettings(fixture.nativeElement as HTMLElement);
    fixture.detectChanges();
    return { fixture, ...boundaries };
}

function configureProfileNotifications(
    preferences: NotificationPreferences,
    routes: Routes,
    devices = knownDevices,
    providers: Provider[] = [],
): ProfileNotificationBoundaries {
    const account = createAccountMocks(devices);
    const browser = createBrowserMocks();
    const toast = { info: vi.fn(), error: vi.fn(), success: vi.fn() };
    const recordSubscription = vi.fn<FrontendObservabilityService['recordNotificationSubscriptionEvent']>();
    TestBed.configureTestingModule({
        imports: [UserManageComponent],
        providers: [
            provideRouter(routes),
            provideTranslateTesting(),
            ...createProfileProviders(preferences, recordSubscription, devices),
            { provide: NotificationService, useValue: account },
            { provide: PushNotificationService, useValue: browser.push },
            { provide: BrowserNotificationCapabilityService, useValue: browser.capability },
            { provide: FdUiToastService, useValue: toast },
            ...providers,
        ],
    });
    const translation = TestBed.inject(TranslateService);
    translation.setFallbackLang('en');
    translation.use('en');
    return { account, browser, toast, recordSubscription };
}

async function setupRoutedProfileAsync(): Promise<ProfileNotificationBoundaries & { harness: RouterTestingHarness; profile: HTMLElement }> {
    const boundaries = configureProfileNotifications(initialPreferences, [
        { path: 'profile', component: UserManageComponent, canDeactivate: [unsavedChangesGuard] },
        { path: 'diary', component: NotificationDepartureComponent },
    ]);
    const harness = await RouterTestingHarness.create('/profile');
    const profile = harness.routeNativeElement;
    if (profile === null) {
        throw new Error('Expected the routed account profile');
    }
    openNotificationSettings(profile);
    harness.detectChanges();
    return { ...boundaries, harness, profile };
}

function openNotificationSettings(profile: ParentNode): void {
    const summary = profile.querySelector('details:has(fd-user-manage-notifications-card) > summary');
    if (!(summary instanceof HTMLElement)) {
        throw new Error('Expected the profile notification settings section');
    }
    summary.click();
}

function createAccountMocks(devices: WebPushSubscriptionItem[]): {
    notificationsChangedVersion: WritableSignal<number>;
    getWebPushConfiguration: Mock<NotificationService['getWebPushConfiguration']>;
    upsertWebPushSubscription: Mock<NotificationService['upsertWebPushSubscription']>;
    updateNotificationPreferences: Mock<NotificationService['updateNotificationPreferences']>;
    scheduleTestNotification: Mock<NotificationService['scheduleTestNotification']>;
    removeWebPushSubscription: Mock<NotificationService['removeWebPushSubscription']>;
    getWebPushSubscriptions: Mock<NotificationService['getWebPushSubscriptions']>;
} {
    return {
        notificationsChangedVersion: signal(0),
        getWebPushConfiguration: vi
            .fn<NotificationService['getWebPushConfiguration']>()
            .mockReturnValue(of({ enabled: true, publicKey: 'public-key' })),
        upsertWebPushSubscription: vi.fn<NotificationService['upsertWebPushSubscription']>().mockReturnValue(of(void 0)),
        updateNotificationPreferences: vi
            .fn<NotificationService['updateNotificationPreferences']>()
            .mockReturnValue(of(initialPreferences)),
        scheduleTestNotification: vi.fn<NotificationService['scheduleTestNotification']>(),
        removeWebPushSubscription: vi.fn<NotificationService['removeWebPushSubscription']>().mockReturnValue(of(void 0)),
        getWebPushSubscriptions: vi.fn<NotificationService['getWebPushSubscriptions']>().mockReturnValue(of(devices)),
    };
}

function createBrowserMocks(): {
    push: {
        canCheckBrowserSubscription: boolean;
        isSupported: WritableSignal<boolean>;
        isSubscribed: WritableSignal<boolean>;
        isBusy: WritableSignal<boolean>;
        currentSubscriptionEndpoint: WritableSignal<string | null>;
        ensureSubscriptionAsync: Mock<PushNotificationService['ensureSubscriptionAsync']>;
        removeSubscriptionAsync: Mock<PushNotificationService['removeSubscriptionAsync']>;
    };
    capability: {
        getPermission: Mock<BrowserNotificationCapabilityService['getPermission']>;
        requestPermissionAsync: Mock<BrowserNotificationCapabilityService['requestPermissionAsync']>;
        isPermissionDenied: BrowserNotificationCapabilityService['isPermissionDenied'];
        getUserAgent: BrowserNotificationCapabilityService['getUserAgent'];
    };
} {
    return {
        push: {
            canCheckBrowserSubscription: true,
            isSupported: signal(true),
            isSubscribed: signal(true),
            isBusy: signal(false),
            currentSubscriptionEndpoint: signal<string | null>(localDevice.endpoint),
            ensureSubscriptionAsync: vi.fn<PushNotificationService['ensureSubscriptionAsync']>().mockResolvedValue('subscribed'),
            removeSubscriptionAsync: vi.fn<PushNotificationService['removeSubscriptionAsync']>().mockResolvedValue(true),
        },
        capability: {
            getPermission: vi.fn<BrowserNotificationCapabilityService['getPermission']>().mockReturnValue('granted'),
            requestPermissionAsync: vi.fn<BrowserNotificationCapabilityService['requestPermissionAsync']>(),
            isPermissionDenied: () => false,
            getUserAgent: () => localDevice.userAgent ?? '',
        },
    };
}

function createProfileProviders(
    preferences: NotificationPreferences,
    recordSubscription: ProfileNotificationBoundaries['recordSubscription'],
    devices: WebPushSubscriptionItem[],
): Provider[] {
    const overview: UserProfileOverview = {
        user: {
            ...preferences,
            id: entityId<'user'>('profile-user'),
            email: 'profile@example.com',
            hasPassword: true,
            isEmailConfirmed: true,
            isActive: true,
        },
        notificationPreferences: preferences,
        webPushSubscriptions: devices,
        dietologistRelationship: null,
    };
    return [
        { provide: UserService, useValue: { getOverview: (): ReturnType<UserService['getOverview']> => of(overview) } },
        { provide: ProfileMeasurementsService, useValue: { getLatest: () => of({ weightKg: null, waistCm: null }) } },
        { provide: DIETOLOGIST_RELATIONSHIP_ACTIONS, useValue: { getRelationship: () => of(null) } },
        { provide: AuthService, useValue: { isAdmin: () => false } },
        {
            provide: LocalizationService,
            useValue: { getCurrentLanguage: (): string => 'en', applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(undefined) },
        },
        { provide: ThemeService, useValue: { syncWithUserPreferences: vi.fn() } },
        { provide: TelegramWebAppService, useValue: {} },
        { provide: TelegramBackupEmailFlowService, useValue: { read: () => null } },
        { provide: TelegramUnlinkFlowService, useValue: { consumeError: () => null } },
        {
            provide: FrontendObservabilityService,
            useValue: {
                recordNotificationSettingsViewed: vi.fn(),
                recordNotificationPreferenceChanged: vi.fn(),
                recordNotificationSubscriptionEvent: recordSubscription,
                recordFastingReminderPresetSelected: vi.fn(),
                recordFastingReminderTimingSaved: vi.fn(),
            },
        },
    ];
}

function element(fixture: ComponentFixture<UserManageComponent>, selector: string): HTMLElement {
    const found = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>(selector);
    if (found === null) {
        throw new Error(`Expected an element matching ${selector}`);
    }
    return found;
}

function categorySwitch(fixture: ComponentFixture<UserManageComponent>, category: 'SOCIAL' | 'FASTING'): HTMLButtonElement {
    const toggle = element(fixture, `[role="switch"][aria-label="USER_MANAGE.${category}_NOTIFICATIONS"]`);
    if (!(toggle instanceof HTMLButtonElement)) {
        throw new TypeError(`Expected a button for ${category} notifications`);
    }
    return toggle;
}

function removeButton(fixture: ComponentFixture<UserManageComponent>, deviceName: string): HTMLButtonElement {
    return deviceRemoveButton(fixture.nativeElement as HTMLElement, deviceName);
}

function deviceRemoveButton(profile: ParentNode, deviceName: string): HTMLButtonElement {
    const rows = profile.querySelectorAll<HTMLElement>('.user-manage__notifications-device-item');
    const button = Array.from(rows)
        .find(row => row.textContent.includes(deviceName))
        ?.querySelector('button');
    if (button === undefined || button === null) {
        throw new Error(`Expected a remove button for ${deviceName}`);
    }
    return button;
}

describe('Profile notification settings', () => {
    it('rejects competing clicks before the switches render as disabled and applies the server state', async () => {
        const { fixture, account, toast } = setup();
        const save = new Subject<NotificationPreferences>();
        account.updateNotificationPreferences.mockReturnValueOnce(save);
        const fasting = categorySwitch(fixture, 'FASTING');
        const social = categorySwitch(fixture, 'SOCIAL');
        const accountSwitch = notificationButton(fixture, '[role="switch"][aria-label="USER_MANAGE.PUSH_NOTIFICATIONS"]');

        fasting.click();
        expect(fasting.disabled || social.disabled || accountSwitch.disabled).toBe(false);
        fasting.click();
        social.click();
        accountSwitch.click();
        expect(account.updateNotificationPreferences).toHaveBeenCalledExactlyOnceWith({ fastingPushNotificationsEnabled: false });

        fixture.detectChanges();
        expect(fasting.disabled).toBe(true);
        expect(social.disabled).toBe(true);
        expect(accountSwitch.disabled).toBe(true);
        expect(fasting.getAttribute('aria-checked')).toBe('true');

        save.next({ ...initialPreferences, fastingPushNotificationsEnabled: false });
        await fixture.whenStable();
        fixture.detectChanges();
        expect(fasting.getAttribute('aria-checked')).toBe('false');
        expect(social.disabled).toBe(false);
        expect((fixture.nativeElement as HTMLElement).querySelector('.user-manage__notifications-reminders')).toBeNull();
        expect(toast.info).toHaveBeenCalledExactlyOnceWith('USER_MANAGE.NOTIFICATIONS_FASTING_DISABLED_TOAST');
    });

    it.each([
        { category: 'FASTING', key: 'fastingPushNotificationsEnabled' },
        { category: 'SOCIAL', key: 'socialPushNotificationsEnabled' },
    ] as const)('keeps the saved $category switch on failure, then clears the visible error after retry', async ({ category, key }) => {
        const { fixture, account, toast } = setup();
        const failed = new Subject<NotificationPreferences>();
        const disabled = { [key]: false };
        account.updateNotificationPreferences.mockReturnValueOnce(failed).mockReturnValueOnce(of({ ...initialPreferences, ...disabled }));
        const toggle = categorySwitch(fixture, category);

        toggle.click();
        failed.error(new Error('Connection lost'));
        await fixture.whenStable();
        fixture.detectChanges();
        expect(toggle.getAttribute('aria-checked')).toBe('true');
        expect(element(fixture, '.user-manage__form > .user-manage__error [role="alert"]').textContent).toContain(
            'USER_MANAGE.UPDATE_ERROR',
        );
        expect(toast.info).not.toHaveBeenCalled();

        toggle.click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(toggle.getAttribute('aria-checked')).toBe('false');
        expect((fixture.nativeElement as HTMLElement).querySelector('.user-manage__form > .user-manage__error')).toBeNull();
        expect(account.updateNotificationPreferences.mock.calls).toEqual([[disabled], [disabled]]);
        expect(toast.info).toHaveBeenCalledExactlyOnceWith(`USER_MANAGE.NOTIFICATIONS_${category}_DISABLED_TOAST`);
    });
});

describe('Profile category saves across route navigation', () => {
    it.each([
        { category: 'FASTING', key: 'fastingPushNotificationsEnabled' },
        { category: 'SOCIAL', key: 'socialPushNotificationsEnabled' },
    ] as const)('finishes the $category change without notifying the diary', async ({ category, key }) => {
        const { harness, profile, account, toast } = await setupRoutedProfileAsync();
        const pending = new Subject<NotificationPreferences>();
        const update = { [key]: false };
        account.updateNotificationPreferences.mockReturnValueOnce(pending);
        const toggle = profile.querySelector<HTMLButtonElement>(
            `button[role="switch"][aria-label="USER_MANAGE.${category}_NOTIFICATIONS"]`,
        );
        if (toggle === null) {
            throw new Error(`Expected the ${category} notification switch`);
        }
        expect(toggle.getAttribute('aria-checked')).toBe('true');

        toggle.click();
        expect(account.updateNotificationPreferences).toHaveBeenCalledExactlyOnceWith(update);
        await harness.navigateByUrl('/diary', NotificationDepartureComponent);
        expect(profile.isConnected).toBe(false);
        expect(pending.observed).toBe(true);

        pending.next({ ...initialPreferences, ...update });
        pending.complete();
        await waitForAsyncTasksAsync();

        expect(harness.routeNativeElement?.textContent).toContain('Diary opened');
        expect(account.updateNotificationPreferences).toHaveBeenCalledExactlyOnceWith(update);
        expect(toast.info).not.toHaveBeenCalled();
    });
});

describe('Profile notification category reactivation', () => {
    it.each([
        { category: 'FASTING', otherCategory: 'SOCIAL', field: 'fastingPushNotificationsEnabled' },
        { category: 'SOCIAL', otherCategory: 'FASTING', field: 'socialPushNotificationsEnabled' },
    ] as const)('enables only $category after the account confirms the change', async ({ category, otherCategory, field }) => {
        const disabled = { ...initialPreferences, fastingPushNotificationsEnabled: false, socialPushNotificationsEnabled: false };
        const { fixture, account, toast } = setup(disabled);
        account.updateNotificationPreferences.mockReturnValueOnce(of({ ...disabled, [field]: true }));
        const selected = categorySwitch(fixture, category);
        expect(selected.getAttribute('aria-checked')).toBe('false');

        selected.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(account.updateNotificationPreferences).toHaveBeenCalledExactlyOnceWith({ [field]: true });
        expect(selected.getAttribute('aria-checked')).toBe('true');
        expect(categorySwitch(fixture, otherCategory).getAttribute('aria-checked')).toBe('false');
        expect(toast.info).toHaveBeenCalledExactlyOnceWith(`USER_MANAGE.NOTIFICATIONS_${category}_ENABLED_TOAST`);
    });
});

describe('Profile connected notification devices', () => {
    it('locks device actions during server removal and refreshes the remaining current device', async () => {
        const { fixture, account, browser, toast } = setup();
        const pendingRemoval = new Subject<void>();
        const refresh = new Subject<WebPushSubscriptionItem[]>();
        account.removeWebPushSubscription.mockReturnValueOnce(pendingRemoval);
        account.getWebPushSubscriptions.mockReturnValueOnce(refresh);
        const localButton = removeButton(fixture, 'Chrome / Windows');
        const remoteButton = removeButton(fixture, 'Firefox / Linux');
        expect(localButton.closest('.user-manage__notifications-device-item')?.textContent).toContain(
            'USER_MANAGE.NOTIFICATIONS_DEVICE_CURRENT',
        );
        expect(remoteButton.closest('.user-manage__notifications-device-item')?.textContent).not.toContain(
            'USER_MANAGE.NOTIFICATIONS_DEVICE_CURRENT',
        );

        remoteButton.click();
        expect(localButton.disabled).toBe(false);
        localButton.click();
        expect(browser.push.removeSubscriptionAsync).not.toHaveBeenCalled();

        fixture.detectChanges();
        expect(account.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(remoteDevice.endpoint);
        expect(remoteButton.disabled).toBe(true);
        expect(localButton.disabled).toBe(true);

        pendingRemoval.next();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(account.getWebPushSubscriptions).toHaveBeenCalledOnce();
        });
        expect(element(fixture, 'fd-user-manage-connected-devices').textContent).toContain(
            'USER_MANAGE.NOTIFICATIONS_CONNECTED_DEVICES_LOADING',
        );
        refresh.next([localDevice]);
        refresh.complete();
        fixture.detectChanges();
        expect(removeButton(fixture, 'Chrome / Windows').disabled).toBe(false);
        expect(element(fixture, 'fd-user-manage-connected-devices').textContent).not.toContain('Firefox / Linux');
        expect(toast.info).toHaveBeenCalledExactlyOnceWith('USER_MANAGE.NOTIFICATIONS_DEVICE_REMOVED_TOAST');
    });

    it('removes the current device through the browser adapter and leaves a remote device present', async () => {
        const { fixture, account, browser, recordSubscription } = setup();
        account.getWebPushSubscriptions.mockReturnValueOnce(of([remoteDevice]));
        removeButton(fixture, 'Chrome / Windows').click();
        browser.push.currentSubscriptionEndpoint.set(null);
        browser.push.isSubscribed.set(false);
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(element(fixture, 'fd-user-manage-connected-devices').textContent).not.toContain('Chrome / Windows');
        });
        expect(browser.push.removeSubscriptionAsync).toHaveBeenCalledExactlyOnceWith(localDevice.endpoint);
        expect(account.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(recordSubscription).toHaveBeenCalledExactlyOnceWith('subscription.remove', 'success', { currentDevice: true });
        expect(removeButton(fixture, 'Firefox / Linux').disabled).toBe(false);
        expect(element(fixture, '.user-manage__notifications-enable-device button').textContent).toContain(
            'USER_MANAGE.NOTIFICATIONS_ENABLE_DEVICE',
        );
    });

    it('keeps both devices when removal fails and lets the user retry', async () => {
        const { fixture, account, browser, toast } = setup();
        const pendingRemoval = new Subject<void>();
        account.removeWebPushSubscription.mockReturnValueOnce(pendingRemoval);
        const remoteButton = removeButton(fixture, 'Firefox / Linux');

        remoteButton.click();
        pendingRemoval.error(new Error('Connection lost'));
        await fixture.whenStable();
        fixture.detectChanges();

        expect(toast.error).toHaveBeenCalledExactlyOnceWith('USER_MANAGE.NOTIFICATIONS_DEVICE_REMOVE_ERROR');
        expect(removeButton(fixture, 'Chrome / Windows').disabled).toBe(false);
        expect(remoteButton.disabled).toBe(false);
        expect(account.getWebPushSubscriptions).not.toHaveBeenCalled();
        expect(browser.push.removeSubscriptionAsync).not.toHaveBeenCalled();
        account.getWebPushSubscriptions.mockReturnValueOnce(of([localDevice]));
        remoteButton.click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(element(fixture, 'fd-user-manage-connected-devices').textContent).not.toContain('Firefox / Linux');
        });
    });
});

describe('Profile connected notification device discovery', () => {
    it('checks the pending browser subscription before removing the device from the account', async () => {
        const discovery = new ReplaySubject<PushSubscription | null>(1);
        const unsubscribe = vi.fn<PushSubscription['unsubscribe']>().mockResolvedValue(true);
        const { worker, providers } = createDiscoveryWorker(discovery);
        const storageKey = 'webPushSubscriptionOwner';
        localStorage.setItem(storageKey, JSON.stringify({ endpoint: localDevice.endpoint, userId: 'profile-user' }));
        onTestFinished(() => {
            discovery.complete();
            localStorage.removeItem(storageKey);
            localStorage.removeItem('webPushDeviceOptIn:profile-user');
        });

        const { fixture, account, toast } = setup(initialPreferences, knownDevices, providers);
        account.getWebPushSubscriptions.mockReturnValueOnce(of([remoteDevice]));
        expect(discovery.observed).toBe(true);
        const remove = removeButton(fixture, 'Chrome / Windows');
        expect(remove.closest('.user-manage__notifications-device-item')?.textContent).not.toContain(
            'USER_MANAGE.NOTIFICATIONS_DEVICE_CURRENT',
        );

        remove.click();
        expect(account.removeWebPushSubscription).not.toHaveBeenCalled();

        discovery.next(
            createExistingBrowserSubscription(async () => {
                const removed = await unsubscribe();
                discovery.next(null);
                return removed;
            }),
        );
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(unsubscribe).toHaveBeenCalledOnce();
            expect(account.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(localDevice.endpoint);
            expect(element(fixture, 'fd-user-manage-connected-devices').textContent).not.toContain('Chrome / Windows');
        });

        expect(unsubscribe.mock.invocationCallOrder[0]).toBeLessThan(account.removeWebPushSubscription.mock.invocationCallOrder[0]);
        expect(removeButton(fixture, 'Firefox / Linux').disabled).toBe(false);
        expect(worker.requestSubscription).not.toHaveBeenCalled();
        expect(account.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(toast.info).toHaveBeenCalledExactlyOnceWith('USER_MANAGE.NOTIFICATIONS_DEVICE_REMOVED_TOAST');
    });
});

type BrowserDiscoveryWorker = Pick<SwPush, 'isEnabled' | 'subscription' | 'pushSubscriptionChanges' | 'notificationClicks'> & {
    requestSubscription: Mock<SwPush['requestSubscription']>;
};

function createDiscoveryWorker(
    discovery: Observable<PushSubscription | null>,
    enabled = true,
): {
    worker: BrowserDiscoveryWorker;
    providers: Provider[];
} {
    const worker: BrowserDiscoveryWorker = {
        isEnabled: enabled,
        subscription: discovery,
        pushSubscriptionChanges: new Subject(),
        notificationClicks: new Subject(),
        requestSubscription: vi.fn<SwPush['requestSubscription']>(),
    };
    const auth: Pick<AuthService, 'isAdmin' | 'isAuthenticated' | 'isImpersonating' | 'getUserId' | 'getSessionKey'> = {
        isAdmin: signal(false),
        isAuthenticated: signal(true),
        isImpersonating: signal(false),
        getUserId: () => 'profile-user',
        getSessionKey: () => 'profile-session',
    };
    return {
        worker,
        providers: [PushNotificationService, { provide: SwPush, useValue: worker }, { provide: AuthService, useValue: auth }],
    };
}

function createExistingBrowserSubscription(unsubscribe: PushSubscription['unsubscribe']): PushSubscription {
    const keys = {
        p256dh: 'BGsX0fLhLEJH-Lzm5WOkQPJ3A32BLeszoPShOUXYmMJWT-NC4v4af5uO5-tKfA-eFivOM1drMV7Oy7ZAaDe_UfU',
        auth: 'AAECAwQFBgcICQoLDA0ODw',
    };
    return {
        endpoint: localDevice.endpoint,
        expirationTime: null,
        options: { userVisibleOnly: true, applicationServerKey: null },
        unsubscribe,
        getKey: name =>
            Uint8Array.from(atob(keys[name].replaceAll('-', '+').replaceAll('_', '/')), value => value.codePointAt(0) ?? 0).buffer,
        toJSON: () => ({ endpoint: localDevice.endpoint, expirationTime: null, keys }),
    };
}

describe('Profile device removal with browser discovery unavailable', () => {
    it('keeps both devices after a failed browser read and removes just the remote device on retry', async () => {
        const firstRead = new ReplaySubject<PushSubscription | null>(1);
        let read: Observable<PushSubscription | null> = firstRead;
        const { worker, providers } = createDiscoveryWorker(defer(() => read));
        const unsubscribe = vi.fn<PushSubscription['unsubscribe']>().mockResolvedValue(true);
        const { fixture, account, toast } = setup(initialPreferences, knownDevices, providers);
        const remote = removeButton(fixture, 'Firefox / Linux');

        remote.click();
        firstRead.error(new Error('Worker read was interrupted'));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(toast.error).toHaveBeenCalledExactlyOnceWith('USER_MANAGE.NOTIFICATIONS_DEVICE_REMOVE_ERROR');
            expect(removeButton(fixture, 'Firefox / Linux').disabled).toBe(false);
        });
        expect(account.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(removeButton(fixture, 'Chrome / Windows').disabled).toBe(false);

        read = of(createExistingBrowserSubscription(unsubscribe));
        account.getWebPushSubscriptions.mockReturnValueOnce(of([localDevice]));
        removeButton(fixture, 'Firefox / Linux').click();
        await waitForDeviceRemovalAsync(fixture, 'Firefox / Linux');

        expect(account.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(remoteDevice.endpoint);
        expect(unsubscribe).not.toHaveBeenCalled();
        expect(worker.requestSubscription).not.toHaveBeenCalled();
        expect(removeButton(fixture, 'Chrome / Windows').disabled).toBe(false);
    });

    it('removes other devices without consulting a permanently disabled worker', async () => {
        const browserRead = vi.fn<() => Observable<PushSubscription | null>>().mockReturnValue(of(null));
        const { worker, providers } = createDiscoveryWorker(defer(browserRead), false);
        const { fixture, account } = setup(initialPreferences, [remoteDevice], providers);
        account.getWebPushSubscriptions.mockReturnValueOnce(of([]));

        removeButton(fixture, 'Firefox / Linux').click();
        await waitForDeviceRemovalAsync(fixture, 'Firefox / Linux');

        expect(account.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(remoteDevice.endpoint);
        expect(browserRead).not.toHaveBeenCalled();
        expect(worker.requestSubscription).not.toHaveBeenCalled();
    });
});

describe('Profile disconnect with device registration in flight', () => {
    it('removes the current account registration after the earlier request settles', async () => {
        const discovery = new ReplaySubject<PushSubscription | null>(1);
        const unsubscribe = vi.fn<PushSubscription['unsubscribe']>().mockResolvedValue(true);
        const { worker, providers } = createDiscoveryWorker(discovery);
        const registration = new Subject<void>();
        const ownerStorageKey = 'webPushSubscriptionOwner';
        const optInKey = 'webPushDeviceOptIn:profile-user';
        localStorage.setItem(ownerStorageKey, JSON.stringify({ endpoint: localDevice.endpoint, userId: 'profile-user' }));
        onTestFinished(() => {
            discovery.complete();
            localStorage.removeItem(ownerStorageKey);
            localStorage.removeItem(optInKey);
        });
        const { fixture, account } = setup(initialPreferences, knownDevices, providers);
        account.upsertWebPushSubscription.mockReturnValueOnce(registration);
        account.getWebPushSubscriptions.mockReturnValueOnce(of([remoteDevice]));
        discovery.next(
            createExistingBrowserSubscription(async () => {
                const removed = await unsubscribe();
                discovery.next(null);
                return removed;
            }),
        );
        await vi.waitFor(() => {
            expect(registration.observed).toBe(true);
        });

        removeButton(fixture, 'Chrome / Windows').click();
        await vi.waitFor(() => {
            expect(unsubscribe).toHaveBeenCalledOnce();
        });
        expect(account.removeWebPushSubscription).not.toHaveBeenCalled();

        registration.next();
        registration.complete();
        await waitForDeviceRemovalAsync(fixture, 'Chrome / Windows');

        expect(account.upsertWebPushSubscription).toHaveBeenCalledOnce();
        expect(account.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(localDevice.endpoint);
        expect(worker.requestSubscription).not.toHaveBeenCalled();
        expect(localStorage.getItem(ownerStorageKey)).toBeNull();
        expect(localStorage.getItem(optInKey)).toBe('false');
        expect(removeButton(fixture, 'Firefox / Linux').disabled).toBe(false);
    });
});

async function waitForDeviceRemovalAsync(fixture: ComponentFixture<UserManageComponent>, device: string): Promise<void> {
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(element(fixture, 'fd-user-manage-connected-devices').textContent).not.toContain(device);
    });
}

describe('Profile device ownership across real route navigation', () => {
    it('does not refresh the abandoned profile or notify the diary when its current device removal succeeds', async () => {
        const { harness, profile, account, browser, toast, recordSubscription } = await setupRoutedProfileAsync();
        const pendingRemoval = new Subject<boolean>();
        browser.push.removeSubscriptionAsync.mockReturnValueOnce(firstValueFrom(pendingRemoval));

        deviceRemoveButton(profile, 'Chrome / Windows').click();
        expect(browser.push.removeSubscriptionAsync).toHaveBeenCalledExactlyOnceWith(localDevice.endpoint);
        await harness.navigateByUrl('/diary');
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Diary opened');
        expect(profile.isConnected).toBe(false);

        browser.push.currentSubscriptionEndpoint.set(null);
        pendingRemoval.next(true);
        pendingRemoval.complete();
        await waitForAsyncTasksAsync();

        expect(account.getWebPushSubscriptions).not.toHaveBeenCalled();
        expect(toast.info).not.toHaveBeenCalled();
        expect(recordSubscription).not.toHaveBeenCalled();
    });

    it('does not show a failed remote device removal on the diary', async () => {
        const { harness, profile, account, browser, toast, recordSubscription } = await setupRoutedProfileAsync();
        const pendingRemoval = new Subject<void>();
        account.removeWebPushSubscription.mockReturnValueOnce(pendingRemoval);
        deviceRemoveButton(profile, 'Firefox / Linux').click();
        expect(account.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(remoteDevice.endpoint);
        expect(browser.push.removeSubscriptionAsync).not.toHaveBeenCalled();

        await harness.navigateByUrl('/diary');
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Diary opened');
        pendingRemoval.error(new Error('Fixture remote device removal failed'));
        await waitForAsyncTasksAsync();

        expect(account.getWebPushSubscriptions).not.toHaveBeenCalled();
        expect(toast.error).not.toHaveBeenCalled();
        expect(recordSubscription).not.toHaveBeenCalled();
    });
});

describe('Profile connected-device refresh recovery', () => {
    it('keeps the confirmed removal and the remaining device when the refresh fails', async () => {
        const { fixture, account, browser, toast } = setup();
        const refresh = new Subject<WebPushSubscriptionItem[]>();
        account.getWebPushSubscriptions.mockReturnValueOnce(refresh).mockReturnValueOnce(of([]));

        removeButton(fixture, 'Firefox / Linux').click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(element(fixture, 'fd-user-manage-connected-devices').textContent).toContain(
                'USER_MANAGE.NOTIFICATIONS_CONNECTED_DEVICES_LOADING',
            );
        });
        refresh.error(new Error('Fixture device list temporarily unavailable'));
        fixture.detectChanges();

        expect(element(fixture, 'fd-user-manage-connected-devices').textContent).not.toContain('Firefox / Linux');
        const remaining = removeButton(fixture, 'Chrome / Windows');
        expect(remaining.disabled).toBe(false);
        remaining.click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(element(fixture, 'fd-user-manage-connected-devices').textContent).toContain(
                'USER_MANAGE.NOTIFICATIONS_CONNECTED_DEVICES_EMPTY',
            );
        });
        expect(browser.push.removeSubscriptionAsync).toHaveBeenCalledExactlyOnceWith(localDevice.endpoint);
        expect(account.getWebPushSubscriptions).toHaveBeenCalledTimes(2);
        expect(toast.info).toHaveBeenCalledTimes(2);
    });
});

describe('Profile device presentation after changing language', () => {
    it('updates the fallback device name and last activity date without refetching the device', async () => {
        const device = { ...remoteDevice, userAgent: null, updatedAtUtc: '2026-05-18T11:30:00Z' };
        const { fixture, account } = setup(initialPreferences, [device]);
        const localization = TestBed.inject(LocalizationService);
        const language = vi.spyOn(localization, 'getCurrentLanguage');
        const translations = TestBed.inject(TranslateService);
        const card = element(fixture, 'fd-user-manage-connected-devices');
        translations.setTranslation('en', { USER_MANAGE: { NOTIFICATIONS_DEVICE_GENERIC: 'Browser device' } });
        translations.setTranslation('ru', { USER_MANAGE: { NOTIFICATIONS_DEVICE_GENERIC: 'Браузер' } });
        fixture.detectChanges();
        expect(card.textContent).toContain('Browser device');
        expect(card.textContent).toContain('May 18, 2026');

        language.mockReturnValue('ru');
        await firstValueFrom(translations.use('ru'));
        fixture.detectChanges();

        expect(card.textContent).toContain('Браузер');
        expect(card.textContent).toContain('18 мая 2026');
        expect(card.textContent).toContain('push.example');
        expect(card.textContent).not.toContain('Browser device');
        expect(account.getWebPushSubscriptions).not.toHaveBeenCalled();
    });
});

describe('Profile notification browser permission', () => {
    it('renders the blocked state when the user returns from changing system permission', () => {
        const { fixture, browser } = setup();
        expect(element(fixture, 'fd-user-manage-notifications-card').textContent).toContain('USER_MANAGE.NOTIFICATIONS_STATUS_ENABLED');

        browser.capability.getPermission.mockReturnValue('denied');
        window.dispatchEvent(new Event('focus'));
        fixture.detectChanges();

        expect(element(fixture, 'fd-user-manage-notifications-card').textContent).toContain('USER_MANAGE.NOTIFICATIONS_BLOCKED_HINT');
        expect(browser.push.ensureSubscriptionAsync).not.toHaveBeenCalled();
    });
});

describe('Profile fasting reminder drafts', () => {
    it('keeps visible reminder edits and validation when an independent category is saved', async () => {
        const { fixture, account, toast } = setup();
        const first = reminderInput(fixture, 0);
        const followUp = reminderInput(fixture, 1);
        const save = notificationButton(fixture, '.user-manage__notifications-reminder-save button');
        const firstDraft = 15;
        const correctedFollowUp = 22;
        inputReminder(fixture, first, String(firstDraft));
        inputReminder(fixture, followUp, '0');
        expect(first.value).toBe(String(firstDraft));
        expect(followUp.value).toBe('0');

        const socialSaved = { ...initialPreferences, socialPushNotificationsEnabled: false };
        account.updateNotificationPreferences.mockReturnValueOnce(of(socialSaved));
        const social = categorySwitch(fixture, 'SOCIAL');
        social.click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(social.getAttribute('aria-checked')).toBe('false');
        expect(first.value).toBe(String(firstDraft));
        expect(followUp.value).toBe('0');

        save.click();
        expect(toast.error).toHaveBeenCalledExactlyOnceWith('USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_INPUT_ERROR');
        expect(account.updateNotificationPreferences).toHaveBeenCalledExactlyOnceWith({ socialPushNotificationsEnabled: false });

        inputReminder(fixture, followUp, String(correctedFollowUp));
        const saved = {
            ...socialSaved,
            fastingCheckInReminderHours: firstDraft,
            fastingCheckInFollowUpReminderHours: correctedFollowUp,
        };
        account.updateNotificationPreferences.mockReturnValueOnce(of(saved));
        save.click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(account.updateNotificationPreferences).toHaveBeenLastCalledWith({
            fastingCheckInReminderHours: firstDraft,
            fastingCheckInFollowUpReminderHours: correctedFollowUp,
        });
        expect(toast.info).toHaveBeenLastCalledWith('USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_SAVED');
        expect(followUp.value).toBe(String(correctedFollowUp));
    });
});

describe('Profile fasting reminder empty fields', () => {
    it('keeps each cleared input empty and saves only after both reminder times are valid', async () => {
        const { fixture, account, toast } = setup();
        const first = reminderInput(fixture, 0);
        const followUp = reminderInput(fixture, 1);
        const save = notificationButton(fixture, '.user-manage__notifications-reminder-save button');
        const request = { fastingCheckInReminderHours: 15, fastingCheckInFollowUpReminderHours: 22 };
        const inputError = 'USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_INPUT_ERROR';

        inputReminder(fixture, first, '');
        save.click();
        expect(first.value).toBe('');
        expect(followUp.value).toBe(String(initialPreferences.fastingCheckInFollowUpReminderHours));
        expect(account.updateNotificationPreferences).not.toHaveBeenCalled();
        expect(toast.error).toHaveBeenCalledExactlyOnceWith(inputError);

        inputReminder(fixture, first, String(request.fastingCheckInReminderHours));
        inputReminder(fixture, followUp, '');
        save.click();
        expect(first.value).toBe(String(request.fastingCheckInReminderHours));
        expect(followUp.value).toBe('');
        expect(account.updateNotificationPreferences).not.toHaveBeenCalled();
        expect(toast.error.mock.calls).toEqual([[inputError], [inputError]]);

        inputReminder(fixture, followUp, String(request.fastingCheckInFollowUpReminderHours));
        account.updateNotificationPreferences.mockReturnValueOnce(of({ ...initialPreferences, ...request }));
        save.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(account.updateNotificationPreferences).toHaveBeenCalledExactlyOnceWith(request);
        expect(toast.info).toHaveBeenCalledExactlyOnceWith('USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_SAVED');
        expect(first.value).toBe(String(request.fastingCheckInReminderHours));
        expect(followUp.value).toBe(String(request.fastingCheckInFollowUpReminderHours));
    });
});

describe('Profile fasting reminder preset persistence', () => {
    it('keeps the selected reminder schedule after a failed save and sends it again on retry', async () => {
        const { fixture, account, toast } = setup();
        const recordTiming = vi.spyOn(TestBed.inject(FrontendObservabilityService), 'recordFastingReminderTimingSaved');
        const firstReminder = 16;
        const followUpReminder = 24;
        const pending = new Subject<NotificationPreferences>();
        const request = {
            fastingCheckInReminderHours: firstReminder,
            fastingCheckInFollowUpReminderHours: followUpReminder,
        };
        account.updateNotificationPreferences.mockReturnValueOnce(pending).mockReturnValueOnce(of({ ...initialPreferences, ...request }));
        const presets = element(fixture, '.user-manage__notifications-reminder-presets-list');
        const label = 'USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_PRESET_STEADY';
        const preset = Array.from(presets.querySelectorAll('button')).find(button => button.textContent.includes(label));
        expect(preset).toBeDefined();
        preset?.click();
        fixture.detectChanges();
        expect(presets.querySelector('.user-manage__preset-button--active')?.textContent).toContain(label);
        expect(reminderInput(fixture, 0).value).toBe(String(firstReminder));
        expect(reminderInput(fixture, 1).value).toBe(String(followUpReminder));
        expect(account.updateNotificationPreferences).not.toHaveBeenCalled();

        const save = notificationButton(fixture, '.user-manage__notifications-reminder-save button');
        save.click();
        save.click();
        expect(account.updateNotificationPreferences).toHaveBeenCalledExactlyOnceWith(request);
        pending.error(new Error('Reminder settings temporarily unavailable'));
        await fixture.whenStable();
        fixture.detectChanges();
        expect(element(fixture, '.user-manage__form > .user-manage__error [role="alert"]').textContent).toContain(
            'USER_MANAGE.UPDATE_ERROR',
        );
        expect(presets.querySelector('.user-manage__preset-button--active')?.textContent).toContain(label);
        expect(toast.info).not.toHaveBeenCalled();

        save.click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(account.updateNotificationPreferences.mock.calls).toEqual([[request], [request]]);
        expect(toast.info).toHaveBeenCalledExactlyOnceWith('USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_SAVED');
        expect(recordTiming).toHaveBeenCalledExactlyOnceWith({
            firstReminderHours: firstReminder,
            followUpReminderHours: followUpReminder,
            source: 'preset',
            presetId: 'steady',
        });
    });
});

describe('Profile current-device enrollment', () => {
    it('keeps account notifications on when the prompt is cancelled and refreshes devices on successful retry', async () => {
        const { fixture, account, browser, toast } = setup();
        browser.push.isSubscribed.set(false);
        browser.push.currentSubscriptionEndpoint.set(null);
        browser.capability.getPermission.mockReturnValue('default');
        browser.capability.requestPermissionAsync.mockResolvedValueOnce('default').mockResolvedValueOnce('granted');
        window.dispatchEvent(new Event('focus'));
        fixture.detectChanges();

        const enableSelector = '.user-manage__notifications-enable-device button';
        notificationButton(fixture, enableSelector).click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(browser.push.ensureSubscriptionAsync).not.toHaveBeenCalled();
        expect(account.updateNotificationPreferences).not.toHaveBeenCalled();
        expect(toast.info).toHaveBeenCalledExactlyOnceWith('USER_MANAGE.NOTIFICATIONS_SETUP_REQUIRED_HINT');
        expect(notificationButton(fixture, enableSelector).disabled).toBe(false);

        browser.capability.getPermission.mockReturnValue('granted');
        const enrolling = new Subject<'subscribed'>();
        browser.push.ensureSubscriptionAsync.mockReturnValueOnce(firstValueFrom(enrolling));
        notificationButton(fixture, enableSelector).click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(browser.push.ensureSubscriptionAsync).toHaveBeenCalledOnce();
        });
        expect(notificationButton(fixture, enableSelector).disabled).toBe(true);
        browser.push.currentSubscriptionEndpoint.set(localDevice.endpoint);
        browser.push.isSubscribed.set(true);
        enrolling.next('subscribed');
        enrolling.complete();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(browser.capability.requestPermissionAsync).toHaveBeenCalledTimes(2);
        expect(browser.push.ensureSubscriptionAsync).toHaveBeenCalledOnce();
        expect(account.getWebPushSubscriptions).toHaveBeenCalledOnce();
        expect(account.updateNotificationPreferences).not.toHaveBeenCalled();
        expect(toast.success).toHaveBeenCalledExactlyOnceWith('DASHBOARD.ACTIONS.PUSH_ENABLED');
        expect((fixture.nativeElement as HTMLElement).querySelector(enableSelector)).toBeNull();
    });

    it('shows the blocked hint if system permission is revoked while current-device enrollment is pending', async () => {
        const { fixture, account, browser, toast } = setup();
        browser.push.isSubscribed.set(false);
        browser.push.currentSubscriptionEndpoint.set(null);
        browser.capability.requestPermissionAsync.mockResolvedValueOnce('granted');
        const enrolling = new Subject<'unavailable'>();
        browser.push.ensureSubscriptionAsync.mockReturnValueOnce(firstValueFrom(enrolling));
        fixture.detectChanges();

        const enableSelector = '.user-manage__notifications-enable-device button';
        notificationButton(fixture, enableSelector).click();
        await vi.waitFor(() => {
            expect(browser.push.ensureSubscriptionAsync).toHaveBeenCalledOnce();
        });
        browser.capability.getPermission.mockReturnValue('denied');
        enrolling.next('unavailable');
        enrolling.complete();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(element(fixture, 'fd-user-manage-notifications-card').textContent).toContain('USER_MANAGE.NOTIFICATIONS_BLOCKED_HINT');
        expect(toast.info).toHaveBeenCalledExactlyOnceWith('USER_MANAGE.NOTIFICATIONS_BLOCKED_HINT');
        expect(account.updateNotificationPreferences).not.toHaveBeenCalled();
        expect(account.getWebPushSubscriptions).not.toHaveBeenCalled();
    });
});

describe('Profile test notification scheduling', () => {
    it('locks repeated activation until the server responds, then exposes retry after a rejected schedule', () => {
        const { fixture, account, toast } = setup();
        const pending = new Subject<ScheduledNotificationResponse>();
        const schedule = { type: 'FastingCompleted', delaySeconds: TEST_NOTIFICATION_DELAY_SECONDS };
        account.scheduleTestNotification
            .mockReturnValueOnce(pending)
            .mockReturnValueOnce(of({ ...schedule, scheduledAtUtc: '2026-11-20T07:00:00Z' }));
        const send = notificationButton(fixture, '.user-manage__notifications-action-button button');
        send.click();
        send.click();
        fixture.detectChanges();
        expect(account.scheduleTestNotification).toHaveBeenCalledExactlyOnceWith(schedule);
        expect(send.disabled).toBe(true);

        pending.error(new Error('Schedule unavailable'));
        fixture.detectChanges();
        expect(toast.error).toHaveBeenCalledExactlyOnceWith('DASHBOARD.ACTIONS.TEST_PUSH_ERROR');
        expect(send.disabled).toBe(false);
        send.click();
        fixture.detectChanges();
        expect(account.scheduleTestNotification).toHaveBeenCalledTimes(2);
        expect(toast.info).toHaveBeenCalledExactlyOnceWith('DASHBOARD.ACTIONS.TEST_PUSH_SCHEDULED');
        expect(send.disabled).toBe(false);
    });
});

describe('Notification requests after leaving profile settings', () => {
    it.each([true, false])('lets the account save finish without reopening notification setup (enabled=%s)', async enabled => {
        const { fixture, account, browser, toast } = setup({ ...initialPreferences, pushNotificationsEnabled: !enabled });
        const pending = new Subject<NotificationPreferences>();
        account.updateNotificationPreferences.mockReturnValueOnce(pending);
        browser.push.ensureSubscriptionAsync.mockResolvedValue('blocked');
        const master = notificationButton(fixture, 'button[role="switch"][aria-label="USER_MANAGE.PUSH_NOTIFICATIONS"]');

        master.click();
        expect(account.updateNotificationPreferences).toHaveBeenCalledExactlyOnceWith({ pushNotificationsEnabled: enabled });
        fixture.destroy();
        pending.next({ ...initialPreferences, pushNotificationsEnabled: enabled });
        pending.complete();
        await waitForAsyncTasksAsync();

        expect(browser.push.ensureSubscriptionAsync).not.toHaveBeenCalled();
        expect(toast.info).not.toHaveBeenCalled();
        expect(toast.success).not.toHaveBeenCalled();
    });

    it('does not start browser enrollment when permission is granted after the settings close', async () => {
        const { fixture, browser, toast } = setup();
        const pending = new Subject<NotificationPermission>();
        browser.push.isSubscribed.set(false);
        browser.push.currentSubscriptionEndpoint.set(null);
        browser.capability.getPermission.mockReturnValue('default');
        browser.capability.requestPermissionAsync.mockReturnValueOnce(firstValueFrom(pending));
        browser.push.ensureSubscriptionAsync.mockResolvedValue('blocked');
        window.dispatchEvent(new Event('focus'));
        fixture.detectChanges();

        notificationButton(fixture, '.user-manage__notifications-enable-device button').click();
        expect(browser.capability.requestPermissionAsync).toHaveBeenCalledOnce();
        fixture.destroy();
        pending.next('granted');
        pending.complete();
        await waitForAsyncTasksAsync();

        expect(browser.push.ensureSubscriptionAsync).not.toHaveBeenCalled();
        expect(toast.info).not.toHaveBeenCalled();
    });

    it('leaves the page quiet when a pending browser enrollment fails after settings close', async () => {
        const { fixture, browser, toast } = setup({ ...initialPreferences, pushNotificationsEnabled: false });
        const pending = new Subject<'blocked'>();
        browser.push.ensureSubscriptionAsync.mockReturnValueOnce(firstValueFrom(pending));

        notificationButton(fixture, 'button[role="switch"][aria-label="USER_MANAGE.PUSH_NOTIFICATIONS"]').click();
        await vi.waitFor(() => {
            expect(browser.push.ensureSubscriptionAsync).toHaveBeenCalledOnce();
        });
        fixture.destroy();
        pending.next('blocked');
        pending.complete();
        await waitForAsyncTasksAsync();

        expect(toast.info).not.toHaveBeenCalled();
        expect(toast.success).not.toHaveBeenCalled();
    });
});

describe('Current-device enrollment after navigating from profile', () => {
    it.each(['success', 'failure'] as const)('does not notify the diary when the enrollment ends in %s', async outcome => {
        const { harness, profile, account, browser, toast, recordSubscription } = await setupRoutedProfileAsync();
        const pending = new Subject<'subscribed'>();
        browser.push.isSubscribed.set(false);
        browser.push.currentSubscriptionEndpoint.set(null);
        browser.capability.requestPermissionAsync.mockResolvedValueOnce('granted');
        browser.push.ensureSubscriptionAsync.mockReturnValueOnce(firstValueFrom(pending));
        harness.detectChanges();
        const enable = profile.querySelector<HTMLButtonElement>('.user-manage__notifications-enable-device button');
        expect(enable).not.toBeNull();
        enable?.click();
        await vi.waitFor(() => {
            expect(browser.push.ensureSubscriptionAsync).toHaveBeenCalledOnce();
        });
        const reads = account.getWebPushSubscriptions.mock.calls.length;
        await harness.navigateByUrl('/diary', NotificationDepartureComponent);
        if (outcome === 'success') {
            pending.next('subscribed');
            pending.complete();
        } else {
            pending.error(new Error('Device enrollment did not finish'));
        }
        await waitForAsyncTasksAsync();
        harness.detectChanges();

        expect(harness.routeNativeElement?.textContent).toContain('Diary opened');
        expect(account.getWebPushSubscriptions).toHaveBeenCalledTimes(reads);
        expect(recordSubscription).not.toHaveBeenCalled();
        expect(toast.info).not.toHaveBeenCalled();
        expect(toast.success).not.toHaveBeenCalled();
    });
});

function notificationButton(fixture: ComponentFixture<UserManageComponent>, selector: string): HTMLButtonElement {
    const button = element(fixture, selector);
    if (!(button instanceof HTMLButtonElement)) {
        throw new TypeError(`Expected the notification button ${selector}`);
    }
    return button;
}

function reminderInput(fixture: ComponentFixture<UserManageComponent>, index: number): HTMLInputElement {
    const input = Array.from(element(fixture, '.user-manage__notifications-reminders').querySelectorAll('input')).at(index);
    if (input === undefined) {
        throw new Error(`Expected reminder input ${index}`);
    }
    return input;
}

function inputReminder(fixture: ComponentFixture<UserManageComponent>, input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
}
