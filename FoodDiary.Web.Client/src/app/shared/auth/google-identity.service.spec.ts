import { PLATFORM_ID, type Renderer2, RendererFactory2, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import type { AppThemeName } from '../../theme/app-theme.config';
import { ThemeService } from '../theme/theme.service';
import { GoogleIdentityService } from './google-identity.service';

const CLIENT_ID = 'google-client-id';
const UPDATED_CLIENT_ID = 'updated-client-id';
const CREDENTIAL = 'credential-token';

type GoogleIdentityApiMock = {
    cancel: ReturnType<typeof vi.fn<GoogleIdentityApi['cancel']>>;
    initialize: ReturnType<typeof vi.fn<GoogleIdentityApi['initialize']>>;
    prompt: ReturnType<typeof vi.fn<GoogleIdentityApi['prompt']>>;
    renderButton: ReturnType<typeof vi.fn<GoogleIdentityApi['renderButton']>>;
} & GoogleIdentityApi;

type GoogleIdentityApi = {
    cancel: () => void;
    initialize: (config: { client_id: string; callback: (response: { credential?: string | null }) => void }) => void;
    prompt: () => void;
    renderButton: (element: HTMLElement, options: Record<string, unknown>) => void;
};

const googleWindow = window as Window & {
    google?: {
        accounts?: {
            id?: GoogleIdentityApi;
        };
    };
};

describe('GoogleIdentityService browser initialization', () => {
    let googleIdentityApi: GoogleIdentityApiMock;

    beforeEach(() => {
        resetGoogleIdentityTestState();
        googleIdentityApi = createGoogleIdentityApiMock();
    });

    afterEach(() => {
        resetGoogleIdentityTestState();
        vi.restoreAllMocks();
    });

    it('loads Google script and initializes client once script resolves', async () => {
        const service = setupBrowserService();
        const callback = vi.fn();

        const initialized = service.initializeAsync({ clientId: CLIENT_ID, callback });
        const script = getGoogleScript();
        googleWindow.google = { accounts: { id: googleIdentityApi } };
        script.onload?.(new Event('load'));
        await initialized;

        expect(script.async).toBe(true);
        expect(script.defer).toBe(true);
        expect(googleIdentityApi.initialize).toHaveBeenCalledWith(
            expect.objectContaining({
                client_id: CLIENT_ID,
                ux_mode: 'popup',
                auto_select: false,
                cancel_on_tap_outside: true,
            }),
        );
    });

    it('forwards non-empty credential to the latest callback for the same client', async () => {
        const service = setupBrowserService();
        const firstCallback = vi.fn();
        const latestCallback = vi.fn();

        const initialized = service.initializeAsync({ clientId: CLIENT_ID, callback: firstCallback });
        googleWindow.google = { accounts: { id: googleIdentityApi } };
        getGoogleScript().onload?.(new Event('load'));
        await initialized;

        await service.initializeAsync({ clientId: CLIENT_ID, callback: latestCallback });
        const initializeConfig = googleIdentityApi.initialize.mock.calls.at(-1)?.[0] as {
            callback: (response: { credential?: string | null }) => void;
        };
        initializeConfig.callback({ credential: CREDENTIAL });
        initializeConfig.callback({ credential: '' });
        initializeConfig.callback({ credential: null });
        initializeConfig.callback({});

        expect(googleIdentityApi.initialize).toHaveBeenCalledTimes(1);
        expect(firstCallback).not.toHaveBeenCalled();
        expect(latestCallback).toHaveBeenCalledOnce();
        expect(latestCallback).toHaveBeenCalledWith(CREDENTIAL);
    });
});

describe('GoogleIdentityService browser controls', () => {
    let googleIdentityApi: GoogleIdentityApiMock;

    beforeEach(() => {
        resetGoogleIdentityTestState();
        googleIdentityApi = createGoogleIdentityApiMock();
    });

    afterEach(() => {
        resetGoogleIdentityTestState();
        vi.restoreAllMocks();
    });

    it('renders button and forwards prompt/cancel when Google API is available', () => {
        const service = setupBrowserService();
        const target = document.createElement('div');
        googleWindow.google = { accounts: { id: googleIdentityApi } };

        service.renderButton(target, 'filled_blue');
        service.prompt();
        service.cancel();

        expect(googleIdentityApi.renderButton).toHaveBeenCalledWith(
            target,
            expect.objectContaining({
                theme: 'filled_blue',
                shape: 'pill',
                locale: 'en',
            }),
        );
        expect(googleIdentityApi.prompt).toHaveBeenCalledOnce();
        expect(googleIdentityApi.cancel).toHaveBeenCalledOnce();
    });

    it('uses the dark Google button and keeps the iframe backdrop transparent on dark pages', () => {
        const theme = signal<AppThemeName>('dark');
        const service = setupBrowserService(theme);
        const target = document.createElement('div');
        googleWindow.google = { accounts: { id: googleIdentityApi } };

        service.renderButton(target, 'filled_blue', 'ru');

        expect(googleIdentityApi.renderButton).toHaveBeenLastCalledWith(
            target,
            expect.objectContaining({ theme: 'filled_black', locale: 'ru' }),
        );
        expect(target.style.getPropertyValue('color-scheme')).toBe('light only');

        theme.set('ocean');
        service.renderButton(target, 'filled_blue', 'ru');

        expect(googleIdentityApi.renderButton).toHaveBeenLastCalledWith(
            target,
            expect.objectContaining({ theme: 'filled_blue', locale: 'ru' }),
        );
    });
});

describe('GoogleIdentityService browser script errors', () => {
    beforeEach(() => {
        resetGoogleIdentityTestState();
    });

    afterEach(() => {
        resetGoogleIdentityTestState();
        vi.restoreAllMocks();
    });

    it('keeps the local sign-in fallback and uses a late SDK without reloading the script', async () => {
        const service = setupBrowserService();
        const callback = vi.fn();
        const initialized = service.initializeAsync({ clientId: CLIENT_ID, callback });
        const unavailable = expect(initialized).rejects.toThrow('Google Identity Services unavailable');
        const script = getGoogleScript();
        script.dispatchEvent(new Event('load'));
        await unavailable;

        const target = document.createElement('div');
        const fallback = document.createElement('span');
        fallback.textContent = 'Sign in with email';
        target.append(fallback);
        service.renderButton(target);
        expect(target.firstElementChild).toBe(fallback);

        const googleApi = createGoogleIdentityApiMock();
        googleWindow.google = { accounts: { id: googleApi } };
        await service.initializeAsync({ clientId: CLIENT_ID, callback });
        expect(document.head.querySelectorAll('script[src="https://accounts.google.com/gsi/client"]')).toHaveLength(1);
        expect(getGoogleScript()).toBe(script);
        expect(googleApi.initialize).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ client_id: CLIENT_ID }));

        service.renderButton(target);
        expect(target.textContent).toBe('');
        expect(googleApi.renderButton).toHaveBeenCalledExactlyOnceWith(target, expect.objectContaining({ theme: 'outline' }));
        expect(callback).not.toHaveBeenCalled();
    });
    it('rejects initialization when Google script fails to load', async () => {
        const service = setupBrowserService();

        const initialized = service.initializeAsync({ clientId: UPDATED_CLIENT_ID, callback: vi.fn() });
        getGoogleScript().onerror?.(new Event('error'));

        await expect(initialized).rejects.toThrow('Failed to load Google Identity script');
    });
});

