import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { environment } from '../../../../../../environments/environment';
import { GoogleIdentityService } from '../../../../../shared/auth/google-identity.service';
import { AuthGoogleManager } from './auth-google.manager';

let googleIdentityServiceSpy: {
    [Method in 'initializeAsync' | 'prompt' | 'renderButton']: Mock<GoogleIdentityService[Method]>;
};
let translateServiceSpy: {
    getCurrentLang: ReturnType<typeof vi.fn>;
    getFallbackLang: ReturnType<typeof vi.fn>;
    instant: ReturnType<typeof vi.fn>;
};
const originalGoogleClient = environment.googleClientId;

beforeEach(() => {
    environment.googleClientId = 'google-client-id';
    googleIdentityServiceSpy = {
        initializeAsync: vi.fn().mockResolvedValue(void 0),
        prompt: vi.fn(),
        renderButton: vi.fn(),
    };
    translateServiceSpy = {
        getCurrentLang: vi.fn(() => 'en'),
        getFallbackLang: vi.fn(() => 'en'),
        instant: vi.fn((key: string) => key),
    };

    TestBed.configureTestingModule({
        providers: [
            AuthGoogleManager,
            { provide: GoogleIdentityService, useValue: googleIdentityServiceSpy },
            { provide: TranslateService, useValue: translateServiceSpy },
        ],
    });
});

afterEach(() => {
    environment.googleClientId = originalGoogleClient;
});

describe('AuthGoogleManager initialization', () => {
    it('should initialize Google identity and mark manager ready', async () => {
        const manager = TestBed.inject(AuthGoogleManager);

        await manager.initializeAsync(vi.fn());

        expect(googleIdentityServiceSpy.initializeAsync).toHaveBeenCalled();
        expect(googleIdentityServiceSpy.prompt).toHaveBeenCalled();
        expect(manager.ready()).toBe(true);
    });

    it('should keep manager not ready when initialization fails', async () => {
        const manager = TestBed.inject(AuthGoogleManager);
        googleIdentityServiceSpy.initializeAsync.mockRejectedValue(new Error('fail'));

        await manager.initializeAsync(vi.fn());

        expect(manager.ready()).toBe(false);
    });

    it.each([
        { clientId: '', configuration: 'empty' },
        { clientId: undefined, configuration: 'omitted' },
    ])('keeps Google disabled when the client id is $configuration', async ({ clientId }) => {
        environment.googleClientId = clientId;
        const manager = TestBed.inject(AuthGoogleManager);

        await manager.initializeAsync(vi.fn());

        expect(googleIdentityServiceSpy.initializeAsync).not.toHaveBeenCalled();
        expect(googleIdentityServiceSpy.prompt).not.toHaveBeenCalled();
        expect(manager.ready()).toBe(false);
    });
});

describe('AuthGoogleManager render', () => {
    it('should render the button for current mode only', async () => {
        const manager = TestBed.inject(AuthGoogleManager);
        const loginButton = document.createElement('div');
        const registerButton = document.createElement('div');
        loginButton.innerHTML = '<span>old</span>';
        registerButton.innerHTML = '<span>old</span>';
        await manager.initializeAsync(vi.fn());

        manager.renderButton('register', loginButton, registerButton);

        expect(loginButton.innerHTML).toBe('');
        expect(registerButton.innerHTML).toBe('');
        expect(googleIdentityServiceSpy.renderButton).toHaveBeenCalledWith(registerButton, 'filled_blue', 'en');
    });

    it('should render the button with Russian Google locale when Russian UI is active', async () => {
        translateServiceSpy.getCurrentLang.mockReturnValue('ru');
        const manager = TestBed.inject(AuthGoogleManager);
        const loginButton = document.createElement('div');
        await manager.initializeAsync(vi.fn());

        manager.renderButton('login', loginButton, undefined);

        expect(googleIdentityServiceSpy.renderButton).toHaveBeenCalledWith(loginButton, 'filled_blue', 'ru');
    });

    it('waits for the browser provider and an active form before rendering a Google button', async () => {
        const manager = TestBed.inject(AuthGoogleManager);
        const pending = new Subject<void>();
        googleIdentityServiceSpy.initializeAsync.mockReturnValueOnce(firstValueFrom(pending));
        const initialization = manager.initializeAsync(vi.fn());
        await vi.waitFor(() => {
            expect(pending.observed).toBe(true);
        });
        const loginTarget = document.createElement('div');
        manager.renderButton('login', loginTarget, undefined);
        expect(manager.ready()).toBe(false);
        expect(googleIdentityServiceSpy.renderButton).not.toHaveBeenCalled();

        pending.next();
        await initialization;
        expect(manager.ready()).toBe(true);
        // The password reset view can temporarily replace both Google button targets.
        manager.renderButton('login', undefined, undefined);
        expect(googleIdentityServiceSpy.renderButton).not.toHaveBeenCalled();

        const registerTarget = document.createElement('div');
        manager.renderButton('register', undefined, registerTarget);
        expect(googleIdentityServiceSpy.renderButton).toHaveBeenCalledExactlyOnceWith(registerTarget, 'filled_blue', 'en');
    });
});

