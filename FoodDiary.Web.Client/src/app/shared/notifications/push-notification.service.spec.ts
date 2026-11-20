import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { DefaultUrlSerializer, NavigationEnd, Router, type UrlTree } from '@angular/router';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom, type Observable, of, ReplaySubject, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { AuthService } from '../../services/auth.service';
import { ACCOUNT_ACCESS_STORAGE_KEY, AuthMetadataStorageService } from '../auth/auth-metadata-storage.service';
import { SessionEventsService } from '../auth/session-events.service';
import { LocalizationService } from '../i18n/localization.service';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { BrowserNotificationCapabilityService } from './browser-notification-capability.service';
import { NotificationService, type WebPushSubscriptionRequest } from './notification.service';
import { PushNotificationService } from './push-notification.service';

let service: PushNotificationService;
let sessionEvents: SessionEventsService;
let subscription$: Subject<PushSubscription | null>;
let subscriptionChanges$: Subject<{ oldSubscription: PushSubscription | null; newSubscription: PushSubscription | null }>;
type NotificationClick = {
    notification: { tag?: string; data?: { targetUrl?: string; url?: string; notificationId?: string; userId?: string } };
};
let notificationClicks$: Subject<NotificationClick>;
let swPush: {
    isEnabled: boolean;
    subscription: Observable<PushSubscription | null>;
    pushSubscriptionChanges: Subject<{ oldSubscription: PushSubscription | null; newSubscription: PushSubscription | null }>;
    notificationClicks: Subject<NotificationClick>;
    requestSubscription: ReturnType<typeof vi.fn<() => Promise<PushSubscription>>>;
};
let authService: {
    isAuthenticated: ReturnType<typeof vi.fn>;
    isImpersonating: ReturnType<typeof vi.fn>;
    getUserId: ReturnType<typeof vi.fn>;
    getSessionKey: ReturnType<typeof vi.fn>;
};
let localizationService: { getCurrentLanguage: ReturnType<typeof vi.fn> };
let notificationService: {
    getWebPushConfiguration: ReturnType<typeof vi.fn>;
    upsertWebPushSubscription: ReturnType<typeof vi.fn>;
    removeWebPushSubscription: ReturnType<typeof vi.fn>;
    fetchUnreadCount: ReturnType<typeof vi.fn>;
    notifyNotificationsChanged: ReturnType<typeof vi.fn>;
    getNotificationPreferences: ReturnType<typeof vi.fn>;
    markAsRead: ReturnType<typeof vi.fn>;
};
let router: {
    navigateByUrl: ReturnType<typeof vi.fn>;
    events: Subject<NavigationEnd>;
    url: string;
    parseUrl: (url: string) => UrlTree;
};
const pushOwnerStorageKey = 'webPushSubscriptionOwner';
const cleanupTimeoutMs = 5000;
const subscriptionSettleMs = 20;

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    // SwPush re-reads PushManager.getSubscription for every new subscriber.
    // Replay only a discovered value; initial discovery can still be delayed.
    subscription$ = new ReplaySubject<PushSubscription | null>(1);
    subscriptionChanges$ = new Subject<{ oldSubscription: PushSubscription | null; newSubscription: PushSubscription | null }>();
    notificationClicks$ = new Subject<NotificationClick>();

    swPush = {
        isEnabled: true,
        subscription: subscription$,
        pushSubscriptionChanges: subscriptionChanges$,
        notificationClicks: notificationClicks$,
        requestSubscription: vi.fn<() => Promise<PushSubscription>>(),
    };
    authService = {
        isAuthenticated: vi.fn(() => true),
        isImpersonating: vi.fn(() => false),
        getUserId: vi.fn(() => 'user-a'),
        getSessionKey: vi.fn(() => 'token-a'),
    };
    localizationService = {
        getCurrentLanguage: vi.fn(() => 'en'),
    };
    notificationService = {
        getWebPushConfiguration: vi.fn(() => of({ enabled: true, publicKey: 'public-key' })),
        upsertWebPushSubscription: vi.fn(() => of(void 0)),
        removeWebPushSubscription: vi.fn(() => of(void 0)),
        fetchUnreadCount: vi.fn(),
        notifyNotificationsChanged: vi.fn(),
        getNotificationPreferences: vi.fn(() => of({ pushNotificationsEnabled: true })),
        markAsRead: vi.fn(() => of(void 0)),
    };
    router = {
        navigateByUrl: vi.fn().mockResolvedValue(true),
        events: new Subject<NavigationEnd>(),
        url: '/',
        parseUrl: (url): UrlTree => new DefaultUrlSerializer().parse(url),
    };

    TestBed.configureTestingModule({
        providers: [
            PushNotificationService,
            { provide: SwPush, useValue: swPush },
            { provide: AuthService, useValue: authService },
            { provide: LocalizationService, useValue: localizationService },
            { provide: NotificationService, useValue: notificationService },
            { provide: Router, useValue: router },
        ],
    });

    service = TestBed.inject(PushNotificationService);
    sessionEvents = TestBed.inject(SessionEventsService);
});

function createPushSubscription(
    endpoint: string,
    applicationServerKey: ArrayBuffer | null = null,
    expirationTime: number | null = null,
): PushSubscription & { unsubscribe: ReturnType<typeof vi.fn>; unsubscribeMock: ReturnType<typeof vi.fn> } {
    const options: PushSubscriptionOptions = {
        applicationServerKey,
        userVisibleOnly: true,
    };
    const unsubscribeMock = vi.fn().mockImplementation(
        async () =>
            new Promise<boolean>(resolve => {
                swPush.subscription = of(null);
                resolve(true);
            }),
    );

    return {
        endpoint,
        expirationTime,
        unsubscribe: unsubscribeMock,
        unsubscribeMock,
        toJSON: () => ({
            endpoint,
            expirationTime,
            keys: {
                p256dh: 'p256',
                auth: 'auth',
            },
        }),
        getKey: vi.fn(),
        options,
    };
}

function useSessionCleanupTimers(): void {
    beforeEach(() => {
        vi.useFakeTimers();
    });
    afterEach(() => {
        vi.useRealTimers();
    });
}

function confirmSubscriptionOwner(subscription: PushSubscription, userId: string): void {
    localStorage.setItem(pushOwnerStorageKey, JSON.stringify({ endpoint: subscription.endpoint, userId }));
}

function resolveRequestedSubscription(subscription: PushSubscription): void {
    swPush.requestSubscription.mockImplementation(
        async () =>
            new Promise<PushSubscription>(resolve => {
                swPush.subscription = of(subscription);
                resolve(subscription);
            }),
    );
}

function ownerConflict(): { status: number; error: { error: string; message: string } } {
    return { status: 400, error: { error: 'Validation.Invalid', message: 'Field Endpoint is invalid: Endpoint cannot be registered.' } };
}

function publishBobAfterGuestSubscription(subscription: PushSubscription | null): void {
    // Bob has previously opted in on this device; Alice's endpoint must still be revoked first.
    localStorage.setItem('webPushDeviceOptIn:user-b', 'true');
    vi.spyOn(TestBed.inject(BrowserNotificationCapabilityService), 'getPermission').mockReturnValue('granted');
    authService.isAuthenticated.mockReturnValue(false);
    authService.getUserId.mockReturnValue(null);
    authService.getSessionKey.mockReturnValue(null);
    localStorage.setItem('refreshSession', 'true');
    localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-a', 'User']));
    if (subscription !== null) {
        confirmSubscriptionOwner(subscription, 'user-a');
    }
    swPush.subscription = of(subscription);
    subscription$.next(subscription);
    TestBed.tick();
    sessionEvents.notifyAuthenticated();
    authService.isAuthenticated.mockReturnValue(true);
    authService.getUserId.mockReturnValue('user-b');
    authService.getSessionKey.mockReturnValue('token-b');
    localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
    TestBed.tick();
}

describe('PushNotificationService subscription support', () => {
    it('preserves the real browser endpoint and marker during token-only impersonation and logout', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/real-bob');
        confirmSubscriptionOwner(subscription, 'real-bob');
        authService.isImpersonating.mockReturnValue(true);
        authService.getUserId.mockReturnValue('impersonated-alice');
        authService.getSessionKey.mockReturnValue('impersonation-token');
        sessionEvents.notifyAuthenticated();
        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');

        const logoutReady = vi.fn();
        const logout = sessionEvents.prepareSessionEnd();
        void logout?.then(logoutReady);
        await nextBrowserTaskAsync();
        expect(logoutReady).toHaveBeenCalledOnce();

        swPush.subscription = of(subscription);
        subscription$.next(subscription);
        subscriptionChanges$.next({ oldSubscription: subscription, newSubscription: subscription });
        await waitForAsyncTasksAsync();

        expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: subscription.endpoint, userId: 'real-bob' }));
        expect(service.isSubscribed()).toBe(false);
    });
    it('should mark push unsupported when the subscription stream fails', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/current');
        subscription$.next(subscription);

        subscription$.error(new TypeError('PushManager is unavailable'));

        expect(service.isSupported()).toBe(false);
        expect(service.isSubscribed()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unsupported');
    });

    it('should return unsupported when service worker push is disabled', async () => {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [
                PushNotificationService,
                {
                    provide: SwPush,
                    useValue: {
                        isEnabled: false,
                        subscription: new Subject<PushSubscription | null>(),
                        pushSubscriptionChanges: new Subject(),
                        notificationClicks: new Subject(),
                        requestSubscription: vi.fn(),
                    },
                },
                { provide: AuthService, useValue: authService },
                { provide: LocalizationService, useValue: localizationService },
                { provide: NotificationService, useValue: notificationService },
                { provide: Router, useValue: router },
                {
                    provide: BrowserStorageService,
                    useFactory: (): BrowserStorageService => {
                        throw new Error('Unsupported push must not resolve storage');
                    },
                },
                {
                    provide: BrowserWindowService,
                    useFactory: (): BrowserWindowService => {
                        throw new Error('Unsupported push must not resolve browser locks');
                    },
                },
                {
                    provide: AuthMetadataStorageService,
                    useFactory: (): AuthMetadataStorageService => {
                        throw new Error('Unsupported push must not resolve owner storage');
                    },
                },
            ],
        });

        const disabledService = TestBed.inject(PushNotificationService);
        await expect(disabledService.ensureSubscriptionAsync()).resolves.toBe('unsupported');
    });
});

