import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { BrowserWindowService } from './browser-window.service';

type TelegramCapableWindow = Window & {
    Telegram?: {
        WebApp?: {
            initData?: string;
        };
    };
};

describe('BrowserWindowService', () => {
    afterEach(() => {
        Reflect.deleteProperty(window, 'Telegram');
        vi.restoreAllMocks();
    });

    it('reads origin and href from the browser document window', () => {
        const service = createService(document);

        expect(service.getOrigin()).toBe(window.location.origin);
        expect(service.getHref()).toBe(window.location.href);
        expect(service.getPathname()).toBe(window.location.pathname);
        expect(service.getHostname()).toBe(window.location.hostname);
        expect(service.isAvailable()).toBe(true);
    });

    it('normalizes Telegram init data', () => {
        (window as TelegramCapableWindow).Telegram = {
            WebApp: {
                initData: '  telegram-payload  ',
            },
        };
        const service = createService(document);

        expect(service.getTelegramInitData()).toBe('telegram-payload');
    });

    it('returns null when Telegram init data is missing', () => {
        const service = createService(document);

        expect(service.getTelegramInitData()).toBeNull();
    });

    it('replaces current URL through history API', () => {
        const replaceState = vi.spyOn(window.history, 'replaceState').mockImplementation(() => {});
        const service = createService(document);

        service.replaceCurrentUrl('/next?x=1');

        expect(replaceState).toHaveBeenCalledWith({}, '', '/next?x=1');
    });

    it('registers pagehide listeners on the browser window', () => {
        const addEventListener = vi.spyOn(window, 'addEventListener');
        const callback = vi.fn();
        const service = createService(document);

        service.onPageHideOnce(callback);

        expect(addEventListener).toHaveBeenCalledWith('pagehide', callback, { once: true });
    });
});

function createService(ownerDocument: Document): BrowserWindowService {
    TestBed.configureTestingModule({
        providers: [BrowserWindowService, { provide: DOCUMENT, useValue: ownerDocument }],
    });

    return TestBed.inject(BrowserWindowService);
}
