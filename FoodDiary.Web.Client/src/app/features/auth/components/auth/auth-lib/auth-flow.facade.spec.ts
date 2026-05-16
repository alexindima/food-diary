import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../../services/auth.service';
import { LocalizationService } from '../../../../../services/localization.service';
import { AuthFlowFacade } from './auth-flow.facade';

let authServiceMock: {
    login: ReturnType<typeof vi.fn>;
    loginWithGoogle: ReturnType<typeof vi.fn>;
    register: ReturnType<typeof vi.fn>;
    requestPasswordReset: ReturnType<typeof vi.fn>;
    restoreAccount: ReturnType<typeof vi.fn>;
};
let localizationServiceMock: {
    getCurrentLanguage: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    authServiceMock = {
        login: vi.fn(),
        loginWithGoogle: vi.fn(),
        register: vi.fn(),
        requestPasswordReset: vi.fn(),
        restoreAccount: vi.fn(),
    };
    localizationServiceMock = {
        getCurrentLanguage: vi.fn().mockReturnValue('en'),
    };

    TestBed.configureTestingModule({
        providers: [
            AuthFlowFacade,
            { provide: AuthService, useValue: authServiceMock },
            { provide: LocalizationService, useValue: localizationServiceMock },
        ],
    });
});

describe('AuthFlowFacade login', () => {
    it('should map invalid credentials error', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.login.mockReturnValue(throwError(() => createApiError('Authentication.InvalidCredentials')));

        await expect(firstValueFrom(facade.login({ email: 'user@example.com', password: 'wrong' }))).resolves.toBe('invalidCredentials');
    });

    it('should map deleted account error', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.login.mockReturnValue(throwError(() => createApiError('Authentication.AccountDeleted')));

        await expect(firstValueFrom(facade.login({ email: 'user@example.com', password: 'password' }))).resolves.toBe('accountDeleted');
    });
});

describe('AuthFlowFacade register', () => {
    it('should add current language to register request', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.register.mockReturnValue(throwError(() => createApiError('Validation.Conflict')));

        await expect(firstValueFrom(facade.register({ email: 'taken@example.com', password: 'password' }))).resolves.toBe('emailExists');
        expect(authServiceMock.register).toHaveBeenCalledWith(expect.objectContaining({ language: 'en' }));
    });
});

describe('AuthFlowFacade simple flows', () => {
    it('should return true for successful Google login and password reset request', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.loginWithGoogle.mockReturnValue(of({}));
        authServiceMock.requestPasswordReset.mockReturnValue(of({}));

        await expect(firstValueFrom(facade.loginWithGoogle({ credential: 'credential', rememberMe: true }))).resolves.toBe(true);
        await expect(firstValueFrom(facade.requestPasswordReset({ email: 'user@example.com' }))).resolves.toBe(true);
    });

    it('should return false for failed restore', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.restoreAccount.mockReturnValue(throwError(() => new Error('fail')));

        await expect(firstValueFrom(facade.restoreAccount({ email: 'user@example.com', password: 'password' }, true))).resolves.toBe(false);
    });
});

function createApiError(error: string): unknown {
    return { error: { error } };
}
