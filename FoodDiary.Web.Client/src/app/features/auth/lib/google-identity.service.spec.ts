import { PLATFORM_ID, type Renderer2, RendererFactory2 } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

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

type GoogleIdentityApi = NonNullable<NonNullable<NonNullable<Window['google']>['accounts']>['id']>;

describe('GoogleIdentityService browser integration', () => {
    let googleIdentityApi: GoogleIdentityApiMock;

    beforeEach(() => {
        TestBed.resetTestingModule();
        document.head.querySelectorAll('script[src="https://accounts.google.com/gsi/client"]').forEach(script => {
            script.remove();
        });
        googleIdentityApi = createGoogleIdentityApiMock();
        delete window.google;
    });

    afterEach(() => {
        document.head.querySelectorAll('script[src="https://accounts.google.com/gsi/client"]').forEach(script => {
            script.remove();
        });
        delete window.google;
        vi.restoreAllMocks();
    });

    it('loads Google script and initializes client once script resolves', async () => {
        const service = setupBrowserService();
        const callback = vi.fn();

        const initialized = service.initializeAsync({ clientId: CLIENT_ID, callback });
        const script = getGoogleScript();
        window.google = { accounts: { id: googleIdentityApi } };
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
        window.google = { accounts: { id: googleIdentityApi } };
        getGoogleScript().onload?.(new Event('load'));
        await initialized;

        await service.initializeAsync({ clientId: CLIENT_ID, callback: latestCallback });
        const initializeConfig = googleIdentityApi.initialize.mock.calls.at(-1)?.[0] as {
            callback: (response: { credential?: string | null }) => void;
        };
        initializeConfig.callback({ credential: CREDENTIAL });
        initializeConfig.callback({ credential: '' });
        initializeConfig.callback({ credential: null });

        expect(googleIdentityApi.initialize).toHaveBeenCalledTimes(1);
        expect(firstCallback).not.toHaveBeenCalled();
        expect(latestCallback).toHaveBeenCalledOnce();
        expect(latestCallback).toHaveBeenCalledWith(CREDENTIAL);
    });

    it('renders button and forwards prompt/cancel when Google API is available', () => {
        const service = setupBrowserService();
        const target = document.createElement('div');
        window.google = { accounts: { id: googleIdentityApi } };

        service.renderButton(target, 'filled_blue');
        service.prompt();
        service.cancel();

        expect(googleIdentityApi.renderButton).toHaveBeenCalledWith(
            target,
            expect.objectContaining({
                theme: 'filled_blue',
                shape: 'pill',
            }),
        );
        expect(googleIdentityApi.prompt).toHaveBeenCalledOnce();
        expect(googleIdentityApi.cancel).toHaveBeenCalledOnce();
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
        delete window.google;
    });

    afterEach(() => {
        delete window.google;
    });

    it('throws during initialization and ignores UI methods outside browser platform', async () => {
        const service = setupServerService();
        const googleApi = createGoogleIdentityApiMock();
        window.google = { accounts: { id: googleApi } };

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

function setupBrowserService(): GoogleIdentityService {
    TestBed.configureTestingModule({
        providers: [GoogleIdentityService],
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
