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

const TEST_SCROLL_Y = 120;
const TEST_TIMEOUT_MS = 500;

describe('BrowserWindowService', () => {
    afterEach(() => {
        Reflect.deleteProperty(window, 'Telegram');
        Reflect.deleteProperty(window, 'matchMedia');
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

    it('delegates viewport and scrolling operations to the browser window', () => {
        const mediaQuery: MediaQueryList = {
            matches: true,
            media: '(max-width: 700px)',
            onchange: null,
            addEventListener: vi.fn(),
            removeEventListener: vi.fn(),
            dispatchEvent: vi.fn(),
            addListener: vi.fn(),
            removeListener: vi.fn(),
        };
        const matchMedia = vi.fn<Window['matchMedia']>().mockReturnValue(mediaQuery);
        Object.defineProperty(window, 'matchMedia', { configurable: true, value: matchMedia });
        const scrollTo = vi.spyOn(window, 'scrollTo').mockImplementation(() => {});
        const service = createService(document);

        expect(service.matchMedia('(max-width: 700px)')).toBe(mediaQuery);
        expect(matchMedia).toHaveBeenCalledWith('(max-width: 700px)');
        expect(service.getViewportWidth()).toBe(window.innerWidth);
        expect(service.getScrollY()).toBe(window.scrollY);

        service.scrollTo(0, TEST_SCROLL_Y);

        expect(scrollTo).toHaveBeenCalledWith(0, TEST_SCROLL_Y);
    });

    it('opens a window and schedules browser work', () => {
        const popup = { close: vi.fn() } as unknown as Window;
        const open = vi.spyOn(window, 'open').mockReturnValue(popup);
        const setTimeout = vi.spyOn(window, 'setTimeout').mockImplementation(() => 1);
        const callback = vi.fn();
        const service = createService(document);

        expect(service.open('/admin', '_blank')).toBe(popup);
        service.setTimeout(callback, TEST_TIMEOUT_MS);

        expect(open).toHaveBeenCalledWith('/admin', '_blank');
        expect(setTimeout).toHaveBeenCalledWith(callback, TEST_TIMEOUT_MS);
    });
});

function createService(ownerDocument: Document): BrowserWindowService {
    TestBed.configureTestingModule({
        providers: [BrowserWindowService, { provide: DOCUMENT, useValue: ownerDocument }],
    });

    return TestBed.inject(BrowserWindowService);
}
