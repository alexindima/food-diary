import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../../services/auth.service';
import { LocalizationService } from '../../../../../shared/i18n/localization.service';
import { MarketingAttributionService } from '../../../../../shared/marketing/marketing-attribution.service';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
import type { AuthResponse } from '../../../models/auth.data';
import { AuthFlowFacade } from './auth-flow.facade';

let authServiceMock: {
    login: ReturnType<typeof vi.fn>;
    loginWithGoogle: ReturnType<typeof vi.fn>;
    linkGoogle: ReturnType<typeof vi.fn>;
    register: ReturnType<typeof vi.fn>;
    requestPasswordReset: ReturnType<typeof vi.fn>;
    restoreAccount: ReturnType<typeof vi.fn>;
};
let localizationServiceMock: {
    applyLanguagePreferenceAsync: ReturnType<typeof vi.fn>;
    getCurrentLanguage: ReturnType<typeof vi.fn>;
    loadApplicationTranslationsAsync: ReturnType<typeof vi.fn>;
};
let marketingAttributionMock: {
    recordSignupCompleted: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    authServiceMock = {
        login: vi.fn(),
        loginWithGoogle: vi.fn(),
        linkGoogle: vi.fn(),
        register: vi.fn(),
        requestPasswordReset: vi.fn(),
        restoreAccount: vi.fn(),
    };
    localizationServiceMock = {
        applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(undefined),
        getCurrentLanguage: vi.fn().mockReturnValue('en'),
        loadApplicationTranslationsAsync: vi.fn().mockResolvedValue(undefined),
    };
    marketingAttributionMock = {
        recordSignupCompleted: vi.fn(),
    };

    TestBed.configureTestingModule({
        providers: [
            AuthFlowFacade,
            { provide: AuthService, useValue: authServiceMock },
            { provide: LocalizationService, useValue: localizationServiceMock },
            { provide: MarketingAttributionService, useValue: marketingAttributionMock },
        ],
    });
});

describe('AuthFlowFacade login', () => {
    it('should load authenticated translations before reporting success', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.login.mockReturnValue(of(createAuthResponse('ru')));

        await expect(firstValueFrom(facade.login({ email: 'user@example.com', password: 'password' }))).resolves.toBe('success');
        expect(authServiceMock.login).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ rememberMe: false }));
        expect(localizationServiceMock.applyLanguagePreferenceAsync).toHaveBeenCalledWith('ru');
        expect(localizationServiceMock.loadApplicationTranslationsAsync).toHaveBeenCalledOnce();
        expect(localizationServiceMock.applyLanguagePreferenceAsync.mock.invocationCallOrder[0]).toBeLessThan(
            localizationServiceMock.loadApplicationTranslationsAsync.mock.invocationCallOrder[0],
        );
    });

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

    it('should record signup attribution after successful registration', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.register.mockReturnValue(of(createAuthResponse('en')));

        await expect(firstValueFrom(facade.register({ email: 'new@example.com', password: 'password' }))).resolves.toBe('success');

        expect(marketingAttributionMock.recordSignupCompleted).toHaveBeenCalledWith();
    });
});

describe('AuthFlowFacade simple flows', () => {
    it('should return success for successful Google login and password reset request', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.loginWithGoogle.mockReturnValue(of(createAuthResponse('en')));
        authServiceMock.requestPasswordReset.mockReturnValue(of({}));

        await expect(firstValueFrom(facade.loginWithGoogle({ credential: 'credential', rememberMe: true }))).resolves.toBe('success');
        await expect(firstValueFrom(facade.requestPasswordReset({ email: 'user@example.com' }))).resolves.toBe('success');
    });

    it('should preserve the Google account-link-required outcome', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.loginWithGoogle.mockReturnValue(throwError(() => createApiError('Authentication.GoogleAccountLinkRequired')));

        await expect(firstValueFrom(facade.loginWithGoogle({ credential: 'credential' }))).resolves.toBe('accountLinkRequired');
    });

    it('should report authenticated Google linking outcome', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.linkGoogle.mockReturnValue(of(undefined));

        await expect(firstValueFrom(facade.linkGoogle('credential'))).resolves.toBe(true);
    });

    it('should return false for failed restore', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.restoreAccount.mockReturnValue(throwError(() => new Error('fail')));

        await expect(firstValueFrom(facade.restoreAccount({ email: 'user@example.com', password: 'password' }, true))).resolves.toBe(false);
    });

    it('should load authenticated translations after restore', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.restoreAccount.mockReturnValue(of(createAuthResponse('ru')));

        await expect(firstValueFrom(facade.restoreAccount({ email: 'user@example.com', password: 'password' }, true))).resolves.toBe(true);
        expect(localizationServiceMock.applyLanguagePreferenceAsync).toHaveBeenCalledWith('ru');
        expect(localizationServiceMock.loadApplicationTranslationsAsync).toHaveBeenCalledOnce();
    });
});

function createApiError(error: string): unknown {
    return { error: { error } };
}

