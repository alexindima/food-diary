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
            TestBed.configureTestingModule({ providers: [{ provide: NotificationService, useValue: {
                getWebPushConfiguration: vi.fn().mockReturnValue(of({ enabled: true, publicKey: 'private-provider-key' })),
                getWebPushSubscriptions: vi.fn().mockReturnValue(of([{ endpoint: 'private-server-endpoint', expirationTimeUtc: null, userAgent: 'private-agent' }])),
            } }] });
            const result = await TestBed.inject(LocalWebmcpPushService).readAsync();
            expect(result).toMatchObject({ notificationPermission: 'granted', providerAvailable: true, serviceWorker: { controlled: true, active: 'activated' }, browserSubscription: { presence: 'present', expirationTime: null }, serverDevices: { count: 1, expirations: [null] }, delivery: 'not-observed' });
            expect(JSON.stringify(result)).not.toContain('private-');
        } finally {
            if (previous === undefined) {Reflect.deleteProperty(navigator, 'serviceWorker');} else {Object.defineProperty(navigator, 'serviceWorker', previous);}
        }
    });
    it('leaves failed server observations unknown', async () => {
        TestBed.configureTestingModule({ providers: [{ provide: NotificationService, useValue: {
            getWebPushConfiguration: vi.fn().mockReturnValue(throwError(() => new Error('private-error'))),
            getWebPushSubscriptions: vi.fn().mockReturnValue(throwError(() => new Error('private-error'))),
        } }] });
        const result = await TestBed.inject(LocalWebmcpPushService).readAsync();
        expect(result).toMatchObject({ providerAvailable: null, serverDevices: null, delivery: 'not-observed' });
        expect(JSON.stringify(result)).not.toContain('private-');
    });
});
