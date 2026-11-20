import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { BrowserWindowService } from './browser-window.service';

type OpenTelegramInvoice = (url: string, callback: (status: string) => void) => void;

type TelegramCapableWindow = Window & {
    Telegram?: {
        WebApp?: {
            initData?: string;
            openInvoice?: OpenTelegramInvoice;
        };
    };
};

const TEST_SCROLL_Y = 120;
const TEST_TIMEOUT_MS = 500;

describe('BrowserWindowService visibility flush', () => {
    it('waits through visible events and flushes once when the document becomes hidden', () => {
        const target = document.implementation.createHTMLDocument('visibility fixture');
        let visibility = 'visible';
        Object.defineProperty(target, 'visibilityState', { get: () => visibility });
        const callback = vi.fn();
        const service = createService(target);
        service.onVisibilityHiddenOnce(callback);
        target.dispatchEvent(new Event('visibilitychange'));
        expect(callback).not.toHaveBeenCalled();
        visibility = 'hidden';
        target.dispatchEvent(new Event('visibilitychange'));
        target.dispatchEvent(new Event('visibilitychange'));
        expect(callback, 'Visibility flush must survive an earlier visible event').toHaveBeenCalledOnce();
    });
});

describe('BrowserWindowService location and Telegram bridge', () => {
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

    it.each(['', '  '])('treats a blank Telegram launch payload as absent', initData => {
        (window as TelegramCapableWindow).Telegram = { WebApp: { initData } };

        expect(createService(document).getTelegramInitData()).toBeNull();
    });

    it('waits for Telegram to close the invoice and forwards its result', async () => {
        let completeInvoice: (status: string) => void = () => {};
        const openInvoice = vi.fn<OpenTelegramInvoice>((_url, complete) => {
            completeInvoice = complete;
        });
        const app = { openInvoice };
        (window as TelegramCapableWindow).Telegram = { WebApp: app };
        const url = 'https://example.test/invoice';
        const result = createService(document).openTelegramInvoiceAsync(url);

        expect(openInvoice).toHaveBeenCalledExactlyOnceWith(url, expect.any(Function));
        expect(openInvoice.mock.contexts[0]).toBe(app);
        completeInvoice('cancelled');

        await expect(result).resolves.toBe('cancelled');
    });

    it('rejects opening an invoice when the Telegram client has no invoice support', async () => {
        (window as TelegramCapableWindow).Telegram = { WebApp: {} };

        await expect(createService(document).openTelegramInvoiceAsync('https://example.test/invoice')).rejects.toThrow(
            'Telegram invoice interface unavailable',
        );
    });
});

