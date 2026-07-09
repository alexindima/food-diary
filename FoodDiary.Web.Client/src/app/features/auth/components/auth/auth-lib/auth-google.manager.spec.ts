import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../../../environments/environment';
import { GoogleIdentityService } from '../../../lib/google-identity.service';
import { AuthGoogleManager } from './auth-google.manager';

let googleIdentityServiceSpy: {
    initializeAsync: ReturnType<typeof vi.fn>;
    prompt: ReturnType<typeof vi.fn>;
    renderButton: ReturnType<typeof vi.fn>;
};
let translateServiceSpy: {
    getCurrentLang: ReturnType<typeof vi.fn>;
    getFallbackLang: ReturnType<typeof vi.fn>;
    instant: ReturnType<typeof vi.fn>;
};

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

    it('should keep manager not ready when Google client id is not configured', async () => {
        environment.googleClientId = '';
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
});
