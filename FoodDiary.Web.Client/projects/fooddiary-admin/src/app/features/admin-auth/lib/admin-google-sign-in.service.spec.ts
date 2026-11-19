import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminGoogleSignInService } from './admin-google-sign-in.service';

const CLIENT_ID = 'test-google-client';
const GOOGLE_SCRIPT = 'script[src="https://accounts.google.com/gsi/client"]';

type GoogleInit = {
    client_id: string;
    callback: (response: { credential?: string | null }) => void;
    ux_mode: 'popup';
    auto_select: false;
};

function googleScript(): HTMLScriptElement {
    const script = document.head.querySelector<HTMLScriptElement>(GOOGLE_SCRIPT);
    if (script === null) {
        throw new Error('Expected the Google Identity Services script');
    }
    return script;
}

let service: AdminGoogleSignInService;
let target: HTMLDivElement;

beforeEach(() => {
    TestBed.configureTestingModule({ providers: [AdminGoogleSignInService] });
    service = TestBed.inject(AdminGoogleSignInService);
    target = document.createElement('div');
    document.body.appendChild(target);
});

afterEach(() => {
    vi.unstubAllGlobals();
    document.head.querySelectorAll(GOOGLE_SCRIPT).forEach(script => {
        script.remove();
    });
    target.remove();
});

describe('AdminGoogleSignInService', () => {
    it.each([
        { preference: false, theme: 'filled_blue' },
        { preference: true, theme: 'filled_black' },
    ])('uses $theme with the resolved browser preference and forwards only a live credential', async ({ preference, theme }) => {
        target.style.colorScheme = 'dark';
        const initialize = vi.fn<(config: GoogleInit) => void>();
        const renderButton = vi.fn();
        const matchMedia = vi.fn(() => ({ matches: preference }));
        vi.stubGlobal('google', { accounts: { id: { initialize, renderButton } } });
        vi.stubGlobal('matchMedia', matchMedia);
        const onCredential = vi.fn<(credential: string) => void>();

        const ready = service.renderAsync(target, CLIENT_ID, 'ru', onCredential);
        const script = googleScript();
        expect(script.async).toBe(true);
        expect(renderButton).not.toHaveBeenCalled();
        script.dispatchEvent(new Event('load'));
        await ready;

        expect(matchMedia).toHaveBeenCalledExactlyOnceWith('(prefers-color-scheme: dark)');
        expect(initialize).toHaveBeenCalledOnce();
        const [[{ callback, ...config }]] = initialize.mock.calls;
        expect(config).toEqual({
            client_id: CLIENT_ID,
            ux_mode: 'popup',
            auto_select: false,
        });
        expect(renderButton).toHaveBeenCalledExactlyOnceWith(target, { theme, size: 'large', shape: 'pill', locale: 'ru' });
        expect(new Set(target.style.colorScheme.split(' '))).toEqual(new Set(['only', 'light']));

        callback({});
        callback({ credential: null });
        callback({ credential: '' });
        callback({ credential: 'valid-token' });
        target.remove();
        callback({ credential: 'late-token' });
        expect(onCredential).toHaveBeenCalledExactlyOnceWith('valid-token');
    });

    it('shares the in-progress script and skips a target removed before loading completes', async () => {
        const liveTarget = document.createElement('div');
        target.after(liveTarget);
        const initialize = vi.fn();
        const renderButton = vi.fn();
        vi.stubGlobal('google', { accounts: { id: { initialize, renderButton } } });
        const ready = service.renderAsync(target, CLIENT_ID, 'en', vi.fn());
        const liveReady = service.renderAsync(liveTarget, CLIENT_ID, 'en', vi.fn());
        try {
            target.remove();
            expect(document.head.querySelectorAll(GOOGLE_SCRIPT)).toHaveLength(1);
            googleScript().dispatchEvent(new Event('load'));
            await Promise.all([ready, liveReady]);

            expect(initialize).toHaveBeenCalledOnce();
            expect(renderButton).toHaveBeenCalledExactlyOnceWith(liveTarget, {
                theme: 'filled_blue',
                size: 'large',
                shape: 'pill',
                locale: 'en',
            });
        } finally {
            liveTarget.remove();
        }
    });

    it.each(['error', 'load'])('fails safely on script %s without a usable identity API', async event => {
        const onCredential = vi.fn<(credential: string) => void>();
        const ready = expect(service.renderAsync(target, CLIENT_ID, 'en', onCredential)).rejects.toThrow(
            'Google Identity Services unavailable',
        );
        googleScript().dispatchEvent(new Event(event));
        await ready;
        expect(onCredential).not.toHaveBeenCalled();
    });
});