describe('PushNotificationService subscription lifecycle', () => {
    it('should upsert existing subscription and return already-subscribed', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/current');
        confirmSubscriptionOwner(subscription, 'user-a');
        swPush.subscription = of(subscription);

        const result = await service.ensureSubscriptionAsync();

        expect(result).toBe('already-subscribed');
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledWith(
            expect.objectContaining({
                endpoint: subscription.endpoint,
                locale: 'en',
            }),
        );
        expect(service.currentSubscriptionEndpoint()).toBe(subscription.endpoint);
    });

    it('should keep an existing subscription created with the current public key', async () => {
        const applicationServerKey = new TextEncoder().encode('old').buffer;
        const subscription = createPushSubscription('https://push.example.com/subscriptions/current', applicationServerKey);
        confirmSubscriptionOwner(subscription, 'user-a');
        notificationService.getWebPushConfiguration.mockReturnValue(of({ enabled: true, publicKey: 'b2xk' }));
        swPush.subscription = of(subscription);

        const result = await service.ensureSubscriptionAsync();

        expect(result).toBe('already-subscribed');
        expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledWith(
            expect.objectContaining({ endpoint: subscription.endpoint }),
        );
    });
});

describe('PushNotificationService registration authentication', () => {
    it('uses normal authentication for registration after the access token refreshes while configuration is pending', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/current');
        confirmSubscriptionOwner(subscription, 'user-a');
        const configuration$ = new Subject<{ enabled: boolean; publicKey: string }>();
        swPush.subscription = of(subscription);
        notificationService.getWebPushConfiguration.mockReturnValue(configuration$);

        const enabling = service.ensureSubscriptionAsync();
        await vi.waitFor(() => {
            expect(notificationService.getWebPushConfiguration).toHaveBeenCalled();
        });

        authService.getSessionKey.mockReturnValue('refreshed-token-a');
        configuration$.next({ enabled: true, publicKey: 'public-key' });
        await expect(enabling).resolves.toBe('already-subscribed');

        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledWith(
            expect.objectContaining({ endpoint: subscription.endpoint }),
        );
    });
});

describe('PushNotificationService service worker renewal', () => {
    useSessionCleanupTimers();

    async function beginWithConfirmedDeviceAsync(subscription: PushSubscription): Promise<void> {
        confirmSubscriptionOwner(subscription, 'user-a');
        swPush.subscription = of(subscription);
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(0);
        expect(service.currentSubscriptionEndpoint()).toBe(subscription.endpoint);
        notificationService.upsertWebPushSubscription.mockClear();
    }

    it('registers a renewed endpoint and its expiration after the old server registration is removed', async () => {
        const old = createPushSubscription('https://push.example.com/subscriptions/before-renewal');
        const expiry = '2026-12-01T00:00:00.000Z';
        const renewed = createPushSubscription('https://push.example.com/subscriptions/renewed', null, Date.parse(expiry));
        await beginWithConfirmedDeviceAsync(old);

        const removal = new Subject<void>();
        notificationService.removeWebPushSubscription.mockReturnValueOnce(removal);
        swPush.subscription = of(renewed);
        subscriptionChanges$.next({ oldSubscription: old, newSubscription: renewed });
        await vi.advanceTimersByTimeAsync(0);

        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(old.endpoint);
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        removal.next();
        await vi.advanceTimersByTimeAsync(0);

        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ endpoint: renewed.endpoint, expirationTime: expiry }),
        );
        expect(service.currentSubscriptionEndpoint()).toBe(renewed.endpoint);
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: renewed.endpoint, userId: 'user-a' }));
        expect(old.unsubscribeMock).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
    });

    it('clears the device state when the service worker reports an expired subscription without a replacement', async () => {
        const current = createPushSubscription('https://push.example.com/subscriptions/expired');
        await beginWithConfirmedDeviceAsync(current);

        swPush.subscription = of(null);
        subscriptionChanges$.next({ oldSubscription: current, newSubscription: null });
        await vi.advanceTimersByTimeAsync(0);

        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(current.endpoint);
        expect(service.isSubscribed()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBeNull();

        window.dispatchEvent(new Event('focus'));
        await vi.advanceTimersByTimeAsync(0);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledOnce();
    });

    it('lets logout clean up its own registration when a queued worker renewal belongs to the ending session', async () => {
        const current = createPushSubscription('https://push.example.com/subscriptions/logout-before-renewal');
        await beginWithConfirmedDeviceAsync(current);

        swPush.subscription = of(null);
        subscriptionChanges$.next({ oldSubscription: current, newSubscription: null });
        const cleanup = sessionEvents.prepareSessionEnd();
        await vi.advanceTimersByTimeAsync(0);
        await cleanup;

        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(current.endpoint, 'token-a');
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });

    it('leaves the device unregistered when a renewed endpoint cannot be saved', async () => {
        const old = createPushSubscription('https://push.example.com/subscriptions/old-after-offline');
        const renewed = createPushSubscription('https://push.example.com/subscriptions/renewed-offline');
        await beginWithConfirmedDeviceAsync(old);
        notificationService.upsertWebPushSubscription.mockReturnValueOnce(throwError(() => new Error('Offline')));

        swPush.subscription = of(renewed);
        subscriptionChanges$.next({ oldSubscription: old, newSubscription: renewed });
        await vi.advanceTimersByTimeAsync(0);

        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(old.endpoint);
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ endpoint: renewed.endpoint }),
        );
        expect(service.isSubscribed()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });
});

describe('PushNotificationService public key rotation', () => {
    it('should replace an existing subscription created with a different public key', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/stale', new TextEncoder().encode('old').buffer);
        confirmSubscriptionOwner(subscription, 'user-a');
        const replacement = createPushSubscription(
            'https://push.example.com/subscriptions/replacement',
            new TextEncoder().encode('new').buffer,
        );
        notificationService.getWebPushConfiguration.mockReturnValue(of({ enabled: true, publicKey: 'bmV3' }));
        swPush.subscription = of(subscription);
        resolveRequestedSubscription(replacement);

        const result = await service.ensureSubscriptionAsync();

        expect(result).toBe('subscribed');
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint);
        expect(subscription.unsubscribeMock).toHaveBeenCalledTimes(1);
        expect(swPush.requestSubscription).toHaveBeenCalledWith({ serverPublicKey: 'bmV3' });
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledWith(
            expect.objectContaining({ endpoint: replacement.endpoint }),
        );
        expect(service.currentSubscriptionEndpoint()).toBe(replacement.endpoint);
    });

    it('reports unavailable when the browser cannot rotate an obsolete key, then enables after a retry', async () => {
        const previous = createPushSubscription('https://push.example.com/subscriptions/old-key', new TextEncoder().encode('old').buffer);
        const replacement = createPushSubscription(
            'https://push.example.com/subscriptions/new-key',
            new TextEncoder().encode('new').buffer,
        );
        confirmSubscriptionOwner(previous, 'user-a');
        swPush.subscription = of(previous);
        notificationService.getWebPushConfiguration.mockReturnValue(of({ enabled: true, publicKey: 'b2xk' }));
        await expect(service.ensureSubscriptionAsync()).resolves.toBe('already-subscribed');
        expect(service.currentSubscriptionEndpoint()).toBe(previous.endpoint);

        previous.unsubscribeMock.mockResolvedValueOnce(false);
        notificationService.getWebPushConfiguration.mockReturnValue(of({ enabled: true, publicKey: 'bmV3' }));
        resolveRequestedSubscription(replacement);

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');
        expect(service.isBusy()).toBe(false);
        expect(service.isSubscribed()).toBe(false);
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('subscribed');
        expect(previous.unsubscribeMock).toHaveBeenCalledTimes(2);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(previous.endpoint);
        expect(service.currentSubscriptionEndpoint()).toBe(replacement.endpoint);
    });
});

describe('PushNotificationService subscription failures', () => {
    it.each([false, true])('does not let queued Alice rotate Bob replacement endpoint (storage event processed=%s)', async processed => {
        const alice = createPushSubscription('https://push.example.com/subscriptions/queued-alice');
        const bob = createPushSubscription('https://push.example.com/subscriptions/replacement-bob');
        confirmSubscriptionOwner(alice, 'user-a');
        swPush.subscription = of(alice);
        let releaseLock = (): void => {};
        const gate = new Promise<void>(resolve => {
            releaseLock = resolve;
        });
        vi.spyOn(TestBed.inject(BrowserWindowService), 'runWithLockAsync').mockImplementation(
            async <T>(_name: string, operation: () => Promise<T>): Promise<T> => gate.then(operation),
        );

        const pending = service.ensureSubscriptionAsync();
        await waitForAsyncTasksAsync();
        expect(notificationService.getWebPushConfiguration).not.toHaveBeenCalled();
        localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
        if (processed) {
            authService.getUserId.mockReturnValue('user-b');
            authService.getSessionKey.mockReturnValue('token-b');
            sessionEvents.notifyAuthenticated();
        }
        confirmSubscriptionOwner(bob, 'user-b');
        swPush.subscription = of(bob);
        releaseLock();

        await expect(pending).resolves.toBe('unavailable');
        expect(alice.unsubscribeMock).not.toHaveBeenCalled();
        expect(bob.unsubscribeMock).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
    });

    it('treats an unsuccessful marker write as unknown on the next reconciliation', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/unpersisted');
        const write = vi.spyOn(TestBed.inject(BrowserStorageService), 'setJson').mockImplementation(() => {});
        swPush.subscription = of(null);
        resolveRequestedSubscription(subscription);
        await expect(service.ensureSubscriptionAsync()).resolves.toBe('subscribed');
        expect(localStorage.getItem(pushOwnerStorageKey)).toBeNull();
        expect(write).toHaveBeenCalled();

        swPush.subscription = of(subscription);
        notificationService.getWebPushConfiguration.mockReturnValue(throwError(() => new Error('Offline')));
        notificationService.upsertWebPushSubscription.mockReturnValue(throwError(() => new Error('Offline')));
        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');
        expect(subscription.unsubscribeMock).toHaveBeenCalledOnce();
        expect(service.isSubscribed()).toBe(false);
    });
});

