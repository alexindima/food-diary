import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
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
import { NotificationService, type WebPushSubscriptionRequest } from './notification.service';
import { PushNotificationService } from './push-notification.service';

let service: PushNotificationService;
let sessionEvents: SessionEventsService;
let subscription$: Subject<PushSubscription | null>;
let subscriptionChanges$: Subject<{ oldSubscription: PushSubscription | null; newSubscription: PushSubscription | null }>;
let notificationClicks$: Subject<{ notification: { data?: { targetUrl?: string; url?: string } } }>;
let swPush: {
    isEnabled: boolean;
    subscription: Observable<PushSubscription | null>;
    pushSubscriptionChanges: Subject<{ oldSubscription: PushSubscription | null; newSubscription: PushSubscription | null }>;
    notificationClicks: Subject<{ notification: { data?: { url?: string } } }>;
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
};
let router: { navigateByUrl: ReturnType<typeof vi.fn> };
const pushOwnerStorageKey = 'webPushSubscriptionOwner';
const cleanupTimeoutMs = 5000;

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    // SwPush re-reads PushManager.getSubscription for every new subscriber.
    // Replay only a discovered value; initial discovery can still be delayed.
    subscription$ = new ReplaySubject<PushSubscription | null>(1);
    subscriptionChanges$ = new Subject<{ oldSubscription: PushSubscription | null; newSubscription: PushSubscription | null }>();
    notificationClicks$ = new Subject<{ notification: { data?: { targetUrl?: string; url?: string } } }>();

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
    };
    router = {
        navigateByUrl: vi.fn().mockResolvedValue(true),
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
        expirationTime: null,
        unsubscribe: unsubscribeMock,
        unsubscribeMock,
        toJSON: () => ({
            endpoint,
            expirationTime: null,
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
    it('preserves the real browser endpoint and marker during token-only impersonation', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/real-bob');
        confirmSubscriptionOwner(subscription, 'real-bob');
        authService.isImpersonating.mockReturnValue(true);
        authService.getUserId.mockReturnValue('impersonated-alice');
        authService.getSessionKey.mockReturnValue('impersonation-token');
        swPush.subscription = of(subscription);
        subscription$.next(subscription);
        sessionEvents.notifyAuthenticated();
        subscriptionChanges$.next({ oldSubscription: subscription, newSubscription: subscription });
        await waitForAsyncTasksAsync();

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('unavailable');
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

describe('PushNotificationService notification clicks', () => {
    it('should react to notification click by navigating and refreshing notifications', () => {
        notificationClicks$.next({ notification: { data: { targetUrl: '/fasting?intent=check-in' } } });

        expect(router.navigateByUrl).toHaveBeenCalledWith('/fasting?intent=check-in');
        expect(notificationService.fetchUnreadCount).toHaveBeenCalledTimes(1);
        expect(notificationService.notifyNotificationsChanged).toHaveBeenCalledTimes(1);
    });
});
