import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { SwPush } from '@angular/service-worker';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from './auth.service';
import { LocalizationService } from './localization.service';
import { NotificationService } from './notification.service';
import { PushNotificationService } from './push-notification.service';

describe('PushNotificationService', () => {
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
            upsertWebPushSubscription: vi.fn(() => of(undefined)),
            removeWebPushSubscription: vi.fn(() => of(undefined)),
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
        await expect(disabledService.ensureSubscription()).resolves.toBe('unsupported');
    });

    it('should upsert existing subscription and return already-subscribed', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/current');
        swPush.subscription = of(subscription) as never;

        const result = await service.ensureSubscription();

        expect(result).toBe('already-subscribed');
        expect(notificationService.upsertWebPushSubscription).toHaveBeenCalledWith(
            expect.objectContaining({
                endpoint: subscription.endpoint,
                locale: 'en',
            }),
        );
        expect(service.currentSubscriptionEndpoint()).toBe(subscription.endpoint);
    });

    it('should request and persist a new subscription', async () => {
        const subscription = createPushSubscription('https://push.example.com/subscriptions/new');
        swPush.subscription = of(null) as never;
        swPush.requestSubscription.mockResolvedValue(subscription);

        const result = await service.ensureSubscription();

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

        const removed = await service.removeSubscription(subscription.endpoint);

        expect(removed).toBe(true);
        expect(subscription.unsubscribe).toHaveBeenCalledTimes(1);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith(subscription.endpoint);
        expect(service.currentSubscriptionEndpoint()).toBeNull();
    });

    it('should react to notification click by navigating and refreshing notifications', async () => {
        notificationClicks$.next({ notification: { data: { targetUrl: '/fasting?intent=check-in' } } });

        expect(router.navigateByUrl).toHaveBeenCalledWith('/fasting?intent=check-in');
        expect(notificationService.fetchUnreadCount).toHaveBeenCalledTimes(1);
        expect(notificationService.notifyNotificationsChanged).toHaveBeenCalledTimes(1);
    });

    function createPushSubscription(endpoint: string): PushSubscription & { unsubscribe: ReturnType<typeof vi.fn> } {
        const options: PushSubscriptionOptions = {
            applicationServerKey: null,
            userVisibleOnly: true,
        };

        return {
            endpoint,
            expirationTime: null,
            unsubscribe: vi.fn().mockResolvedValue(true),
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
        } as unknown as PushSubscription & { unsubscribe: ReturnType<typeof vi.fn> };
    }
});
