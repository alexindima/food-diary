import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

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