type NativeResult = { provider: 'google'; result?: { idToken: string } | null };
function nativeGoogle({ hasPlugin = true } = {}): {
    initialize: Mock<(options: { google: { webClientId: string } }) => Promise<void>>;
    login: Mock<(options: { provider: 'google' }) => Promise<NativeResult>>;
} {
    const initialize = vi.fn<(options: { google: { webClientId: string } }) => Promise<void>>().mockResolvedValue(undefined);
    const login = vi.fn<(options: { provider: 'google' }) => Promise<NativeResult>>();
    const original = Object.getOwnPropertyDescriptor(window, 'Capacitor');
    Object.defineProperty(window, 'Capacitor', {
        configurable: true,
        value: { isNativePlatform: (): boolean => true, Plugins: hasPlugin ? { SocialLogin: { initialize, login } } : {} },
    });
    onTestFinished(() => {
        if (original === undefined) {
            Reflect.deleteProperty(window, 'Capacitor');
        } else {
            Object.defineProperty(window, 'Capacitor', original);
        }
    });
    return { initialize, login };
}

function nativeButton(target: HTMLElement): HTMLButtonElement {
    const button = target.querySelector('button');
    if (button === null) {
        throw new Error('Expected the native Google button.');
    }
    return button;
}

describe('AuthGoogleManager native buttons', () => {
    it('renders the appropriate action, allows a retry after cancellation and coalesces rapid clicks', async () => {
        const { initialize, login } = nativeGoogle();
        const manager = TestBed.inject(AuthGoogleManager);
        const callback = vi.fn<(credential: string) => void>();
        await manager.initializeAsync(callback);
        expect(initialize).toHaveBeenCalledExactlyOnceWith({ google: { webClientId: 'google-client-id' } });
        expect(googleIdentityServiceSpy.initializeAsync).not.toHaveBeenCalled();

        const loginTarget = document.createElement('div');
        const registerTarget = document.createElement('div');
        manager.renderButton('login', loginTarget, registerTarget);
        expect(nativeButton(loginTarget).textContent).toBe('AUTH.GOOGLE.LOGIN_BUTTON');
        const original = nativeButton(loginTarget);
        manager.renderButton('register', loginTarget, registerTarget);
        expect(loginTarget.querySelector('button')).toBeNull();
        const button = nativeButton(registerTarget);
        expect(button.textContent).toBe('AUTH.GOOGLE.REGISTER_BUTTON');
        original.click();
        expect(login).not.toHaveBeenCalled();

        const pending = new Subject<NativeResult>();
        const pendingOutcome = firstValueFrom(pending);
        const cancelled = new Subject<NativeResult>();
        const cancelledOutcome = firstValueFrom(cancelled);
        login
            .mockReturnValueOnce(pendingOutcome)
            .mockReturnValueOnce(cancelledOutcome)
            .mockResolvedValueOnce({ provider: 'google', result: { idToken: 'accepted-fixture' } });
        button.click();
        button.click();
        expect(login).toHaveBeenCalledExactlyOnceWith({ provider: 'google' });
        pending.next({ provider: 'google', result: null });
        pending.complete();
        await pendingOutcome;
        expect(callback).not.toHaveBeenCalled();
        button.click();
        const dismissal = new Error('User dismissed Google');
        cancelled.error(dismissal);
        await expect(cancelledOutcome).rejects.toBe(dismissal);
        expect(callback).not.toHaveBeenCalled();
        expect(manager.ready()).toBe(true);
        button.click();
        await vi.waitFor(() => {
            expect(callback).toHaveBeenCalledExactlyOnceWith('accepted-fixture');
        });
    });

    it.each(['native initialization failure', 'missing native plugin', 'server platform'])(
        'uses browser adapter initialization for %s',
        async reason => {
            const { initialize, login } = nativeGoogle({ hasPlugin: reason !== 'missing native plugin' });
            if (reason === 'server platform') {
                TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
            } else if (reason === 'native initialization failure') {
                initialize.mockRejectedValueOnce(new Error('Native initialization unavailable'));
            }
            const manager = TestBed.inject(AuthGoogleManager);
            const callback = vi.fn<(credential: string) => void>();
            await manager.initializeAsync(callback);
            expect(initialize).toHaveBeenCalledTimes(Number(reason === 'native initialization failure'));
            expect(googleIdentityServiceSpy.initializeAsync).toHaveBeenCalledOnce();
            expect(googleIdentityServiceSpy.initializeAsync.mock.calls[0][0].clientId).toBe('google-client-id');
            expect(googleIdentityServiceSpy.prompt).toHaveBeenCalledOnce();
            expect(login).not.toHaveBeenCalled();
        },
    );
});