describe('PushNotificationService confirmed ownership', () => {
    it('rotates only the explicitly owner-rejected persisted endpoint for the current session', async () => {
        const rejected = createPushSubscription('https://push.example.com/subscriptions/foreign');
        const replacement = createPushSubscription('https://push.example.com/subscriptions/own-replacement');
        confirmSubscriptionOwner(rejected, 'user-a');
        swPush.subscription = of(rejected);
        notificationService.upsertWebPushSubscription.mockImplementation((request: WebPushSubscriptionRequest) =>
            request.endpoint === rejected.endpoint ? throwError(ownerConflict) : of(void 0),
        );
        resolveRequestedSubscription(replacement);

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('subscribed');

        expect(rejected.unsubscribeMock).toHaveBeenCalledOnce();
        expect(replacement.unsubscribeMock).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBe(replacement.endpoint);
    });

    it('reports unavailable when the browser cannot disconnect an endpoint rejected by the server', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/rejected-unsubscribe');
        confirmSubscriptionOwner(subscription, 'user-a');
        subscription.unsubscribeMock.mockResolvedValue(false);
        swPush.subscription = of(subscription);
        notificationService.upsertWebPushSubscription.mockReturnValue(throwError(ownerConflict));

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');

        expect(subscription.unsubscribeMock).toHaveBeenCalledOnce();
        expect(service.isBusy()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
    });

    it('disconnects an owner-rejected endpoint when no replacement key is configured', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/no-configured-key');
        confirmSubscriptionOwner(subscription, 'user-a');
        swPush.subscription = of(subscription);
        notificationService.getWebPushConfiguration.mockReturnValue(of({ enabled: false, publicKey: null }));
        notificationService.upsertWebPushSubscription.mockReturnValue(throwError(ownerConflict));

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');

        expect(subscription.unsubscribeMock).toHaveBeenCalledOnce();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBeNull();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
    });

    it.each([
        { status: 0, error: null },
        { status: 401, error: { error: 'Authentication.InvalidToken' } },
        { status: 400, error: { error: 'Validation.Invalid', message: 'Field Endpoint is invalid: Invalid endpoint.' } },
    ])('preserves a confirmed owner endpoint for unrelated registration error %j', async error => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/confirmed');
        confirmSubscriptionOwner(subscription, 'user-a');
        swPush.subscription = of(subscription);
        notificationService.upsertWebPushSubscription.mockReturnValue(throwError(() => error));

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');

        expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
    });
});

describe('PushNotificationService offline account takeover', () => {
    it('revokes Alice before the receiving Bob configuration and registration can finish', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/guest-alice');
        const replacement = createPushSubscription('https://push.example.com/subscriptions/receiving-bob');
        const configuration = new Subject<{ enabled: boolean; publicKey: string }>();
        const registration = new Subject<void>();
        notificationService.getWebPushConfiguration.mockReturnValue(configuration);
        notificationService.upsertWebPushSubscription.mockReturnValue(registration);
        resolveRequestedSubscription(replacement);
        publishBobAfterGuestSubscription(subscription);
        await vi.waitFor(() => {
            expect(notificationService.getWebPushConfiguration).toHaveBeenCalled();
        });

        expect(subscription.unsubscribeMock).toHaveBeenCalledOnce();
        expect(subscription.unsubscribeMock.mock.invocationCallOrder[0]).toBeLessThan(
            notificationService.getWebPushConfiguration.mock.invocationCallOrder[0],
        );
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBeNull();
        configuration.next({ enabled: true, publicKey: 'public-key' });
        await vi.waitFor(() => {
            expect(notificationService.upsertWebPushSubscription).toHaveBeenCalled();
        });
        expect(subscription.unsubscribeMock).toHaveBeenCalledOnce();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBeNull();
        registration.error(new Error('Registration unavailable'));
        await waitForAsyncTasksAsync();
    });
});

describe('PushNotificationService automatic owner replacement', () => {
    it('restores a previously existing subscription for Bob after privacy revocation without manual enabling', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/legacy-alice');
        const replacement = createPushSubscription('https://push.example.com/subscriptions/automatic-bob');
        resolveRequestedSubscription(replacement);
        publishBobAfterGuestSubscription(subscription);
        await vi.waitFor(() => {
            expect(service.currentSubscriptionEndpoint()).toBe(replacement.endpoint);
        });
        expect(subscription.unsubscribeMock).toHaveBeenCalledOnce();
        expect(replacement.unsubscribeMock).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledWith(
            expect.objectContaining({ endpoint: replacement.endpoint }),
        );
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: replacement.endpoint, userId: 'user-b' }));
    });

    it('keeps a genuinely empty subscription opted out during Bob authentication', async () => {
        const owner = signal<string | null>(null);
        authService.isAuthenticated.mockImplementation(() => owner() !== null);
        authService.getUserId.mockImplementation(() => owner());
        authService.getSessionKey.mockImplementation(() => (owner() === null ? null : 'token-b'));
        swPush.subscription = of(null);
        subscription$.next(null);
        TestBed.tick();
        sessionEvents.notifyAuthenticated();
        localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
        owner.set('user-b');
        TestBed.tick();
        await waitForAsyncTasksAsync();
        expect(notificationService.getWebPushConfiguration).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });
});

describe('PushNotificationService offline endpoint ownership', () => {
    it.each(['unknown', 'foreign'])('revokes %s persisted ownership before offline reconciliation', async ownership => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/possibly-alice');
        authService.getUserId.mockReturnValue('user-b');
        authService.getSessionKey.mockReturnValue('token-b');
        if (ownership === 'foreign') {
            confirmSubscriptionOwner(subscription, 'user-a');
        }
        swPush.subscription = of(subscription);
        notificationService.removeWebPushSubscription.mockReturnValue(throwError(() => ({ status: 401 })));
        notificationService.getWebPushConfiguration.mockReturnValue(throwError(() => new Error('Offline')));
        notificationService.upsertWebPushSubscription.mockReturnValue(throwError(() => new Error('Offline')));

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');

        expect(subscription.unsubscribeMock).toHaveBeenCalledOnce();
        expect(service.isSubscribed()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
    });

    it('stops Alice delivery when both Alice cleanup and receiving Bob registration are offline', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/offline-alice');
        confirmSubscriptionOwner(subscription, 'user-a');
        swPush.subscription = of(subscription);
        subscription$.next(subscription);
        notificationService.removeWebPushSubscription.mockReturnValue(throwError(() => ({ status: 401 })));
        sessionEvents.notifyLocalSessionInvalidating();
        authService.isAuthenticated.mockReturnValue(false);
        authService.getUserId.mockReturnValue(null);
        authService.getSessionKey.mockReturnValue(null);
        sessionEvents.notifyLocalSessionInvalidated();
        await vi.waitFor(() => {
            expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint, 'token-a');
        });
        expect(subscription.unsubscribeMock).not.toHaveBeenCalled();

        authService.isAuthenticated.mockReturnValue(true);
        authService.getUserId.mockReturnValue('user-b');
        authService.getSessionKey.mockReturnValue('token-b');
        localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
        notificationService.getWebPushConfiguration.mockReturnValue(throwError(() => new Error('Offline')));
        notificationService.upsertWebPushSubscription.mockReturnValue(throwError(() => new Error('Offline')));
        sessionEvents.notifyAuthenticated();

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');
        expect(subscription.unsubscribeMock).toHaveBeenCalledOnce();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });
});

describe('PushNotificationService configuration failures', () => {
    it.each([
        { enabled: false, publicKey: 'valid-key' },
        { enabled: true, publicKey: null },
        { enabled: true, publicKey: '' },
    ])('does not prompt when the API configuration prevents registration (%j)', async configuration => {
        swPush.subscription = of(null);
        notificationService.getWebPushConfiguration.mockReturnValue(of(configuration));

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');

        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(service.isBusy()).toBe(false);
    });

    it('preserves confirmed Bob browser delivery when Bob configuration and registration are offline', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/offline-bob');
        confirmSubscriptionOwner(subscription, 'user-b');
        authService.getUserId.mockReturnValue('user-b');
        authService.getSessionKey.mockReturnValue('token-b');
        localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
        swPush.subscription = of(subscription);
        notificationService.getWebPushConfiguration.mockReturnValue(throwError(() => new Error('Offline')));
        notificationService.upsertWebPushSubscription.mockReturnValue(throwError(() => new Error('Offline')));

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');

        expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: subscription.endpoint, userId: 'user-b' }));
    });

    it('should retain the existing subscription when configuration cannot be loaded', async () => {
        const subscription = createPushSubscription(
            'https://push.example.com/subscriptions/current',
            new TextEncoder().encode('old').buffer,
        );
        confirmSubscriptionOwner(subscription, 'user-a');
        swPush.subscription = of(subscription);
        notificationService.getWebPushConfiguration.mockReturnValue(throwError(() => new Error('Configuration unavailable')));

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('already-subscribed');

        expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBe(subscription.endpoint);
    });

    it('should clear stale state when requesting a replacement subscription fails', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/stale', new TextEncoder().encode('old').buffer);
        confirmSubscriptionOwner(subscription, 'user-a');
        subscription$.next(subscription);
        swPush.subscription = of(subscription);
        notificationService.getWebPushConfiguration.mockReturnValue(of({ enabled: true, publicKey: 'bmV3' }));
        swPush.requestSubscription.mockRejectedValue(new Error('Push service unavailable'));

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');

        expect(subscription.unsubscribeMock).toHaveBeenCalledTimes(1);
        expect(service.isSubscribed()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
        expect(service.isBusy()).toBe(false);
    });
});

describe('PushNotificationService subscription creation and removal', () => {
    it('should request and persist a new subscription', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/new');
        swPush.subscription = of(null);
        resolveRequestedSubscription(subscription);

        const result = await service.ensureSubscriptionAsync();

        expect(result).toBe('subscribed');
        expect(swPush.requestSubscription).toHaveBeenCalledWith({ serverPublicKey: 'public-key' });
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledWith(
            expect.objectContaining({
                endpoint: subscription.endpoint,
            }),
        );
    });

    it('should remove current subscription and clear local state', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/current');
        swPush.subscription = of(subscription);

        const removed = await service.removeSubscriptionAsync(subscription.endpoint);

        expect(removed).toBe(true);
        expect(subscription.unsubscribeMock).toHaveBeenCalledTimes(1);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });

    it('keeps the current browser subscribed and serializes device changes while removing a different device', async () => {
        const local = createPushSubscription('https://push.example.com/subscriptions/local');
        const remote = 'https://push.example.com/subscriptions/remote';
        confirmSubscriptionOwner(local, 'user-a');
        swPush.subscription = of(local);
        await expect(service.ensureSubscriptionAsync()).resolves.toBe('already-subscribed');
        const removal = new Subject<void>();
        notificationService.removeWebPushSubscription.mockReturnValueOnce(removal);

        const pending = service.removeSubscriptionAsync(remote);
        expect(service.isBusy()).toBe(true);
        await expect(service.removeSubscriptionAsync(local.endpoint)).resolves.toBe(false);
        await expect(service.ensureSubscriptionAsync()).resolves.toBe('already-subscribed');
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(remote);
        removal.next();
        await expect(pending).resolves.toBe(true);

        expect(service.isBusy()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBe(local.endpoint);
        expect(local.unsubscribeMock).not.toHaveBeenCalled();
    });
});

