import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { GoogleIdentityService } from '../../../lib/google-identity.service';
import { AuthGoogleManager } from './auth-google.manager';

let googleIdentityServiceSpy: {
    initializeAsync: ReturnType<typeof vi.fn>;
    prompt: ReturnType<typeof vi.fn>;
    renderButton: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    googleIdentityServiceSpy = {
        initializeAsync: vi.fn().mockResolvedValue(undefined),
        prompt: vi.fn(),
        renderButton: vi.fn(),
    };

    TestBed.configureTestingModule({
        providers: [AuthGoogleManager, { provide: GoogleIdentityService, useValue: googleIdentityServiceSpy }],
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
        expect(googleIdentityServiceSpy.renderButton).toHaveBeenCalledWith(registerButton, 'filled_blue');
    });
});