describe('BrowserWindowService window actions', () => {
    afterEach(() => {
        Reflect.deleteProperty(window, 'matchMedia');
        vi.restoreAllMocks();
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
        const mediaQuery = {
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

describe('BrowserWindowService task yielding', () => {
    const originalScheduler = Object.getOwnPropertyDescriptor(window, 'scheduler');

    afterEach(() => {
        vi.restoreAllMocks();
        if (originalScheduler === undefined) {
            Reflect.deleteProperty(window, 'scheduler');
        } else {
            Object.defineProperty(window, 'scheduler', originalScheduler);
        }
    });

    it('waits for the native scheduler continuation', async () => {
        let continueTask: (() => void) | undefined;
        const scheduler = {
            yield: vi.fn(
                async () =>
                    new Promise<void>(resolve => {
                        continueTask = resolve;
                    }),
            ),
        };
        Object.defineProperty(window, 'scheduler', { configurable: true, value: scheduler });
        let completed = false;
        const task = createService(document)
            .yieldToBrowserAsync()
            .then(() => (completed = true));

        expect(scheduler.yield).toHaveBeenCalledOnce();
        expect(scheduler.yield.mock.contexts[0]).toBe(scheduler);
        expect(completed).toBe(false);
        continueTask?.();
        await task;
        expect(completed).toBe(true);
    });

    it.each([undefined, {}])('waits for a browser timer when scheduler.yield is unavailable: %s', async scheduler => {
        Object.defineProperty(window, 'scheduler', { configurable: true, value: scheduler });
        let continueTask: (() => void) | undefined;
        const timer = vi.spyOn(window, 'setTimeout').mockImplementation(callback => {
            if (typeof callback === 'function') {
                continueTask = (): void => {
                    callback();
                };
            }
            return 1;
        });
        let completed = false;
        const task = createService(document)
            .yieldToBrowserAsync()
            .then(() => (completed = true));

        expect(timer).toHaveBeenCalledWith(expect.any(Function), 0);
        expect(completed).toBe(false);
        continueTask?.();
        await task;
        expect(completed).toBe(true);
    });

    it('completes without browser work when rendering without a window', async () => {
        const scheduler = { yield: vi.fn() };
        Object.defineProperty(window, 'scheduler', { configurable: true, value: scheduler });
        const timer = vi.spyOn(window, 'setTimeout');

        await createService(document.implementation.createHTMLDocument()).yieldToBrowserAsync();

        expect(scheduler.yield).not.toHaveBeenCalled();
        expect(timer).not.toHaveBeenCalled();
    });
});

describe('BrowserWindowService storage events', () => {
    it('observes local storage changes and stops listening after cleanup', () => {
        const service = createService(document);
        const callback = vi.fn();
        const stopListening = service.onLocalStorageChange(callback);
        const sessionEvent = new StorageEvent('storage', { key: 'session' });
        Object.defineProperty(sessionEvent, 'storageArea', { value: sessionStorage });
        window.dispatchEvent(sessionEvent);
        expect(callback).not.toHaveBeenCalled();

        const event = new StorageEvent('storage', { key: 'session' });
        Object.defineProperty(event, 'storageArea', { value: localStorage });
        window.dispatchEvent(event);
        expect(callback).toHaveBeenCalledWith(event);

        stopListening();
        window.dispatchEvent(event);
        expect(callback).toHaveBeenCalledOnce();
    });
});

describe('BrowserWindowService platform capabilities', () => {
    const originalClipboard = Object.getOwnPropertyDescriptor(window.navigator, 'clipboard');
    const originalMediaDevices = Object.getOwnPropertyDescriptor(window.navigator, 'mediaDevices');

    afterEach(() => {
        vi.restoreAllMocks();
        if (originalClipboard === undefined) {
            Reflect.deleteProperty(window.navigator, 'clipboard');
        } else {
            Object.defineProperty(window.navigator, 'clipboard', originalClipboard);
        }
        if (originalMediaDevices === undefined) {
            Reflect.deleteProperty(window.navigator, 'mediaDevices');
        } else {
            Object.defineProperty(window.navigator, 'mediaDevices', originalMediaDevices);
        }
    });

    it('reports no browser APIs on the server even when the injected document has a window', async () => {
        const service = createService(document, 'server');
        const callback = vi.fn();

        expect(service.isAvailable()).toBe(false);
        expect(service.getOrigin()).toBeUndefined();
        expect(service.getHref()).toBeNull();
        expect(service.getSearch()).toBeNull();
        expect(service.getPathname()).toBeNull();
        expect(service.getHostname()).toBeNull();
        expect(service.getViewportWidth()).toBe(0);
        expect(service.getScrollY()).toBe(0);
        expect(service.requestAnimationFrame(callback)).toBeNull();
        expect(service.setInterval(callback, TEST_TIMEOUT_MS)).toBeNull();
        expect(service.setTimeout(callback, TEST_TIMEOUT_MS)).toBeNull();
        expect(service.createBarcodeDetector(['ean_13'])).toBeNull();
        await expect(service.openTelegramInvoiceAsync('https://example.test/invoice')).rejects.toThrow(
            'Telegram invoice interface unavailable',
        );
        await expect(service.copyTextAsync('Diary link')).rejects.toThrow('Clipboard unavailable');
        await expect(service.getUserMediaAsync({ video: true })).rejects.toThrow('Browser media devices are unavailable');
        service.onResize(callback)();
        service.onLocalStorageChange(callback)();
        expect(callback).not.toHaveBeenCalled();
    });

    it('copies the provided text using browser clipboard permission', async () => {
        const writeText = vi.fn<Clipboard['writeText']>().mockResolvedValue(undefined);
        Object.defineProperty(window.navigator, 'clipboard', { configurable: true, value: { writeText } });
        const service = createService(document);

        await service.copyTextAsync('https://example.test/recipes/view');

        expect(writeText).toHaveBeenCalledExactlyOnceWith('https://example.test/recipes/view');
    });

    it('preserves camera constraints and propagates a browser permission refusal', async () => {
        const refusal = new DOMException('Camera permission denied', 'NotAllowedError');
        const getUserMedia = vi.fn<MediaDevices['getUserMedia']>().mockRejectedValue(refusal);
        Object.defineProperty(window.navigator, 'mediaDevices', { configurable: true, value: { getUserMedia } });
        const constraints: MediaStreamConstraints = { video: { facingMode: 'environment' }, audio: false };

        await expect(createService(document).getUserMediaAsync(constraints)).rejects.toBe(refusal);

        expect(getUserMedia).toHaveBeenCalledExactlyOnceWith(constraints);
    });

    it('cancels only the requested animation frame and clears its resize listener', () => {
        const frame = 42;
        const schedule = vi.spyOn(window, 'requestAnimationFrame').mockReturnValue(frame);
        const cancel = vi.spyOn(window, 'cancelAnimationFrame');
        const service = createService(document);
        const callback = vi.fn();
        const unlisten = service.onResize(callback);

        window.dispatchEvent(new Event('resize'));
        const id = service.requestAnimationFrame(callback);
        service.cancelAnimationFrame(null);
        service.cancelAnimationFrame(id);
        unlisten();
        window.dispatchEvent(new Event('resize'));

        expect(schedule).toHaveBeenCalledExactlyOnceWith(callback);
        expect(cancel).toHaveBeenCalledExactlyOnceWith(frame);
        expect(callback).toHaveBeenCalledOnce();
    });

    it.each(['', 'https://example.test/diary'])('preserves the document referrer %s', referrer => {
        const source = document.implementation.createHTMLDocument();
        Object.defineProperty(source, 'referrer', { value: referrer });

        expect(createService(source).getReferrer()).toBe(referrer === '' ? null : referrer);
    });
});

describe('BrowserWindowService blocked operations and cleanup', () => {
    afterEach(() => {
        vi.restoreAllMocks();
    });

    it('returns a blocked popup result while preserving the requested window features', () => {
        const open = vi.spyOn(window, 'open').mockReturnValue(null);
        const service = createService(document);

        expect(service.open('/recipes', '_blank', 'noopener,noreferrer')).toBeNull();
        expect(open).toHaveBeenCalledExactlyOnceWith('/recipes', '_blank', 'noopener,noreferrer');
    });

    it('clears only the allocated interval and ignores cleanup when no timer was registered', () => {
        const intervalId = 42;
        const set = vi.spyOn(window, 'setInterval').mockReturnValue(intervalId);
        const clear = vi.spyOn(window, 'clearInterval').mockImplementation(() => {});
        const service = createService(document);
        const callback = vi.fn();

        const active = service.setInterval(callback, TEST_TIMEOUT_MS);
        service.clearInterval(null);
        service.clearInterval(active);

        expect(set).toHaveBeenCalledExactlyOnceWith(callback, TEST_TIMEOUT_MS);
        expect(clear).toHaveBeenCalledExactlyOnceWith(intervalId);
    });
});

describe('BrowserWindowService barcode capabilities', () => {
    it('constructs the native barcode detector with the formats requested by the scanner', () => {
        const construct = vi.fn<(options?: BarcodeDetectorOptions) => void>();
        class NativeDetector implements BarcodeDetector {
            public readonly detect = vi.fn<BarcodeDetector['detect']>();

            public constructor(options?: BarcodeDetectorOptions) {
                construct(options);
            }
        }
        vi.stubGlobal('BarcodeDetector', NativeDetector);
        try {
            const formats = ['ean_13', 'qr_code'];

            expect(createService(document).createBarcodeDetector(formats)).toBeInstanceOf(NativeDetector);
            expect(construct).toHaveBeenCalledExactlyOnceWith({ formats });
        } finally {
            vi.unstubAllGlobals();
        }
    });
});

describe('BrowserWindowService locks', () => {
    const originalLocks = Object.getOwnPropertyDescriptor(window.navigator, 'locks');

    afterEach(() => {
        if (originalLocks !== undefined) {
            Object.defineProperty(window.navigator, 'locks', originalLocks);
        } else {
            Reflect.deleteProperty(window.navigator, 'locks');
        }
    });

    it('keeps the operation pending until the browser grants the lock', async () => {
        let grantLock: (() => void) | undefined;
        const request = vi.fn(
            async <T>(_name: string, operation: () => Promise<T>): Promise<T> =>
                new Promise(resolve => {
                    grantLock = (): void => {
                        resolve(operation());
                    };
                }),
        );
        Object.defineProperty(window.navigator, 'locks', { configurable: true, value: { request } });
        const operation = vi.fn<() => Promise<string>>().mockResolvedValue('completed');
        const result = createService(document).runWithLockAsync('shared-session', operation);

        expect(operation).not.toHaveBeenCalled();
        expect(request).toHaveBeenCalledWith('shared-session', operation);
        grantLock?.();
        expect(await result).toBe('completed');
        expect(operation).toHaveBeenCalledOnce();
    });

    it('runs normally when the browser has no lock API', async () => {
        Object.defineProperty(window.navigator, 'locks', { configurable: true, value: undefined });
        const operation = vi.fn<() => Promise<string>>().mockResolvedValue('fallback');
        expect(await createService(document).runWithLockAsync('shared-session', operation)).toBe('fallback');
    });

    it('runs normally when the document has no browser window', async () => {
        const ownerDocument = document.implementation.createHTMLDocument();
        const operation = vi.fn<() => Promise<string>>().mockResolvedValue('fallback');
        expect(await createService(ownerDocument).runWithLockAsync('shared-session', operation)).toBe('fallback');
    });
});

function createService(ownerDocument: Document, platform = 'browser'): BrowserWindowService {
    TestBed.configureTestingModule({
        providers: [BrowserWindowService, { provide: DOCUMENT, useValue: ownerDocument }, { provide: PLATFORM_ID, useValue: platform }],
    });

    return TestBed.inject(BrowserWindowService);
}