describe('Push notification device removal across session and network changes', () => {
    it('drops a pending removal when another account signs in before browser discovery completes', async () => {
        const oldDevice = createPushSubscription('https://push.example.com/subscriptions/old-device');
        const newDevice = createPushSubscription('https://push.example.com/subscriptions/new-device');
        const browserDiscovery = new Subject<PushSubscription | null>();
        swPush.subscription = browserDiscovery;
        const removal = service.removeSubscriptionAsync(oldDevice.endpoint);
        expect(browserDiscovery.observed).toBe(true);
        expect(service.isBusy()).toBe(true);

        authService.getUserId.mockReturnValue('user-b');
        authService.getSessionKey.mockReturnValue('token-b');
        confirmSubscriptionOwner(newDevice, 'user-b');
        swPush.subscription = of(newDevice);
        sessionEvents.notifyAuthenticated();
        browserDiscovery.next(oldDevice);

        await expect(removal).resolves.toBe(false);
        await expect(service.ensureSubscriptionAsync()).resolves.toBe('already-subscribed');
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(oldDevice.unsubscribeMock).not.toHaveBeenCalled();
        expect(newDevice.unsubscribeMock).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBe(newDevice.endpoint);
        expect(service.isBusy()).toBe(false);
    });

    it('retains the current device and permits retry when removing another device fails temporarily', async () => {
        const local = createPushSubscription('https://push.example.com/subscriptions/local-device');
        const remote = 'https://push.example.com/subscriptions/remote-device';
        confirmSubscriptionOwner(local, 'user-a');
        swPush.subscription = of(local);
        await expect(service.ensureSubscriptionAsync()).resolves.toBe('already-subscribed');
        notificationService.removeWebPushSubscription.mockReturnValueOnce(throwError(() => new Error('Connection interrupted')));

        await expect(service.removeSubscriptionAsync(remote)).resolves.toBe(false);
        expect(service.isBusy()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBe(local.endpoint);
        expect(local.unsubscribeMock).not.toHaveBeenCalled();

        await expect(service.removeSubscriptionAsync(remote)).resolves.toBe(true);
        expect(notificationService.removeWebPushSubscription.mock.calls).toEqual([[remote], [remote]]);
        expect(service.currentSubscriptionEndpoint()).toBe(local.endpoint);
        expect(local.unsubscribeMock).not.toHaveBeenCalled();
    });
});

describe('PushNotificationService delayed browser subscription discovery', () => {
    useSessionCleanupTimers();

    it('revokes an old persisted subscription first discovered while logout cleanup is pending and another account signs in', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/account-a');

        const cleanup = sessionEvents.prepareSessionEnd();
        expect(cleanup).not.toBeNull();
        const cleanupFinished = vi.fn();
        void cleanup?.then(cleanupFinished);
        await vi.advanceTimersByTimeAsync(0);
        expect(cleanupFinished).not.toHaveBeenCalled();

        authService.getUserId.mockReturnValue('user-b');
        authService.getSessionKey.mockReturnValue('token-b');
        sessionEvents.notifyAuthenticated();
        TestBed.tick();
        subscription$.next(subscription);
        await cleanup;
        await vi.advanceTimersByTimeAsync(0);

        expect(subscription.unsubscribeMock).toHaveBeenCalledTimes(1);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledTimes(1);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint, 'token-a');
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(service.isSubscribed()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });

    it('retains the old owner for a persisted subscription discovered after logout discovery times out and another account signs in', async () => {
        const discoveryTimeoutMs = 5000;
        const subscription = createPushSubscription('https://push.example.com/subscriptions/account-a');

        const cleanup = sessionEvents.prepareSessionEnd();
        await vi.advanceTimersByTimeAsync(discoveryTimeoutMs);
        await expect(cleanup).resolves.toBeUndefined();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();

        authService.getUserId.mockReturnValue('user-b');
        authService.getSessionKey.mockReturnValue('token-b');
        sessionEvents.notifyAuthenticated();
        TestBed.tick();
        subscription$.next(subscription);
        await vi.advanceTimersByTimeAsync(0);

        expect(subscription.unsubscribeMock).toHaveBeenCalledTimes(1);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledTimes(1);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint, 'token-a');
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(service.isSubscribed()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });
});

describe('Push notification actions during browser changes', () => {
    it('does not start a second registration while browser permission is pending', async () => {
        const endpoint = createPushSubscription('https://push.example.com/subscriptions/permission-pending');
        const permission = new Subject<PushSubscription>();
        swPush.subscription = of(null);
        swPush.requestSubscription.mockImplementation(async () => {
            const subscription = await firstValueFrom(permission);
            swPush.subscription = of(subscription);
            return subscription;
        });

        const pending = service.ensureSubscriptionAsync();
        await vi.waitFor(() => {
            expect(swPush.requestSubscription).toHaveBeenCalledOnce();
        });
        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');
        expect(service.isBusy()).toBe(true);
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();

        permission.next(endpoint);
        permission.complete();

        await expect(pending).resolves.toBe('subscribed');
        expect(swPush.requestSubscription).toHaveBeenCalledOnce();
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ endpoint: endpoint.endpoint }),
        );
        expect(service.currentSubscriptionEndpoint()).toBe(endpoint.endpoint);
        expect(service.isBusy()).toBe(false);
    });

    it('leaves the device registered when the browser declines to disconnect and permits retry', async () => {
        const endpoint = createPushSubscription('https://push.example.com/subscriptions/declined-disconnect');
        confirmSubscriptionOwner(endpoint, 'user-a');
        swPush.subscription = of(endpoint);
        await expect(service.ensureSubscriptionAsync()).resolves.toBe('already-subscribed');
        endpoint.unsubscribeMock.mockResolvedValueOnce(false);

        await expect(service.removeSubscriptionAsync(endpoint.endpoint)).resolves.toBe(false);

        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBe(endpoint.endpoint);
        expect(service.isBusy()).toBe(false);
        await expect(service.removeSubscriptionAsync(endpoint.endpoint)).resolves.toBe(true);
        expect(endpoint.unsubscribeMock).toHaveBeenCalledTimes(2);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(endpoint.endpoint);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });

    it('reconciles a confirmed browser endpoint after a renewal without a previous endpoint', async () => {
        const endpoint = createPushSubscription('https://push.example.com/subscriptions/renewed-without-old');
        confirmSubscriptionOwner(endpoint, 'user-a');
        swPush.subscription = of(endpoint);
        subscription$.next(endpoint);
        TestBed.tick();
        await expect(service.ensureSubscriptionAsync()).resolves.toBe('already-subscribed');
        await waitForAsyncTasksAsync();
        notificationService.upsertWebPushSubscription.mockClear();

        subscriptionChanges$.next({ oldSubscription: null, newSubscription: endpoint });

        await vi.waitFor(() => {
            expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledExactlyOnceWith(
                expect.objectContaining({ endpoint: endpoint.endpoint }),
            );
        });
        expect(service.currentSubscriptionEndpoint()).toBe(endpoint.endpoint);
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(endpoint.unsubscribeMock).not.toHaveBeenCalled();
    });
});

describe('Push notification ownership after a worker renewal', () => {
    it('disconnects an unconfirmed renewed endpoint without registering it for the current account', async () => {
        const endpoint = createPushSubscription('https://push.example.com/subscriptions/unconfirmed-renewal');
        swPush.subscription = of(null);
        subscription$.next(null);
        TestBed.tick();
        await waitForAsyncTasksAsync();
        swPush.subscription = of(endpoint);

        subscriptionChanges$.next({ oldSubscription: null, newSubscription: endpoint });

        await vi.waitFor(() => {
            expect(endpoint.unsubscribeMock).toHaveBeenCalledOnce();
        });
        await waitForAsyncTasksAsync();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });
});

function invalidateAliceLocally(): void {
    sessionEvents.notifyLocalSessionInvalidating();
    authService.isAuthenticated.mockReturnValue(false);
    authService.getUserId.mockReturnValue(null);
    authService.getSessionKey.mockReturnValue(null);
    sessionEvents.notifyLocalSessionInvalidated();
    TestBed.tick();
}

function expectBrowserPreservedWithScopedCleanup(subscription: ReturnType<typeof createPushSubscription>): void {
    expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
    expect(notificationService.removeWebPushSubscription).toHaveBeenCalled();
    expect(notificationService.removeWebPushSubscription.mock.calls.every(([, token]) => token === 'token-a' || token === 'token-b')).toBe(
        true,
    );
    expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
    expect(service.isSubscribed()).toBe(false);
    expect(service.currentSubscriptionEndpoint()).toBeNull();
}

describe('Push notification device operations after account ownership changes', () => {
    useSessionCleanupTimers();

    it('does not revoke a newly published browser endpoint after the previous tab invalidates itself', async () => {
        swPush.subscription = of(null);
        subscription$.next(null);
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(0);
        invalidateAliceLocally();

        const shared = createPushSubscription('https://push.example.com/subscriptions/newly-published-b');
        confirmSubscriptionOwner(shared, 'user-b');
        localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
        swPush.subscription = of(shared);
        subscription$.next(shared);
        await vi.advanceTimersByTimeAsync(0);

        expect(shared.unsubscribeMock).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(shared.endpoint, 'token-a');
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: shared.endpoint, userId: 'user-b' }));
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });

    it('drops the old server removal after the browser finishes unsubscribing under a different account', async () => {
        const previous = createPushSubscription('https://push.example.com/subscriptions/unsubscribing-a');
        confirmSubscriptionOwner(previous, 'user-a');
        swPush.subscription = of(previous);
        subscription$.next(previous);
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(0);
        expect(service.currentSubscriptionEndpoint()).toBe(previous.endpoint);
        notificationService.upsertWebPushSubscription.mockClear();

        const browserRelease = new Subject<boolean>();
        previous.unsubscribeMock.mockReturnValueOnce(firstValueFrom(browserRelease));
        const removal = service.removeSubscriptionAsync(previous.endpoint);
        await vi.advanceTimersByTimeAsync(0);
        expect(previous.unsubscribeMock).toHaveBeenCalledOnce();
        expect(browserRelease.observed).toBe(true);
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();

        const replacement = createPushSubscription('https://push.example.com/subscriptions/replacement-account-b');
        confirmSubscriptionOwner(replacement, 'user-b');
        localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
        swPush.subscription = of(replacement);
        browserRelease.next(true);

        await expect(removal).resolves.toBe(false);
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(replacement.unsubscribeMock).not.toHaveBeenCalled();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: replacement.endpoint, userId: 'user-b' }));
    });
});

