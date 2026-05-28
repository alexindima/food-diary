import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

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

type TestDefaultView = {
    location?: { origin: string };
    navigator?: { userAgent: string };
    Notification?: { permission: NotificationPermission };
    URL?: typeof URL;
};

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