function createAuthResponse(language: string): AuthResponse {
    return {
        accessToken: 'access-token',
        user: {
            id: entityId<'user'>('user-id'),
            email: 'user@example.com',
            hasPassword: true,
            language,
            pushNotificationsEnabled: false,
            fastingPushNotificationsEnabled: false,
            socialPushNotificationsEnabled: false,
            fastingCheckInReminderHours: 0,
            fastingCheckInFollowUpReminderHours: 0,
            isActive: true,
            isEmailConfirmed: true,
        },
    };
}

describe('Auth rate limits', () => {
    it('preserves password reset throttling without retrying the request', async () => {
        authServiceMock.requestPasswordReset.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 429 })));
        const facade = TestBed.inject(AuthFlowFacade);

        expect(await firstValueFrom(facade.requestPasswordReset({ email: 'user@example.com' }))).toBe('rateLimited');
        expect(authServiceMock.requestPasswordReset).toHaveBeenCalledOnce();
    });

    it('keeps other password reset errors generic', async () => {
        authServiceMock.requestPasswordReset.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
        const facade = TestBed.inject(AuthFlowFacade);

        expect(await firstValueFrom(facade.requestPasswordReset({ email: 'user@example.com' }))).toBe('unknown');
    });

    it('distinguishes throttling from invalid credentials without retrying automatically', async () => {
        authServiceMock.login.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 429 })));
        const facade = TestBed.inject(AuthFlowFacade);
        expect(await firstValueFrom(facade.login({ email: 'user@example.com', password: 'password' }))).toBe('rateLimited');
        expect(authServiceMock.login).toHaveBeenCalledOnce();
    });
});

describe('Sign-in rejection details', () => {
    it.each([
        { code: 'Authentication.ReauthenticationAccountMismatch', expected: 'accountMismatch' },
        { code: 'User.InvalidCredentials', expected: 'invalidCredentials' },
    ])('keeps $code distinct from other sign-in failures', async ({ code, expected }) => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.login.mockReturnValue(throwError(() => createApiError(code)));
        const owner = entityId<'user'>('expected-account');

        const result = await firstValueFrom(
            facade.login({ email: 'user@example.com', password: 'fixture-password', expectedUserId: owner }),
        );

        expect(result).toBe(expected);
        expect(authServiceMock.login).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ expectedUserId: owner }));
        expect(localizationServiceMock.loadApplicationTranslationsAsync).not.toHaveBeenCalled();
    });

    it('reports a gateway response without an API error body as a general sign-in failure', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.login.mockReturnValue(
            throwError(() => new HttpErrorResponse({ status: HttpStatusCode.BadGateway, error: 'Gateway unavailable' })),
        );

        const result = await firstValueFrom(facade.login({ email: 'user@example.com', password: 'fixture-password' }));

        expect(result).toBe('unknown');
        expect(authServiceMock.login).toHaveBeenCalledOnce();
        expect(localizationServiceMock.applyLanguagePreferenceAsync).not.toHaveBeenCalled();
    });
});

describe('Registration rejection details', () => {
    it.each([
        { failure: createApiError('User.EmailAlreadyExists'), expected: 'emailExists' },
        { failure: createApiError('Authentication.AccountDeleted'), expected: 'accountDeleted' },
        { failure: new HttpErrorResponse({ status: HttpStatusCode.TooManyRequests }), expected: 'rateLimited' },
        { failure: new HttpErrorResponse({ status: HttpStatusCode.ServiceUnavailable }), expected: 'unknown' },
    ])('reports $expected while keeping signup conversion tied to a successful registration', async ({ failure, expected }) => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.register.mockReturnValue(throwError(() => failure));

        const result = await firstValueFrom(facade.register({ email: 'user@example.com', password: 'fixture-password' }));

        expect(result).toBe(expected);
        expect(authServiceMock.register).toHaveBeenCalledOnce();
        expect(marketingAttributionMock.recordSignupCompleted).not.toHaveBeenCalled();
    });
});

describe('Google confirmation failures', () => {
    it.each([
        { failure: createApiError('Authentication.ReauthenticationAccountMismatch'), expected: 'accountMismatch' },
        { failure: new HttpErrorResponse({ status: HttpStatusCode.ServiceUnavailable }), expected: 'unknown' },
    ])('reports $expected without loading translations or linking a different account', async ({ failure, expected }) => {
        const facade = TestBed.inject(AuthFlowFacade);
        const request = { credential: 'fixture-credential', expectedUserId: entityId<'user'>('expected-account') };
        authServiceMock.loginWithGoogle.mockReturnValue(throwError(() => failure));

        const result = await firstValueFrom(facade.loginWithGoogle(request));

        expect(result).toBe(expected);
        expect(authServiceMock.loginWithGoogle).toHaveBeenCalledExactlyOnceWith(request);
        expect(authServiceMock.linkGoogle).not.toHaveBeenCalled();
        expect(localizationServiceMock.loadApplicationTranslationsAsync).not.toHaveBeenCalled();
    });

    it('reports a failed link so the profile can keep its existing provider connection', async () => {
        const facade = TestBed.inject(AuthFlowFacade);
        authServiceMock.linkGoogle.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));

        await expect(firstValueFrom(facade.linkGoogle('fixture-credential'))).resolves.toBe(false);
        expect(authServiceMock.linkGoogle).toHaveBeenCalledExactlyOnceWith('fixture-credential');
    });
});