describe('PushNotificationService local account invalidation', () => {
    useSessionCleanupTimers();

    it.each(['user-a', 'user-b'])(
        'deletes only captured Alice ownership and preserves the shared browser endpoint (server owner=%s)',
        async initialOwner => {
            const subscription = createPushSubscription('https://push.example.com/subscriptions/shared');
            let serverOwner: string | null = initialOwner;
            notificationService.removeWebPushSubscription.mockImplementation((_endpoint: string, bearer: string) => {
                if (bearer === 'token-a' && serverOwner === 'user-a') {
                    serverOwner = null;
                }
                return of(void 0);
            });
            subscription$.next(subscription);

            invalidateAliceLocally();
            await vi.advanceTimersByTimeAsync(0);

            expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
            expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint, 'token-a');
            expect(serverOwner).toBe(initialOwner === 'user-a' ? null : 'user-b');
            expect(service.isSubscribed()).toBe(false);
        },
    );

    it('drains an Alice upsert before server-only local invalidation cleanup', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/alice-upsert');
        const upsert = new Subject<void>();
        confirmSubscriptionOwner(subscription, 'user-a');
        swPush.subscription = of(subscription);
        notificationService.upsertWebPushSubscription.mockReturnValue(upsert);
        const enabling = service.ensureSubscriptionAsync();
        await vi.advanceTimersByTimeAsync(0);
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalled();

        invalidateAliceLocally();
        await vi.advanceTimersByTimeAsync(0);
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
        upsert.next();
        await enabling;
        await vi.advanceTimersByTimeAsync(0);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint, 'token-a');
        expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
    });

    it('ignores a late initial browser subscription owned by another tab', async () => {
        invalidateAliceLocally();
        const sharedBob = createPushSubscription('https://push.example.com/subscriptions/bob');

        subscription$.next(sharedBob);
        await vi.advanceTimersByTimeAsync(0);

        expectBrowserPreservedWithScopedCleanup(sharedBob);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(sharedBob.endpoint, 'token-a');
    });

    it('ignores renewal of the shared browser subscription after local invalidation', async () => {
        const oldSubscription = createPushSubscription('https://push.example.com/subscriptions/alice');
        const sharedBob = createPushSubscription('https://push.example.com/subscriptions/bob-renewed');
        invalidateAliceLocally();

        subscriptionChanges$.next({ oldSubscription, newSubscription: sharedBob });
        await vi.advanceTimersByTimeAsync(0);

        expectBrowserPreservedWithScopedCleanup(sharedBob);
        expect(oldSubscription.unsubscribeMock).not.toHaveBeenCalled();
    });
});

describe('PushNotificationService invalidated discovery epochs', () => {
    useSessionCleanupTimers();

    it.each([false, true])(
        'does not revoke a permission result from an invalidated epoch (intermediate login=%s)',
        async intermediateLogin => {
            const sharedBob = createPushSubscription('https://push.example.com/subscriptions/late-permission');
            const permission = new Subject<PushSubscription>();
            swPush.subscription = of(null);
            swPush.requestSubscription.mockImplementation(async () => {
                const subscription = await firstValueFrom(permission);
                swPush.subscription = of(subscription);
                return subscription;
            });
            const enabling = service.ensureSubscriptionAsync();
            await vi.advanceTimersByTimeAsync(0);
            expect(swPush.requestSubscription).toHaveBeenCalledOnce();

            if (intermediateLogin) {
                authService.getUserId.mockReturnValue('user-b');
                authService.getSessionKey.mockReturnValue('token-b');
                sessionEvents.notifyAuthenticated();
            }
            invalidateAliceLocally();
            permission.next(sharedBob);
            await expect(enabling).resolves.toBe('unavailable');

            expectBrowserPreservedWithScopedCleanup(sharedBob);
        },
    );

    it.each([false, true])('cancels older discovery ownership on local invalidation (discovery timed out=%s)', async timedOut => {
        const cleanup = sessionEvents.prepareSessionEnd();
        if (timedOut) {
            await vi.advanceTimersByTimeAsync(cleanupTimeoutMs);
            await cleanup;
        }
        authService.getUserId.mockReturnValue('user-b');
        authService.getSessionKey.mockReturnValue('token-b');
        sessionEvents.notifyAuthenticated();
        TestBed.tick();
        invalidateAliceLocally();
        const sharedBob = createPushSubscription('https://push.example.com/subscriptions/bob-after-discovery');

        subscription$.next(sharedBob);
        await vi.advanceTimersByTimeAsync(0);
        await cleanup;

        expectBrowserPreservedWithScopedCleanup(sharedBob);
    });
});

describe('PushNotificationService browser revocation', () => {
    useSessionCleanupTimers();

    it('revokes the browser and server subscription while subscription synchronization is busy', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/current');
        confirmSubscriptionOwner(subscription, 'user-a');
        const configuration$ = new Subject<{ enabled: boolean; publicKey: string }>();
        subscription$.next(subscription);
        swPush.subscription = of(subscription);
        notificationService.getWebPushConfiguration.mockReturnValue(configuration$);

        const enabling = service.ensureSubscriptionAsync();
        await vi.advanceTimersByTimeAsync(0);
        expect(service.isBusy()).toBe(true);

        const cleanup = sessionEvents.prepareSessionEnd();
        expect(cleanup).not.toBeNull();
        await cleanup;

        expect(subscription.unsubscribeMock).toHaveBeenCalledTimes(1);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint, 'token-a');
        expect(service.isBusy()).toBe(false);
        expect(service.isSubscribed()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();

        configuration$.next({ enabled: true, publicKey: 'public-key' });
        await expect(enabling).resolves.toBe('unavailable');
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
    });

    it('revokes the browser subscription when server cleanup fails offline', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/current');
        subscription$.next(subscription);
        notificationService.removeWebPushSubscription.mockReturnValue(throwError(() => new Error('Offline')));

        await expect(sessionEvents.prepareSessionEnd()).resolves.toBeUndefined();

        expect(subscription.unsubscribeMock).toHaveBeenCalledTimes(1);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint, 'token-a');
        expect(service.isSubscribed()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });
});

describe('PushNotificationService stale subscription creation', () => {
    useSessionCleanupTimers();

    it('ignores configuration that arrives after session cleanup', async () => {
        const configuration$ = new Subject<{ enabled: boolean; publicKey: string }>();
        swPush.subscription = of(null);
        notificationService.getWebPushConfiguration.mockReturnValue(configuration$);

        const enabling = service.ensureSubscriptionAsync();
        await vi.advanceTimersByTimeAsync(0);
        expect(notificationService.getWebPushConfiguration).toHaveBeenCalledTimes(1);

        await sessionEvents.prepareSessionEnd();
        authService.isAuthenticated.mockReturnValue(false);
        sessionEvents.notifySessionEnded();
        configuration$.next({ enabled: true, publicKey: 'public-key' });

        await expect(enabling).resolves.toBe('unavailable');
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });

    it('revokes a permission request result that arrives after logout without registering it', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/late');
        const permissionResult$ = new Subject<PushSubscription>();
        swPush.subscription = of(null);
        swPush.requestSubscription.mockImplementation(async () => {
            const result = await firstValueFrom(permissionResult$);
            swPush.subscription = of(result);
            return result;
        });

        const enabling = service.ensureSubscriptionAsync();
        await vi.advanceTimersByTimeAsync(0);
        expect(swPush.requestSubscription).toHaveBeenCalledTimes(1);

        await sessionEvents.prepareSessionEnd();
        authService.isAuthenticated.mockReturnValue(false);
        sessionEvents.notifySessionEnded();
        permissionResult$.next(subscription);

        await expect(enabling).resolves.toBe('unavailable');
        expect(subscription.unsubscribeMock).toHaveBeenCalledTimes(1);
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(service.isSubscribed()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });
});

describe('PushNotificationService registration cleanup ordering', () => {
    useSessionCleanupTimers();

    it('deletes the server registration only after an in-flight upsert settles', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/current');
        confirmSubscriptionOwner(subscription, 'user-a');
        const upsert$ = new Subject<void>();
        swPush.subscription = of(subscription);
        notificationService.upsertWebPushSubscription.mockReturnValue(upsert$);

        const enabling = service.ensureSubscriptionAsync();
        await vi.advanceTimersByTimeAsync(0);
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalled();

        const cleanup = sessionEvents.prepareSessionEnd();
        await vi.advanceTimersByTimeAsync(0);
        expect(subscription.unsubscribeMock).toHaveBeenCalledTimes(1);
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();

        upsert$.next();
        await cleanup;
        await expect(enabling).resolves.toBe('unavailable');

        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledTimes(1);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint, 'token-a');
        expect(service.isSubscribed()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });

    it('keeps old cleanup bound to its account when another account signs in before it finishes', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/account-a');
        confirmSubscriptionOwner(subscription, 'user-a');
        const upsert$ = new Subject<void>();
        swPush.subscription = of(subscription);
        notificationService.upsertWebPushSubscription.mockReturnValue(upsert$);

        const enabling = service.ensureSubscriptionAsync();
        await vi.advanceTimersByTimeAsync(0);
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledWith(
            expect.objectContaining({ endpoint: subscription.endpoint }),
        );
        const registrationCount = notificationService.upsertWebPushSubscription.mock.calls.length;

        const cleanup = sessionEvents.prepareSessionEnd();
        authService.getUserId.mockReturnValue('user-b');
        authService.getSessionKey.mockReturnValue('token-b');
        sessionEvents.notifyAuthenticated();
        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');
        expect(service.isSubscribed()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();

        upsert$.next();
        await cleanup;
        await expect(enabling).resolves.toBe('unavailable');

        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledTimes(registrationCount);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledTimes(1);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint, 'token-a');
        expect(service.isSubscribed()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
        expect(service.isBusy()).toBe(false);
    });
});

