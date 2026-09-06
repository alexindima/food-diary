import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { SwPush } from '@angular/service-worker';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../services/auth.service';
import { LocalizationService } from '../i18n/localization.service';
import { NotificationService } from './notification.service';
import { PushNotificationService } from './push-notification.service';

let service: PushNotificationService;
let subscription$: Subject<PushSubscription | null>;
let subscriptionChanges$: Subject<{ oldSubscription: PushSubscription | null; newSubscription: PushSubscription | null }>;
let notificationClicks$: Subject<{ notification: { data?: { targetUrl?: string; url?: string } } }>;
let swPush: {
    isEnabled: boolean;
    subscription: Subject<PushSubscription | null>;
    pushSubscriptionChanges: Subject<{ oldSubscription: PushSubscription | null; newSubscription: PushSubscription | null }>;
    notificationClicks: Subject<{ notification: { data?: { url?: string } } }>;
    requestSubscription: ReturnType<typeof vi.fn>;
};
let authService: { isAuthenticated: ReturnType<typeof vi.fn> };
let localizationService: { getCurrentLanguage: ReturnType<typeof vi.fn> };
let notificationService: {
    getWebPushConfiguration: ReturnType<typeof vi.fn>;
    upsertWebPushSubscription: ReturnType<typeof vi.fn>;
    removeWebPushSubscription: ReturnType<typeof vi.fn>;
    fetchUnreadCount: ReturnType<typeof vi.fn>;
    notifyNotificationsChanged: ReturnType<typeof vi.fn>;
};
let router: { navigateByUrl: ReturnType<typeof vi.fn> };

beforeEach(() => {
    subscription$ = new Subject<PushSubscription | null>();
    subscriptionChanges$ = new Subject<{ oldSubscription: PushSubscription | null; newSubscription: PushSubscription | null }>();
    notificationClicks$ = new Subject<{ notification: { data?: { targetUrl?: string; url?: string } } }>();

    swPush = {
        isEnabled: true,
        subscription: subscription$,
        pushSubscriptionChanges: subscriptionChanges$,
        notificationClicks: notificationClicks$,
        requestSubscription: vi.fn(),
    };
    authService = {
        isAuthenticated: vi.fn(() => true),
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
});

function createPushSubscription(
    endpoint: string,
    applicationServerKey: ArrayBuffer | null = null,
): PushSubscription & { unsubscribe: ReturnType<typeof vi.fn>; unsubscribeMock: ReturnType<typeof vi.fn> } {
    const options: PushSubscriptionOptions = {
        applicationServerKey,
        userVisibleOnly: true,
    };
    const unsubscribeMock = vi.fn().mockResolvedValue(true);

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

describe('PushNotificationService subscription support', () => {
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
            ],
        });

        const disabledService = TestBed.inject(PushNotificationService);
        await expect(disabledService.ensureSubscriptionAsync()).resolves.toBe('unsupported');
    });
});

describe('PushNotificationService subscription lifecycle', () => {
    it('should upsert existing subscription and return already-subscribed', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/current');
        swPush.subscription = of(subscription) as never;

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
        notificationService.getWebPushConfiguration.mockReturnValue(of({ enabled: true, publicKey: 'b2xk' }));
        swPush.subscription = of(subscription) as never;

        const result = await service.ensureSubscriptionAsync();

        expect(result).toBe('already-subscribed');
        expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(swPush.requestSubscription).not.toHaveBeenCalled();
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledWith(
            expect.objectContaining({ endpoint: subscription.endpoint }),
        );
    });

    it('should replace an existing subscription created with a different public key', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/stale', new TextEncoder().encode('old').buffer);
        const replacement = createPushSubscription(
            'https://push.example.com/subscriptions/replacement',
            new TextEncoder().encode('new').buffer,
        );
        notificationService.getWebPushConfiguration.mockReturnValue(of({ enabled: true, publicKey: 'bmV3' }));
        swPush.subscription = of(subscription) as never;
        swPush.requestSubscription.mockResolvedValue(replacement);

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
    it('should retain the existing subscription when configuration cannot be loaded', async () => {
        const subscription = createPushSubscription(
            'https://push.example.com/subscriptions/current',
            new TextEncoder().encode('old').buffer,
        );
        swPush.subscription = of(subscription) as never;
        notificationService.getWebPushConfiguration.mockReturnValue(throwError(() => new Error('Configuration unavailable')));

        await expect(service.ensureSubscriptionAsync()).resolves.toBe('already-subscribed');

        expect(subscription.unsubscribeMock).not.toHaveBeenCalled();
        expect(notificationService.removeWebPushSubscription).not.toHaveBeenCalled();
        expect(service.currentSubscriptionEndpoint()).toBe(subscription.endpoint);
    });

    it('should clear stale state when requesting a replacement subscription fails', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/stale', new TextEncoder().encode('old').buffer);
        subscription$.next(subscription);
        swPush.subscription = of(subscription) as never;
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
        swPush.subscription = of(null) as never;
        swPush.requestSubscription.mockResolvedValue(subscription);

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
        swPush.subscription = of(subscription) as never;

        const removed = await service.removeSubscriptionAsync(subscription.endpoint);

        expect(removed).toBe(true);
        expect(subscription.unsubscribeMock).toHaveBeenCalledTimes(1);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
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
