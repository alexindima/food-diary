import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BrowserNotificationCapabilityService } from './browser-notification-capability.service';

describe('BrowserNotificationCapabilityService', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
    });

    it('should read notification permission and user agent from document default view', () => {
        TestBed.configureTestingModule({
            providers: [
                {
                    provide: DOCUMENT,
                    useValue: createDocument({
                        Notification: { permission: 'granted' },
                        navigator: { userAgent: 'Test Browser' },
                    }),
                },
            ],
        });

        const service = TestBed.inject(BrowserNotificationCapabilityService);

        expect(service.getPermission()).toBe('granted');
        expect(service.getUserAgent()).toBe('Test Browser');
    });

    it('should return unsupported when default view is unavailable', () => {
        TestBed.configureTestingModule({
            providers: [
                {
                    provide: DOCUMENT,
                    useValue: {
                        defaultView: null,
                    },
                },
            ],
        });

        const service = TestBed.inject(BrowserNotificationCapabilityService);

        expect(service.getPermission()).toBe('unsupported');
        expect(service.getUserAgent()).toBe('');
        expect(service.toAppUrl('https://example.com/path')).toBe('https://example.com/path');
    });

    it('should convert same-origin absolute urls to app urls', () => {
        TestBed.configureTestingModule({
            providers: [
                {
                    provide: DOCUMENT,
                    useValue: createDocument({
                        location: { origin: 'https://app.example.com' },
                    }),
                },
            ],
        });

        const service = TestBed.inject(BrowserNotificationCapabilityService);
        const absoluteUrl = 'https://app.example.com/fasting?intent=check-in#top';

        expect(service.toAppUrl(absoluteUrl)).toBe('/fasting?intent=check-in#top');
        expect(service.toAppUrl('/meals')).toBe('/meals');
    });
});

describe('BrowserNotificationCapabilityService permission requests', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
    });

    it('requests permission synchronously so the browser retains the user gesture', async () => {
        const requestPermission = vi.fn().mockResolvedValue('granted');
        TestBed.configureTestingModule({
            providers: [
                {
                    provide: DOCUMENT,
                    useValue: createDocument({
                        Notification: { permission: 'default', requestPermission },
                    }),
                },
            ],
        });
        const service = TestBed.inject(BrowserNotificationCapabilityService);

        const permission = service.requestPermissionAsync();

        expect(requestPermission).toHaveBeenCalledOnce();
        await expect(permission).resolves.toBe('granted');
    });

    it.each(['granted', 'denied'] as const)('does not re-prompt when permission is %s', async permission => {
        const requestPermission = vi.fn();
        TestBed.configureTestingModule({
            providers: [
                {
                    provide: DOCUMENT,
                    useValue: createDocument({
                        Notification: { permission, requestPermission },
                    }),
                },
            ],
        });

        await expect(TestBed.inject(BrowserNotificationCapabilityService).requestPermissionAsync()).resolves.toBe(permission);
        expect(requestPermission).not.toHaveBeenCalled();
    });

    it('does not request notification permission during server rendering', async () => {
        const requestPermission = vi.fn();
        TestBed.configureTestingModule({
            providers: [
                { provide: PLATFORM_ID, useValue: 'server' },
                { provide: DOCUMENT, useValue: createDocument({ Notification: { permission: 'default', requestPermission } }) },
            ],
        });

        await expect(TestBed.inject(BrowserNotificationCapabilityService).requestPermissionAsync()).resolves.toBe('unsupported');
        expect(requestPermission).not.toHaveBeenCalled();
    });
});

describe('BrowserNotificationCapabilityService edge cases', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
    });

    it('should expose denied permission state', () => {
        TestBed.configureTestingModule({
            providers: [
                {
                    provide: DOCUMENT,
                    useValue: createDocument({
                        Notification: { permission: 'denied' },
                    }),
                },
            ],
        });

        const service = TestBed.inject(BrowserNotificationCapabilityService);

        expect(service.getPermission()).toBe('denied');
        expect(service.isPermissionDenied()).toBe(true);
    });

    it('should keep cross-origin absolute urls unchanged', () => {
        TestBed.configureTestingModule({
            providers: [
                {
                    provide: DOCUMENT,
                    useValue: createDocument({
                        location: { origin: 'https://app.example.com' },
                    }),
                },
            ],
        });

        const service = TestBed.inject(BrowserNotificationCapabilityService);
        const externalUrl = 'https://other.example.com/fasting?intent=check-in';

        expect(service.toAppUrl(externalUrl)).toBe(externalUrl);
    });

    it('should keep malformed absolute urls unchanged', () => {
        TestBed.configureTestingModule({
            providers: [
                {
                    provide: DOCUMENT,
                    useValue: createDocument({
                        URL: ThrowingUrl,
                    }),
                },
            ],
        });

        const service = TestBed.inject(BrowserNotificationCapabilityService);
        const malformedUrl = 'https://app.example.com/%';

        expect(service.toAppUrl(malformedUrl)).toBe(malformedUrl);
    });

    it('should report unsupported capabilities on the server platform', () => {
        TestBed.configureTestingModule({
            providers: [
                { provide: PLATFORM_ID, useValue: 'server' },
                {
                    provide: DOCUMENT,
                    useValue: createDocument({
                        Notification: { permission: 'granted' },
                        navigator: { userAgent: 'Server Agent' },
                    }),
                },
            ],
        });

        const service = TestBed.inject(BrowserNotificationCapabilityService);

        expect(service.getPermission()).toBe('unsupported');
        expect(service.isPermissionDenied()).toBe(false);
        expect(service.getUserAgent()).toBe('');
        expect(service.toAppUrl('https://app.example.com/profile')).toBe('https://app.example.com/profile');
    });
});

type TestDefaultView = {
    location?: { origin: string };
    navigator?: { userAgent: string };
    Notification?: { permission: NotificationPermission; requestPermission?: typeof Notification.requestPermission };
    URL?: typeof URL;
};

class ThrowingUrl extends URL {
    public constructor(url: string | URL, base?: string | URL) {
        super(url, base);
        throw new Error('Invalid URL');
    }
}

function createDocument(defaultView: TestDefaultView): Document {
    return {
        defaultView: {
            URL,
            location: { origin: 'https://app.example.com' },
            navigator: { userAgent: '' },
            ...defaultView,
        },
    } as unknown as Document;
}