describe('AuthGoogleManager component lifetime', () => {
    it('drops a native result if the owning injector closes while the provider is open', async () => {
        const { login } = nativeGoogle();
        const manager = TestBed.inject(AuthGoogleManager);
        const pending = new Subject<NativeResult>();
        const pendingOutcome = firstValueFrom(pending);
        login.mockReturnValue(pendingOutcome);
        const callback = vi.fn<(credential: string) => void>();
        await manager.initializeAsync(callback);
        const target = document.createElement('div');
        manager.renderButton('login', target, undefined);
        const button = nativeButton(target);
        button.click();
        expect(login).toHaveBeenCalledOnce();

        TestBed.resetTestingModule();
        pending.next({ provider: 'google', result: { idToken: 'late-fixture' } });
        pending.complete();
        await pendingOutcome;
        button.click();
        expect(login).toHaveBeenCalledOnce();
        expect(callback).not.toHaveBeenCalled();
    });

    it('drops a browser Google callback after its owner closes', async () => {
        const manager = TestBed.inject(AuthGoogleManager);
        const callback = vi.fn<(credential: string) => void>();
        await manager.initializeAsync(callback);
        const browserCallback = googleIdentityServiceSpy.initializeAsync.mock.calls[0][0].callback;
        browserCallback('current-browser-fixture');
        TestBed.resetTestingModule();
        browserCallback('late-browser-fixture');
        expect(callback).toHaveBeenCalledExactlyOnceWith('current-browser-fixture');
    });
});

describe('AuthGoogleManager pending initialization', () => {
    it('does not open Google One Tap when the browser provider becomes ready after its owner closes', async () => {
        const manager = TestBed.inject(AuthGoogleManager);
        const pending = new Subject<void>();
        googleIdentityServiceSpy.initializeAsync.mockReturnValue(firstValueFrom(pending));
        const initialization = manager.initializeAsync(vi.fn());
        await vi.waitFor(() => {
            expect(googleIdentityServiceSpy.initializeAsync).toHaveBeenCalledOnce();
        });

        TestBed.resetTestingModule();
        pending.next();
        await initialization;

        expect(googleIdentityServiceSpy.prompt).not.toHaveBeenCalled();
        expect(manager.ready()).toBe(false);
    });

    it('does not initialize the browser fallback after the owner closes during native initialization', async () => {
        const { initialize } = nativeGoogle();
        const manager = TestBed.inject(AuthGoogleManager);
        const pending = new Subject<void>();
        initialize.mockReturnValue(firstValueFrom(pending));
        const initialization = manager.initializeAsync(vi.fn());
        expect(initialize).toHaveBeenCalledOnce();

        TestBed.resetTestingModule();
        pending.error(new Error('Native initialization unavailable'));
        await initialization;

        expect(googleIdentityServiceSpy.initializeAsync).not.toHaveBeenCalled();
        expect(manager.ready()).toBe(false);
    });
});