describe('PushNotificationService unauthenticated cleanup and deadlines', () => {
    useSessionCleanupTimers();

    it('preserves a published Bob endpoint while the local tab has guest signals', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/bob-shared');
        authService.isAuthenticated.mockReturnValue(false);
        localStorage.setItem('refreshSession', 'true');
        localStorage.setItem('userId', 'user-b');
        localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
        confirmSubscriptionOwner(subscription, 'user-b');

        subscription$.next(subscription);
        subscriptionChanges$.next({ oldSubscription: null, newSubscription: subscription });
        await vi.advanceTimersByTimeAsync(0);

        expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(service.isSubscribed()).toBe(false);
    });

    it('keeps a startup invalidation after Push is first instantiated from revoking Bob', async () => {
        authService.isAuthenticated.mockReturnValue(false);
        authService.getUserId.mockReturnValue(null);
        authService.getSessionKey.mockReturnValue(null);
        sessionEvents.notifyLocalSessionInvalidated();
        const retainedEvents = sessionEvents;
        TestBed.resetTestingModule();
        const subscription = createPushSubscription('https://push.example.com/subscriptions/bob-after-startup');
        TestBed.configureTestingModule({
            providers: [
                PushNotificationService,
                { provide: SwPush, useValue: { ...swPush, subscription: of(subscription) } },
                { provide: AuthService, useValue: authService },
                { provide: SessionEventsService, useValue: retainedEvents },
                { provide: LocalizationService, useValue: localizationService },
                { provide: NotificationService, useValue: notificationService },
                { provide: Router, useValue: router },
            ],
        });

        const startupService = TestBed.inject(PushNotificationService);
        await vi.advanceTimersByTimeAsync(0);

        expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
        expect(startupService.isSubscribed()).toBe(false);
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
    });

    it('revokes an orphan browser subscription with no published owner without registering it', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/anonymous');
        authService.isAuthenticated.mockReturnValue(false);
        swPush.subscription = of(subscription);

        subscriptionChanges$.next({ oldSubscription: null, newSubscription: subscription });
        await vi.advanceTimersByTimeAsync(0);

        expect(subscription.unsubscribeMock).toHaveBeenCalledTimes(1);
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(service.isSubscribed()).toBe(false);
    });

    it('bounds logout cleanup when browser revocation, registration and server removal stall', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/stalled');
        confirmSubscriptionOwner(subscription, 'user-a');
        subscription.unsubscribeMock.mockReturnValue(firstValueFrom(new Subject<boolean>()));
        swPush.subscription = of(subscription);
        notificationService.upsertWebPushSubscription.mockReturnValue(new Subject<void>());
        notificationService.removeWebPushSubscription.mockReturnValue(new Subject<void>());

        const enabling = service.ensureSubscriptionAsync();
        await vi.advanceTimersByTimeAsync(0);
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalled();

        const cleanup = sessionEvents.prepareSessionEnd();
        await vi.advanceTimersByTimeAsync(cleanupTimeoutMs);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint, 'token-a');
        await vi.advanceTimersByTimeAsync(cleanupTimeoutMs);

        await expect(cleanup).resolves.toBeUndefined();
        await expect(enabling).resolves.toBe('unavailable');
        expect(service.isBusy()).toBe(false);
        expect(service.isSubscribed()).toBe(false);
    });
});

describe('PushNotificationService revoked device permission', () => {
    it('removes a confirmed registration when permission is denied even if the browser still returns the old subscription', async () => {
        const subscription = createPushSubscription('https://push.example.com/denied-existing-device');
        confirmSubscriptionOwner(subscription, 'user-a');
        swPush.subscription = of(subscription);
        const permission = vi.spyOn(TestBed.inject(BrowserNotificationCapabilityService), 'isPermissionDenied').mockReturnValue(true);
        try {
            await expect(service.ensureSubscriptionAsync()).resolves.toBe('blocked');
            expect(subscription.unsubscribeMock).toHaveBeenCalledOnce();
            expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(subscription.endpoint);
            expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
            expect(localStorage.getItem(pushOwnerStorageKey)).toBeNull();
        } finally {
            permission.mockRestore();
        }
    });
});

describe('Push notification reconciliation after browser resume', () => {
    it('reconciles a missing browser subscription when a suspended PWA becomes visible', async () => {
        const subscription = createPushSubscription('https://push.example.com/suspended-device');
        confirmSubscriptionOwner(subscription, 'user-a');
        swPush.subscription = of(subscription);
        await service.ensureSubscriptionAsync();
        expect(service.isSubscribed()).toBe(true);
        swPush.subscription = of(null);
        const visibility = vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('hidden');
        try {
            window.dispatchEvent(new Event('online'));
            await waitForAsyncTasksAsync();
            expect(service.currentSubscriptionEndpoint()).toBe(subscription.endpoint);
            expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();

            visibility.mockReturnValue('visible');
            window.dispatchEvent(new Event('focus'));
            await vi.waitFor(() => {
                expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint);
            });
            await vi.waitFor(() => {
                expect(service.isSubscribed()).toBe(false);
            });
            expect(service.currentSubscriptionEndpoint()).toBeNull();
        } finally {
            visibility.mockRestore();
        }
    });

    it('removes only the current account device registration when the browser has lost its subscription', async () => {
        const mutation = vi.spyOn(TestBed.inject(BrowserWindowService), 'runWithLockAsync');
        const oldSubscription = createPushSubscription('https://push.example.com/revoked-device');
        confirmSubscriptionOwner(oldSubscription, 'user-a');
        subscription$.next(null);

        TestBed.tick();
        await vi.waitFor(() => {
            expect(mutation).toHaveBeenCalled();
        });
        await mutation.mock.results[0].value;

        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(oldSubscription.endpoint);
        expect(localStorage.getItem(pushOwnerStorageKey)).toBeNull();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
    });

    it('preserves a different account registration when no browser subscription is present', async () => {
        const mutation = vi.spyOn(TestBed.inject(BrowserWindowService), 'runWithLockAsync');
        const oldSubscription = createPushSubscription('https://push.example.com/another-account');
        confirmSubscriptionOwner(oldSubscription, 'user-b');
        subscription$.next(null);

        TestBed.tick();
        await vi.waitFor(() => {
            expect(mutation).toHaveBeenCalled();
        });
        await mutation.mock.results[0].value;

        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(localStorage.getItem(pushOwnerStorageKey)).toContain('user-b');
    });

    it('retains the ownership marker when server removal fails so reconciliation can retry', async () => {
        const mutation = vi.spyOn(TestBed.inject(BrowserWindowService), 'runWithLockAsync');
        const oldSubscription = createPushSubscription('https://push.example.com/revoked-retry');
        confirmSubscriptionOwner(oldSubscription, 'user-a');
        notificationService.removeWebPushSubscription.mockReturnValue(throwError(() => new Error('offline')));
        subscription$.next(null);

        TestBed.tick();
        await vi.waitFor(() => {
            expect(mutation).toHaveBeenCalled();
        });
        await expect(mutation.mock.results[0].value).rejects.toThrow('offline');

        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(oldSubscription.endpoint);
        expect(localStorage.getItem(pushOwnerStorageKey)).toContain(oldSubscription.endpoint);
    });
});

describe('PushNotificationService revoked permission session ownership', () => {
    it('does not remove the old account registration after the account changes during browser revocation', async () => {
        const subscription = createPushSubscription('https://push.example.com/revoked-during-account-change');
        const revoked = new Subject<boolean>();
        confirmSubscriptionOwner(subscription, 'user-a');
        swPush.subscription = of(subscription);
        subscription.unsubscribeMock.mockReturnValue(firstValueFrom(revoked));
        const permission = vi.spyOn(TestBed.inject(BrowserNotificationCapabilityService), 'isPermissionDenied').mockReturnValue(true);
        try {
            const pending = service.ensureSubscriptionAsync();
            await vi.waitFor(() => {
                expect(subscription.unsubscribeMock).toHaveBeenCalledOnce();
            });
            authService.getUserId.mockReturnValue('user-b');
            authService.getSessionKey.mockReturnValue('token-b');
            revoked.next(true);

            await pending;
            expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
            expect(localStorage.getItem(pushOwnerStorageKey)).toContain('user-a');
        } finally {
            permission.mockRestore();
        }
    });
});

describe('PushNotificationService notification clicks', () => {
    it('acknowledges the clicked notification before refreshing its unread count', async () => {
        const notificationId = '11111111-1111-1111-1111-111111111111';
        notificationClicks$.next({ notification: { data: { targetUrl: '/fasting', notificationId, userId: 'user-a' } } });

        await vi.waitFor(() => {
            expect(notificationService.markAsRead).toHaveBeenCalledWith(notificationId);
        });
        await vi.waitFor(() => {
            expect(notificationService.fetchUnreadCount).toHaveBeenCalled();
        });
    });

    it('acknowledges a notification carried by closed-PWA navigation', async () => {
        const notificationId = '11111111-1111-1111-1111-111111111111';
        const url = `/fasting?intent=check-in&notificationId=${notificationId}&notificationUserId=user-a`;
        router.events.next(new NavigationEnd(1, url, url));

        await vi.waitFor(() => {
            expect(notificationService.markAsRead).toHaveBeenCalledWith(notificationId);
        });
    });

    it('should react to notification click by navigating and refreshing notifications', () => {
        notificationClicks$.next({ notification: { data: { targetUrl: '/fasting?intent=check-in' } } });

        expect(router.navigateByUrl).toHaveBeenCalledWith('/fasting?intent=check-in');
        expect(notificationService.fetchUnreadCount).toHaveBeenCalledTimes(1);
        expect(notificationService.notifyNotificationsChanged).toHaveBeenCalledTimes(1);
    });

    it('opens a notification carrying the legacy route field and updates the unread indicator', () => {
        notificationClicks$.next({ notification: { data: { url: '/fasting?intent=check-in' } } });

        expect(router.navigateByUrl).toHaveBeenCalledExactlyOnceWith('/fasting?intent=check-in');
        expect(notificationService.fetchUnreadCount).toHaveBeenCalledExactlyOnceWith({ force: true });
    });

    it('refreshes the inbox without changing pages when a notification has no destination', () => {
        notificationClicks$.next({ notification: {} });

        expect(router.navigateByUrl).not.toHaveBeenCalled();
        expect(notificationService.fetchUnreadCount).toHaveBeenCalledExactlyOnceWith({ force: true });
        expect(notificationService.notifyNotificationsChanged).toHaveBeenCalledOnce();
    });
});

