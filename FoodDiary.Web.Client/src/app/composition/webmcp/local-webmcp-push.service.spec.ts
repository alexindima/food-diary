import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { NotificationService } from '../../shared/notifications/notification.service';
import { LocalWebmcpPushService } from './local-webmcp-push.service';

describe('read-only push diagnostics', () => {
    afterEach(() => vi.unstubAllGlobals());
    it('returns nullable expiration and capability metadata without subscription endpoints or keys', async () => {
        const getSubscription = vi.fn().mockResolvedValue({ endpoint: 'private-endpoint', expirationTime: null, keys: 'private-key' });
        const getRegistration = vi.fn().mockResolvedValue({ active: { state: 'activated' }, pushManager: { getSubscription } });
        const previous = Object.getOwnPropertyDescriptor(navigator, 'serviceWorker');
        Object.defineProperty(navigator, 'serviceWorker', { configurable: true, value: { controller: {}, getRegistration } });
        vi.stubGlobal('Notification', { permission: 'granted' });
        try {
            TestBed.configureTestingModule({
                providers: [
                    {
                        provide: NotificationService,
                        useValue: {
                            getWebPushConfiguration: vi.fn().mockReturnValue(of({ enabled: true, publicKey: 'private-provider-key' })),
                            getWebPushSubscriptions: vi
                                .fn()
                                .mockReturnValue(
                                    of([{ endpoint: 'private-server-endpoint', expirationTimeUtc: null, userAgent: 'private-agent' }]),
                                ),
                        },
                    },
                ],
            });
            const result = await TestBed.inject(LocalWebmcpPushService).readAsync();
            expect(result).toMatchObject({
                notificationPermission: 'granted',
                providerAvailable: true,
                serviceWorker: { controlled: true, active: 'activated' },
                browserSubscription: { presence: 'present', expirationTime: null },
                serverDevices: { count: 1, expirations: [null] },
                delivery: 'not-observed',
            });
            expect(JSON.stringify(result)).not.toContain('private-');
        } finally {
            if (previous === undefined) {
                Reflect.deleteProperty(navigator, 'serviceWorker');
            } else {
                Object.defineProperty(navigator, 'serviceWorker', previous);
            }
        }
    });
    it('leaves failed server observations unknown', async () => {
        TestBed.configureTestingModule({
            providers: [
                {
                    provide: NotificationService,
                    useValue: {
                        getWebPushConfiguration: vi.fn().mockReturnValue(throwError(() => new Error('private-error'))),
                        getWebPushSubscriptions: vi.fn().mockReturnValue(throwError(() => new Error('private-error'))),
                    },
                },
            ],
        });
        const result = await TestBed.inject(LocalWebmcpPushService).readAsync();
        expect(result).toMatchObject({ providerAvailable: null, serverDevices: null, delivery: 'not-observed' });
        expect(JSON.stringify(result)).not.toContain('private-');
    });
});

describe('read-only push diagnostics with partial browser support', () => {
    it.each([
        {
            situation: 'no registration',
            getRegistration: vi.fn().mockResolvedValue(undefined),
            registration: 'absent',
            presence: 'absent',
        },
        { situation: 'no PushManager', getRegistration: vi.fn().mockResolvedValue({}), registration: 'present', presence: 'unknown' },
        {
            situation: 'browser refused a subscription read',
            getRegistration: vi
                .fn()
                .mockResolvedValue({ pushManager: { getSubscription: vi.fn().mockRejectedValue(new Error('unavailable')) } }),
            registration: 'present',
            presence: 'unknown',
        },
        {
            situation: 'browser refused a registration read',
            getRegistration: vi.fn().mockRejectedValue(new Error('unavailable')),
            registration: 'unknown',
            presence: 'unknown',
        },
    ])('keeps the observed browser state distinct for $situation', async ({ getRegistration, registration, presence }) => {
        const previous = Object.getOwnPropertyDescriptor(navigator, 'serviceWorker');
        Object.defineProperty(navigator, 'serviceWorker', { configurable: true, value: { controller: null, getRegistration } });
        try {
            TestBed.configureTestingModule({
                providers: [
                    {
                        provide: NotificationService,
                        useValue: {
                            getWebPushConfiguration: vi.fn().mockReturnValue(of({ enabled: false })),
                            getWebPushSubscriptions: vi.fn().mockReturnValue(of([])),
                        },
                    },
                ],
            });
            expect(await TestBed.inject(LocalWebmcpPushService).readAsync()).toMatchObject({
                providerAvailable: false,
                serviceWorker: { supported: true, controlled: false, registration, active: null },
                browserSubscription: { presence, expirationTime: null },
                serverDevices: { count: 0, expirations: [] },
                delivery: 'not-observed',
            });
        } finally {
            if (previous === undefined) {
                Reflect.deleteProperty(navigator, 'serviceWorker');
            } else {
                Object.defineProperty(navigator, 'serviceWorker', previous);
            }
        }
    });
});

describe('read-only push diagnostics with blocked worker access', () => {
    it('keeps server observations when browser access to the worker is blocked', async () => {
        const previous = Object.getOwnPropertyDescriptor(navigator, 'serviceWorker');
        Object.defineProperty(navigator, 'serviceWorker', {
            configurable: true,
            get: (): never => {
                throw new DOMException('Worker access was blocked', 'SecurityError');
            },
        });
        try {
            TestBed.configureTestingModule({
                providers: [
                    {
                        provide: NotificationService,
                        useValue: {
                            getWebPushConfiguration: vi.fn().mockReturnValue(of({ enabled: true })),
                            getWebPushSubscriptions: vi.fn().mockReturnValue(of([])),
                        },
                    },
                ],
            });
            const observation = await TestBed.inject(LocalWebmcpPushService).readAsync();
            expect(observation).toMatchObject({
                providerAvailable: true,
                serviceWorker: { registration: 'unknown', active: null },
                serverDevices: { count: 0, expirations: [] },
                delivery: 'not-observed',
            });
        } finally {
            if (previous === undefined) {
                Reflect.deleteProperty(navigator, 'serviceWorker');
            } else {
                Object.defineProperty(navigator, 'serviceWorker', previous);
            }
        }
    });
});
