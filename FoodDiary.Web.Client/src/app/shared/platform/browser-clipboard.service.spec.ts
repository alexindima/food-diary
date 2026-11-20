import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import { BrowserClipboardService } from './browser-clipboard.service';

describe('BrowserClipboardService SSR', () => {
    it('does not touch a browser clipboard on the server', async () => {
        const readText = vi.fn();
        TestBed.configureTestingModule({
            providers: [
                { provide: PLATFORM_ID, useValue: 'server' },
                {
                    provide: DOCUMENT,
                    useValue: {
                        defaultView: { navigator: { clipboard: { readText } } },
                        visibilityState: 'visible',
                        hasFocus: (): boolean => true,
                    },
                },
            ],
        });
        const service = TestBed.inject(BrowserClipboardService);
        expect(await service.canReadWithoutPromptAsync()).toBe(false);
        await expect(service.readTextAsync()).rejects.toThrow('Clipboard unavailable');
        expect(readText).not.toHaveBeenCalled();
    });
});

describe('BrowserClipboardService foreground clipboard', () => {
    const originalClipboard = Object.getOwnPropertyDescriptor(window.navigator, 'clipboard');
    const originalPermissions = Object.getOwnPropertyDescriptor(window.navigator, 'permissions');
    let clipboardService: BrowserClipboardService;
    let readText: Mock<Clipboard['readText']>;
    let query: Mock<Permissions['query']>;
    let visibility: DocumentVisibilityState;
    let hasFocus: Mock<() => boolean>;

    beforeEach(() => {
        readText = vi.fn<Clipboard['readText']>().mockResolvedValue('Sample recipe link');
        query = vi.fn<Permissions['query']>();
        Object.defineProperties(window.navigator, {
            clipboard: { configurable: true, value: { readText } },
            permissions: { configurable: true, value: { query } },
        });
        visibility = 'visible';
        vi.spyOn(document, 'visibilityState', 'get').mockImplementation(() => visibility);
        hasFocus = vi.spyOn(document, 'hasFocus').mockReturnValue(true);
        clipboardService = TestBed.inject(BrowserClipboardService);
    });

    afterEach(() => {
        vi.restoreAllMocks();
        restoreCapability('clipboard', originalClipboard);
        restoreCapability('permissions', originalPermissions);
    });

    it.each(['granted', 'prompt', 'denied'] as const)(
        'checks clipboard permission in foreground with state %s without reading',
        async state => {
            query.mockResolvedValue(permissionStatus(state));

            await expect(clipboardService.canReadWithoutPromptAsync()).resolves.toBe(state === 'granted');

            expect(query).toHaveBeenCalledExactlyOnceWith({ name: 'clipboard-read' });
            expect(readText).not.toHaveBeenCalled();
        },
    );

    it('treats a browser that rejects the clipboard-read permission descriptor as unavailable for automatic reads', async () => {
        query.mockRejectedValue(new TypeError('Unsupported clipboard permission'));

        await expect(clipboardService.canReadWithoutPromptAsync()).resolves.toBe(false);

        expect(query).toHaveBeenCalledOnce();
        expect(readText).not.toHaveBeenCalled();
    });

    it('blocks clipboard and permission access while unfocused and resumes reading when active', async () => {
        hasFocus.mockReturnValue(false);

        await expect(clipboardService.canReadWithoutPromptAsync()).resolves.toBe(false);
        await expect(clipboardService.readTextAsync()).rejects.toThrow('Clipboard unavailable');

        expect(query).not.toHaveBeenCalled();
        expect(readText).not.toHaveBeenCalled();

        hasFocus.mockReturnValue(true);

        await expect(clipboardService.readTextAsync()).resolves.toBe('Sample recipe link');
        expect(readText).toHaveBeenCalledOnce();
    });

    it('forwards a refusal from a foreground clipboard read', async () => {
        const refusal = new DOMException('Read permission refused', 'NotAllowedError');
        readText.mockRejectedValue(refusal);

        await expect(clipboardService.readTextAsync()).rejects.toBe(refusal);
    });

    it('observes only active window returns and removes both native listeners on cleanup', () => {
        visibility = 'hidden';
        const callback = vi.fn();
        const stop = clipboardService.onReturn(callback);
        try {
            window.dispatchEvent(new Event('focus'));
            document.dispatchEvent(new Event('visibilitychange'));
            expect(callback).not.toHaveBeenCalled();

            visibility = 'visible';
            document.dispatchEvent(new Event('visibilitychange'));
            expect(callback).toHaveBeenCalledOnce();
            window.dispatchEvent(new Event('focus'));
            expect(callback).toHaveBeenCalledTimes(2);
        } finally {
            stop();
        }

        document.dispatchEvent(new Event('visibilitychange'));
        window.dispatchEvent(new Event('focus'));
        expect(callback).toHaveBeenCalledTimes(2);
    });
});

it('cleans up return observation when a document has no browser view', () => {
    const detached = document.implementation.createHTMLDocument();
    const remove = vi.spyOn(detached, 'removeEventListener');
    TestBed.configureTestingModule({
        providers: [
            { provide: PLATFORM_ID, useValue: 'server' },
            { provide: DOCUMENT, useValue: detached },
        ],
    });
    const callback = vi.fn();
    try {
        const stop = TestBed.inject(BrowserClipboardService).onReturn(callback);
        detached.dispatchEvent(new Event('visibilitychange'));
        stop();

        expect(callback).not.toHaveBeenCalled();
        expect(remove).toHaveBeenCalledWith('visibilitychange', expect.any(Function));
    } finally {
        remove.mockRestore();
    }
});

function permissionStatus(state: PermissionState): PermissionStatus {
    return Object.assign(new EventTarget(), { name: 'clipboard-read', state, onchange: null });
}

function restoreCapability(name: 'clipboard' | 'permissions', original: PropertyDescriptor | undefined): void {
    if (original === undefined) {
        Reflect.deleteProperty(window.navigator, name);
    } else {
        Object.defineProperty(window.navigator, name, original);
    }
}