describe('PushNotificationService browser changes during enable', () => {
    it('uses the existing endpoint when another tab enables it while configuration is loading', async () => {
        const configuration = new Subject<{ enabled: boolean; publicKey: string }>();
        notificationService.getWebPushConfiguration.mockReturnValue(configuration);
        swPush.subscription = of(null);

        const enabling = service.ensureSubscriptionAsync();
        await vi.waitFor(() => {
            expect(notificationService.getWebPushConfiguration).toHaveBeenCalledOnce();
        });
        const subscription = createPushSubscription('https://push.example.com/subscriptions/enabled-in-another-tab');
        confirmSubscriptionOwner(subscription, 'user-a');
        swPush.subscription = of(subscription);
        configuration.next({ enabled: true, publicKey: 'public-key' });

        await expect(enabling).resolves.toBe('subscribed');
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ endpoint: subscription.endpoint }),
        );
        expect(service.currentSubscriptionEndpoint()).toBe(subscription.endpoint);
    });

    it('preserves a newer browser endpoint when the old registration completes late', async () => {
        const registration = new Subject<void>();
        notificationService.upsertWebPushSubscription.mockReturnValue(registration);
        const original = createPushSubscription('https://push.example.com/subscriptions/registering');
        const latest = createPushSubscription('https://push.example.com/subscriptions/newer-registration');
        swPush.subscription = of(null);
        resolveRequestedSubscription(original);

        const enabling = service.ensureSubscriptionAsync();
        await vi.waitFor(() => {
            expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledExactlyOnceWith(
                expect.objectContaining({ endpoint: original.endpoint }),
            );
        });
        confirmSubscriptionOwner(latest, 'user-a');
        swPush.subscription = of(latest);
        registration.next();

        await expect(enabling).resolves.toBe('unavailable');
        expect(service.currentSubscriptionEndpoint()).toBeNull();
        expect(latest.unsubscribeMock).not.toHaveBeenCalled();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: latest.endpoint, userId: 'user-a' }));
    });
});

describe('Push notification registration while the browser endpoint changes', () => {
    useSessionCleanupTimers();

    it('rolls back an old account registration after another tab publishes its own endpoint', async () => {
        swPush.subscription = of(null);
        subscription$.next(null);
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(0);

        const former = createPushSubscription('https://push.example.com/subscriptions/registering-a');
        const registration = new Subject<void>();
        resolveRequestedSubscription(former);
        notificationService.upsertWebPushSubscription.mockReturnValueOnce(registration);
        const enabling = service.ensureSubscriptionAsync();
        await vi.advanceTimersByTimeAsync(0);
        expect(registration.observed).toBe(true);
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ endpoint: former.endpoint }),
        );

        const replacement = createPushSubscription('https://push.example.com/subscriptions/current-b');
        localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
        confirmSubscriptionOwner(replacement, 'user-b');
        swPush.subscription = of(replacement);
        registration.next();

        await expect(enabling).resolves.toBe('unavailable');
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(former.endpoint, 'token-a');
        expect(former.unsubscribeMock).not.toHaveBeenCalled();
        expect(replacement.unsubscribeMock).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBeNull();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: replacement.endpoint, userId: 'user-b' }));
    });

    it('replaces an unconfirmed endpoint that appears before the push key arrives', async () => {
        swPush.subscription = of(null);
        subscription$.next(null);
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(0);

        const configuration = new Subject<{ enabled: boolean; publicKey: string }>();
        notificationService.getWebPushConfiguration.mockReturnValueOnce(configuration);
        const enabling = service.ensureSubscriptionAsync();
        await vi.advanceTimersByTimeAsync(0);
        expect(configuration.observed).toBe(true);
        expect(swPush.requestSubscription).not.toHaveBeenCalled();

        const unconfirmed = createPushSubscription('https://push.example.com/subscriptions/not-yet-confirmed');
        swPush.subscription = of(unconfirmed);
        const replacement = createPushSubscription('https://push.example.com/subscriptions/confirmed-a');
        resolveRequestedSubscription(replacement);
        configuration.next({ enabled: true, publicKey: 'public-key' });

        await expect(enabling).resolves.toBe('subscribed');
        expect(unconfirmed.unsubscribeMock).toHaveBeenCalledOnce();
        expect(replacement.unsubscribeMock).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).toHaveBeenCalledExactlyOnceWith({ serverPublicKey: 'public-key' });
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ endpoint: replacement.endpoint }),
        );
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBe(replacement.endpoint);
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: replacement.endpoint, userId: 'user-a' }));
    });
});

describe('Push notification cleanup after another account is published', () => {
    useSessionCleanupTimers();

    it('retains the original owner marker when a worker renewal finishes its server deletion late', async () => {
        const previous = createPushSubscription('https://push.example.com/subscriptions/previous-worker-device');
        const renewed = createPushSubscription('https://push.example.com/subscriptions/queued-worker-renewal');
        confirmSubscriptionOwner(previous, 'user-a');
        swPush.subscription = of(previous);
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(0);
        expect(service.currentSubscriptionEndpoint()).toBe(previous.endpoint);
        notificationService.upsertWebPushSubscription.mockClear();

        const deletion = new Subject<void>();
        notificationService.removeWebPushSubscription.mockReturnValueOnce(deletion);
        swPush.subscription = of(renewed);
        subscriptionChanges$.next({ oldSubscription: previous, newSubscription: renewed });
        await vi.advanceTimersByTimeAsync(0);
        expect(deletion.observed).toBe(true);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(previous.endpoint);
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();

        localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
        deletion.next();
        await vi.advanceTimersByTimeAsync(0);

        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: previous.endpoint, userId: 'user-a' }));
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledOnce();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(renewed.unsubscribeMock).not.toHaveBeenCalled();
    });

    it('ends an owner-conflict recovery after late browser revocation without prompting for the old session', async () => {
        swPush.subscription = of(null);
        subscription$.next(null);
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(0);

        const rejected = createPushSubscription('https://push.example.com/subscriptions/rejected-previous-device');
        const revocation = new Subject<boolean>();
        rejected.unsubscribeMock.mockReturnValueOnce(firstValueFrom(revocation));
        swPush.subscription = of(rejected);
        confirmSubscriptionOwner(rejected, 'user-a');
        notificationService.upsertWebPushSubscription.mockReturnValueOnce(throwError(ownerConflict));

        const enabling = service.ensureSubscriptionAsync();
        await vi.advanceTimersByTimeAsync(0);
        expect(revocation.observed).toBe(true);
        expect(rejected.unsubscribeMock).toHaveBeenCalledOnce();
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ endpoint: rejected.endpoint }),
        );

        const replacement = createPushSubscription('https://push.example.com/subscriptions/confirmed-new-account');
        localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
        confirmSubscriptionOwner(replacement, 'user-b');
        swPush.subscription = of(replacement);
        revocation.next(true);

        await expect(enabling).resolves.toBe('unavailable');
        expect(replacement.unsubscribeMock).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledOnce();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: replacement.endpoint, userId: 'user-b' }));
    });
});

describe('PushNotificationService account switches during worker discovery', () => {
    it.each(['manual enable', 'focus resume'] as const)('ignores a worker result for the former account during %s', async trigger => {
        vi.spyOn(TestBed.inject(BrowserNotificationCapabilityService), 'getPermission').mockReturnValue('granted');
        swPush.subscription = of(null);
        await finishInitialPushReadAsync();
        localStorage.setItem('webPushDeviceOptIn:user-a', 'true');

        const readFinished = new Subject<void>();
        const releaseLock = new Subject<void>();
        vi.spyOn(TestBed.inject(BrowserWindowService), 'runWithLockAsync').mockImplementationOnce(
            async <T>(_name: string, operation: () => Promise<T>): Promise<T> => {
                const result = await operation();
                readFinished.next();
                await firstValueFrom(releaseLock);
                return result;
            },
        );
        const browserSnapshot = firstValueFrom(readFinished);

        const enabling = trigger === 'manual enable' ? service.ensureSubscriptionAsync() : null;
        if (trigger === 'focus resume') {
            window.dispatchEvent(new Event('focus'));
        }
        await browserSnapshot;

        const replacement = createPushSubscription('https://push.example.com/subscriptions/active-b');
        confirmSubscriptionOwner(replacement, 'user-b');
        localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
        swPush.subscription = of(replacement);
        releaseLock.next();

        if (enabling !== null) {
            await expect(enabling).resolves.toBe('unavailable');
        } else {
            await nextBrowserTaskAsync();
        }
        expect(notificationService.getNotificationPreferences).not.toHaveBeenCalled();
        expect(notificationService.getWebPushConfiguration).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(replacement.unsubscribeMock).not.toHaveBeenCalled();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: replacement.endpoint, userId: 'user-b' }));
    });
});

describe('PushNotificationService account switch during key rotation', () => {
    it('stops rotating a former account endpoint after its pending server deletion finishes', async () => {
        swPush.subscription = of(null);
        await finishInitialPushReadAsync();

        const previous = createPushSubscription(
            'https://push.example.com/subscriptions/obsolete-a',
            new TextEncoder().encode('old').buffer,
        );
        const deletion = new Subject<void>();
        confirmSubscriptionOwner(previous, 'user-a');
        swPush.subscription = of(previous);
        notificationService.getWebPushConfiguration.mockReturnValue(of({ enabled: true, publicKey: 'bmV3' }));
        notificationService.removeWebPushSubscription.mockReturnValueOnce(deletion);

        const enabling = service.ensureSubscriptionAsync();
        await vi.waitFor(() => {
            expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(previous.endpoint);
            expect(deletion.observed).toBe(true);
        });
        const replacement = createPushSubscription('https://push.example.com/subscriptions/replacement-b');
        confirmSubscriptionOwner(replacement, 'user-b');
        localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
        swPush.subscription = of(replacement);
        deletion.next();

        await expect(enabling).resolves.toBe('unavailable');
        expect(previous.unsubscribeMock).toHaveBeenCalledOnce();
        expect(replacement.unsubscribeMock).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: replacement.endpoint, userId: 'user-b' }));
    });
});

async function finishInitialPushReadAsync(): Promise<void> {
    TestBed.tick();
    await nextBrowserTaskAsync();
}

async function nextBrowserTaskAsync(): Promise<void> {
    await new Promise<void>(resolve => {
        setTimeout(resolve, 0);
    });
}