describe('GoogleIdentityService server guards', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
        delete googleWindow.google;
    });

    afterEach(() => {
        delete googleWindow.google;
    });

    it('throws during initialization and ignores UI methods outside browser platform', async () => {
        const service = setupServerService();
        const googleApi = createGoogleIdentityApiMock();
        googleWindow.google = { accounts: { id: googleApi } };

        await expect(service.initializeAsync({ clientId: CLIENT_ID, callback: vi.fn() })).rejects.toThrow(
            'Google Identity Services are only available in the browser',
        );

        service.renderButton(document.createElement('div'));
        service.prompt();
        service.cancel();

        expect(googleApi.renderButton).not.toHaveBeenCalled();
        expect(googleApi.prompt).not.toHaveBeenCalled();
        expect(googleApi.cancel).not.toHaveBeenCalled();
    });
});

function setupBrowserService(theme = signal<AppThemeName>('ocean')): GoogleIdentityService {
    TestBed.configureTestingModule({
        providers: [GoogleIdentityService, { provide: ThemeService, useValue: { theme } }],
    });

    return TestBed.inject(GoogleIdentityService);
}

function setupServerService(): GoogleIdentityService {
    TestBed.configureTestingModule({
        providers: [
            GoogleIdentityService,
            { provide: PLATFORM_ID, useValue: 'server' },
            { provide: RendererFactory2, useValue: createRendererFactoryMock() },
        ],
    });

    return TestBed.inject(GoogleIdentityService);
}

function resetGoogleIdentityTestState(): void {
    TestBed.resetTestingModule();
    document.head.querySelectorAll('script[src="https://accounts.google.com/gsi/client"]').forEach(script => {
        script.remove();
    });
    delete googleWindow.google;
}

function createGoogleIdentityApiMock(): GoogleIdentityApiMock {
    return {
        initialize: vi.fn<GoogleIdentityApi['initialize']>(),
        renderButton: vi.fn<GoogleIdentityApi['renderButton']>(),
        prompt: vi.fn<GoogleIdentityApi['prompt']>(),
        cancel: vi.fn<GoogleIdentityApi['cancel']>(),
    };
}

function createRendererFactoryMock(): { createRenderer: () => Pick<Renderer2, 'appendChild' | 'setAttribute' | 'setProperty'> } {
    return {
        createRenderer: () => ({
            appendChild: vi.fn(),
            setAttribute: vi.fn(),
            setProperty: vi.fn(),
        }),
    };
}

function getGoogleScript(): HTMLScriptElement {
    const script = document.head.querySelector<HTMLScriptElement>('script[src="https://accounts.google.com/gsi/client"]');
    if (script === null) {
        throw new Error('Expected Google Identity script to be appended');
    }

    return script;
}