describe('PushNotificationService browser refusal to replace a subscription', () => {
    it('leaves a different account endpoint intact when the initial browser check cannot remove it', async () => {
        const foreign = createPushSubscription('https://push.example.com/subscriptions/foreign-device');
        confirmSubscriptionOwner(foreign, 'user-b');
        foreign.unsubscribeMock.mockResolvedValue(false);
        swPush.subscription = of(foreign);

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');

        expect(foreign.unsubscribeMock).toHaveBeenCalledOnce();
        expect(notificationService.getWebPushConfiguration).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: foreign.endpoint, userId: 'user-b' }));
        expect(service.isBusy()).toBe(false);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });

    it('avoids registering a different account endpoint that appears while the key is loading', async () => {
        const configuration = new Subject<{ enabled: boolean; publicKey: string }>();
        notificationService.getWebPushConfiguration.mockReturnValueOnce(configuration);
        swPush.subscription = of(null);
        const enabling = service.ensureSubscriptionAsync();
        await vi.waitFor(() => {
            expect(configuration.observed).toBe(true);
        });

        const foreign = createPushSubscription('https://push.example.com/subscriptions/new-foreign-device');
        confirmSubscriptionOwner(foreign, 'user-b');
        foreign.unsubscribeMock.mockResolvedValue(false);
        swPush.subscription = of(foreign);
        configuration.next({ enabled: true, publicKey: 'public-key' });
        configuration.complete();

        await expect(enabling).resolves.toBe('unavailable');
        expect(foreign.unsubscribeMock).toHaveBeenCalledOnce();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: foreign.endpoint, userId: 'user-b' }));
        expect(service.isBusy()).toBe(false);
    });

    it('ignores an old device key response after another account publishes a new subscription', async () => {
        const previous = createPushSubscription('https://push.example.com/subscriptions/old-device');
        const current = createPushSubscription('https://push.example.com/subscriptions/new-device');
        confirmSubscriptionOwner(previous, 'user-a');
        swPush.subscription = of(previous);
        const configuration = new Subject<{ enabled: boolean; publicKey: string }>();
        notificationService.getWebPushConfiguration.mockReturnValue(configuration);
        const enabling = service.ensureSubscriptionAsync();
        await vi.waitFor(() => {
            expect(configuration.observed).toBe(true);
        });

        localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['user-b', 'User']));
        confirmSubscriptionOwner(current, 'user-b');
        swPush.subscription = of(current);
        configuration.next({ enabled: true, publicKey: 'replacement-key' });
        configuration.complete();

        await expect(enabling).resolves.toBe('unavailable');
        expect(previous.unsubscribeMock).not.toHaveBeenCalled();
        expect(current.unsubscribeMock).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: current.endpoint, userId: 'user-b' }));
    });
});

describe('PushNotificationService actions when signed out', () => {
    it('ignores both an expired endpoint without replacement and a queued remove-device action', async () => {
        const expired = createPushSubscription('https://push.example.com/subscriptions/expired-while-signed-out');
        confirmSubscriptionOwner(expired, 'user-a');
        authService.isAuthenticated.mockReturnValue(false);
        authService.getUserId.mockReturnValue(null);
        authService.getSessionKey.mockReturnValue(null);
        swPush.subscription = of(null);

        subscriptionChanges$.next({ oldSubscription: expired, newSubscription: null });
        await expect(service.removeSubscriptionAsync(expired.endpoint)).resolves.toBe(false);
        await waitForAsyncTasksAsync();

        expect(expired.unsubscribeMock).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });
});

describe('PushNotificationService same-account return', () => {
    it('does not prompt when permission changes while automatic restoration is waiting for configuration', async () => {
        const permission = vi.spyOn(TestBed.inject(BrowserNotificationCapabilityService), 'getPermission').mockReturnValue('granted');
        localStorage.setItem('webPushDeviceOptIn:user-a', 'true');
        const configuration = new Subject<{ enabled: boolean; publicKey: string }>();
        notificationService.getWebPushConfiguration.mockReturnValue(configuration);
        swPush.subscription = of(null);
        window.dispatchEvent(new Event('focus'));
        await vi.waitFor(() => {
            expect(notificationService.getWebPushConfiguration).toHaveBeenCalled();
        });
        permission.mockReturnValue('default');
        configuration.next({ enabled: true, publicKey: 'public-key' });
        await vi.waitFor(() => {
            expect(service.isBusy()).toBe(false);
        });
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
    });

    it.each(['default', 'denied', 'unsupported'] as const)(
        'does not request a new subscription when browser permission is %s',
        async permission => {
            vi.spyOn(TestBed.inject(BrowserNotificationCapabilityService), 'getPermission').mockReturnValue(permission);
            localStorage.setItem('webPushDeviceOptIn:user-a', 'true');
            swPush.subscription = of(null);
            window.dispatchEvent(new Event('focus'));
            await new Promise<void>(resolve => setTimeout(resolve, subscriptionSettleMs));
            expect(swPush.requestSubscription).not.toHaveBeenCalled();
        },
    );

    it('does not inherit a different account device opt-in', async () => {
        vi.spyOn(TestBed.inject(BrowserNotificationCapabilityService), 'getPermission').mockReturnValue('granted');
        localStorage.setItem('webPushDeviceOptIn:user-b', 'true');
        swPush.subscription = of(null);
        window.dispatchEvent(new Event('focus'));
        await new Promise<void>(resolve => setTimeout(resolve, subscriptionSettleMs));
        expect(notificationService.getNotificationPreferences).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
    });
});

describe('PushNotificationService automatic restoration ownership', () => {
    it('ignores the earlier preference response while the new account restores push', async () => {
        vi.spyOn(TestBed.inject(BrowserNotificationCapabilityService), 'getPermission').mockReturnValue('granted');
        swPush.subscription = of(null);
        TestBed.tick();
        await waitForAsyncTasksAsync();

        const oldPreferences = new Subject<{ pushNotificationsEnabled: boolean }>();
        const newPreferences = new Subject<{ pushNotificationsEnabled: boolean }>();
        notificationService.getNotificationPreferences.mockReturnValueOnce(oldPreferences).mockReturnValueOnce(newPreferences);
        localStorage.setItem('webPushDeviceOptIn:user-a', 'true');
        localStorage.setItem('webPushDeviceOptIn:user-b', 'true');
        window.dispatchEvent(new Event('focus'));
        await vi.waitFor(() => {
            expect(oldPreferences.observed).toBe(true);
            expect(service.isBusy()).toBe(true);
        });

        authService.getUserId.mockReturnValue('user-b');
        authService.getSessionKey.mockReturnValue('token-b');
        sessionEvents.notifyAuthenticated();
        await vi.waitFor(() => {
            expect(newPreferences.observed).toBe(true);
            expect(service.isBusy()).toBe(true);
        });
        oldPreferences.next({ pushNotificationsEnabled: true });
        await waitForAsyncTasksAsync();

        expect(service.isBusy()).toBe(true);
        expect(service.isSubscribed()).toBe(false);
        expect(notificationService.getWebPushConfiguration).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();

        const subscription = createPushSubscription('https://push.example.com/subscriptions/returned-b');
        resolveRequestedSubscription(subscription);
        newPreferences.next({ pushNotificationsEnabled: true });
        await vi.waitFor(() => {
            expect(service.currentSubscriptionEndpoint()).toBe(subscription.endpoint);
            expect(service.isBusy()).toBe(false);
        });
        expect(swPush.requestSubscription).toHaveBeenCalledOnce();
        expect(localStorage.getItem(pushOwnerStorageKey)).toBe(JSON.stringify({ endpoint: subscription.endpoint, userId: 'user-b' }));
    });
});

describe('PushNotificationService remembered device choices', () => {
    it('keeps an explicitly disconnected device disabled after another sign-in', async () => {
        vi.spyOn(TestBed.inject(BrowserNotificationCapabilityService), 'getPermission').mockReturnValue('granted');
        const subscription = createPushSubscription('https://push.example.com/subscriptions/disabled');
        confirmSubscriptionOwner(subscription, 'user-a');
        swPush.subscription = of(subscription);
        await service.ensureSubscriptionAsync();
        await expect(service.removeSubscriptionAsync(subscription.endpoint)).resolves.toBe(true);
        sessionEvents.notifyAuthenticated();
        window.dispatchEvent(new Event('focus'));
        await new Promise<void>(resolve => setTimeout(resolve, subscriptionSettleMs));
        expect(localStorage.getItem('webPushDeviceOptIn:user-a')).toBe('false');
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
    });

    it('does not restore a device when account-wide push is disabled', async () => {
        vi.spyOn(TestBed.inject(BrowserNotificationCapabilityService), 'getPermission').mockReturnValue('granted');
        localStorage.setItem('webPushDeviceOptIn:user-a', 'true');
        notificationService.getNotificationPreferences.mockReturnValue(of({ pushNotificationsEnabled: false }));
        swPush.subscription = of(null);
        window.dispatchEvent(new Event('focus'));
        await vi.waitFor(() => {
            expect(notificationService.getNotificationPreferences).toHaveBeenCalled();
        });
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
    });

    it('restores a previously enabled device after logout and same-account sign-in without another permission prompt', async () => {
        vi.spyOn(TestBed.inject(BrowserNotificationCapabilityService), 'getPermission').mockReturnValue('granted');
        const previous = createPushSubscription('https://push.example.com/subscriptions/previous');
        const replacement = createPushSubscription('https://push.example.com/subscriptions/returned');
        confirmSubscriptionOwner(previous, 'user-a');
        swPush.subscription = of(previous);
        subscription$.next(previous);
        await service.ensureSubscriptionAsync();

        await sessionEvents.prepareSessionEnd();
        authService.isAuthenticated.mockReturnValue(false);
        sessionEvents.notifySessionEnded();
        expect(previous.unsubscribeMock).toHaveBeenCalledOnce();
        resolveRequestedSubscription(replacement);
        authService.isAuthenticated.mockReturnValue(true);
        authService.getSessionKey.mockReturnValue('new-token-a');
        sessionEvents.notifyAuthenticated();
        window.dispatchEvent(new Event('focus'));

        await vi.waitFor(() => {
            expect(service.isSubscribed()).toBe(true);
        });
        expect(service.currentSubscriptionEndpoint()).toBe(replacement.endpoint);
        expect(notificationService.getNotificationPreferences).toHaveBeenCalled();
    });
});
