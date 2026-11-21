import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { createEnvironmentInjector, DestroyRef, EnvironmentInjector, PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, type Observable, Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import { environment } from '../../environments/environment';
import { waitForAsyncTasksAsync } from '../../testing/async-testing';
import { authResponseFromSdk } from '../shared/api/sdk/auth-sdk.mapper';
import type { AuthenticationHttpResponse } from '../shared/api/sdk/generated/model/authentication-http-response';
import type { TelegramDiaryTransferHttpResponse } from '../shared/api/sdk/generated/model/telegram-diary-transfer-http-response';
import {
    ConfirmPasswordResetRequest,
    LoginRequest,
    PasswordResetRequest,
    RegisterRequest,
    RestoreAccountRequest,
    type TelegramDisconnectConfirmation,
} from '../shared/auth/auth.data';
import { ACCOUNT_ACCESS_STORAGE_KEY } from '../shared/auth/auth-metadata-storage.service';
import { AuthRecoveryStateService } from '../shared/auth/auth-recovery-state.service';
import { BrowserSessionStateService } from '../shared/auth/browser-session-state.service';
import { EmailVerificationCooldownService } from '../shared/auth/email-verification-cooldown.service';
import { SessionEventsService } from '../shared/auth/session-events.service';
import { telegramOAuthCode, telegramOAuthState } from '../shared/auth/telegram-oidc-values';
import { TelegramWebAppService } from '../shared/auth/telegram-web-app.service';
import { LocalizationService } from '../shared/i18n/localization.service';
import { entityId } from '../shared/models/semantics/entity-id';
import { AuthService } from './auth.service';
import { NavigationService } from './navigation.service';

const base = environment.apiUrls.auth;
const SESSION_REQUEST_DEADLINE_MS = 30_000;
const sessionId = 'dd281aa8-5d85-4fda-bc28-a7e1fd6085db';
const response = {
    sessionId,
    roles: ['User'],
    user: {
        id: 'user-123',
        email: 'test@example.com',
        isActive: true,
        isEmailConfirmed: true,
        hasPassword: true,
        pushNotificationsEnabled: false,
        fastingPushNotificationsEnabled: false,
        socialPushNotificationsEnabled: false,
        fastingCheckInReminderHours: 12,
        fastingCheckInFollowUpReminderHours: 20,
        language: 'en',
    },
};
const login = new LoginRequest({ email: response.user.email, password: 'test-password', rememberMe: true });
let service: AuthService;
let requests: HttpTestingController;
let navigation: {
    navigateToAuthAsync: Mock<NavigationService['navigateToAuthAsync']>;
    navigateToLandingAsync: Mock<NavigationService['navigateToLandingAsync']>;
};

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/');
    navigation = {
        navigateToAuthAsync: vi.fn<NavigationService['navigateToAuthAsync']>().mockResolvedValue(undefined),
        navigateToLandingAsync: vi.fn<NavigationService['navigateToLandingAsync']>().mockResolvedValue(undefined),
    };
    configureAuthTesting();
});

function configureAuthTesting(platform: 'browser' | 'server' = 'browser'): void {
    TestBed.configureTestingModule({
        providers: [
            { provide: PLATFORM_ID, useValue: platform },
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: NavigationService, useValue: navigation },
            {
                provide: LocalizationService,
                useValue: { applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(undefined), clearStoredLanguage: vi.fn() },
            },
        ],
    });
    service = TestBed.inject(AuthService);
    requests = TestBed.inject(HttpTestingController);
}

afterEach(() => {
    requests.verify();
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/');
});

function signIn(): void {
    service.login(login).subscribe();
    requests.expectOne(`${base}/login`).flush(response);
}

describe('Authentication during server rendering', () => {
    it('marks prerendered pages ready without restoring a browser session or redirecting', async () => {
        TestBed.resetTestingModule();
        configureAuthTesting('server');

        await service.ensureSessionReadyAsync();

        expect(service.isAuthReady()).toBe(true);
        expect(service.isAuthenticated()).toBe(false);
        expect(service.getUserId()).toBeNull();
        requests.expectNone({ method: 'POST', url: `${base}/session` });
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
    });
});

describe('Authentication restoration after a stalled request', () => {
    it('aborts a timed-out HTTP restoration and shares the successful retry across callers', async () => {
        vi.useFakeTimers();
        try {
            const restoring = service.ensureSessionReadyAsync();
            const pending = requests.expectOne({ method: 'POST', url: `${base}/session` });
            await vi.advanceTimersByTimeAsync(SESSION_REQUEST_DEADLINE_MS);
            await restoring;
            expect(pending.cancelled).toBe(true);
            expect(service.isAuthReady()).toBe(false);
            expect(TestBed.inject(AuthRecoveryStateService).issue()).toBe('restore');

            const retry = service.ensureSessionReadyAsync();
            const concurrent = service.ensureSessionReadyAsync();
            requests.expectOne({ method: 'POST', url: `${base}/session` }).flush(response);
            await Promise.all([retry, concurrent]);

            expect(service.isAuthReady()).toBe(true);
            expect(service.getUserId()).toBe(response.user.id);
            expect(TestBed.inject(AuthRecoveryStateService).issue()).toBeNull();
        } finally {
            vi.useRealTimers();
        }
    });

    it.each([HttpStatusCode.Unauthorized, HttpStatusCode.ServiceUnavailable])(
        'keeps a newer account ready when the old session returns %s',
        async status => {
            signIn();
            const renewal = firstValueFrom(service.refreshSession());
            const previous = requests.expectOne({ method: 'POST', url: `${base}/session` });
            const replacement = { ...response, user: { ...response.user, id: 'new-account' }, sessionId: 'new-session' };
            service.acceptExternalAuthentication(authResponseFromSdk(replacement));

            previous.flush(null, { status, statusText: 'Old session response' });

            await expect(renewal).resolves.toBe(false);
            expect(service.getUserId()).toBe(replacement.user.id);
            expect(service.getSessionKey()).toBe(replacement.sessionId);
            expect(TestBed.inject(AuthRecoveryStateService).issue()).toBeNull();
            expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
        },
    );
});

describe('Admin sign-in handoff', () => {
    it('returns the issued code and expiry from the generated client', async () => {
        const handoff = { code: 'test-handoff', expiresAtUtc: '2026-05-17T10:00:00Z' };
        const request = firstValueFrom(service.startAdminSso());
        requests.expectOne({ method: 'POST', url: `${base}/admin-sso/start` }).flush(handoff);

        await expect(request).resolves.toEqual(handoff);
    });

    it('rejects an incomplete handoff that cannot be used for navigation', async () => {
        const request = firstValueFrom(service.startAdminSso());
        requests.expectOne({ method: 'POST', url: `${base}/admin-sso/start` }).flush({ code: 'test-handoff' });

        await expect(request).rejects.toThrow('expiresAtUtc');
    });
});

describe('Required password renewal ownership', () => {
    it('exits before retrying sign-in when the current session email is gone', async () => {
        signIn();
        const logout = service.onLogoutAsync();
        requests.expectOne({ method: 'POST', url: `${base}/logout` }).flush(null);
        await logout;

        await expect(service.completeRequiredPasswordChangeAsync('test-updated-password')).rejects.toThrow('Missing session email');
        requests.expectNone({ method: 'POST', url: `${base}/login` });
    });

    it('completes logout after an already sent password sign-in receives its response', async () => {
        signIn();
        const renewal = service.completeRequiredPasswordChangeAsync('test-updated-password').catch((error: unknown) => error);
        const pendingLogin = requests.expectOne({ method: 'POST', url: `${base}/login` });

        const logout = service.onLogoutAsync(true);
        expect(service.isAuthenticated()).toBe(false);
        requests.expectNone({ method: 'POST', url: `${base}/logout` });
        pendingLogin.flush({ ...response, sessionId: 'renewed-session' });
        requests.expectOne({ method: 'POST', url: `${base}/logout` }).flush(null);
        const result = await renewal;
        await logout;

        expect(service.isAuthenticated()).toBe(false);
        expect(service.getUserId()).toBeNull();
        expect(navigation.navigateToAuthAsync).toHaveBeenCalledExactlyOnceWith('login');
        expect(result).toEqual(new Error('Authentication session changed'));
    });

    it('does not send a queued password sign-in after the user has signed out', async () => {
        signIn();
        const refresh = firstValueFrom(service.refreshSession());
        const pendingRefresh = requests.expectOne({ method: 'POST', url: `${base}/session` });
        const renewal = service.completeRequiredPasswordChangeAsync('test-updated-password').catch((error: unknown) => error);
        const logout = service.onLogoutAsync(true);
        expect(service.isAuthenticated()).toBe(false);
        pendingRefresh.flush(response);

        requests.expectNone({ method: 'POST', url: `${base}/login` });
        requests.expectOne({ method: 'POST', url: `${base}/logout` }).flush(null);
        await expect(refresh).resolves.toBe(false);
        expect(await renewal).toEqual(new Error('Authentication session changed'));
        await logout;
        expect(service.isAuthenticated()).toBe(false);
        expect(navigation.navigateToAuthAsync).toHaveBeenCalledExactlyOnceWith('login');
    });

    it('keeps a replacement account when an older required-password renewal succeeds', async () => {
        signIn();
        const renewal = service.completeRequiredPasswordChangeAsync('test-updated-password').catch((error: unknown) => error);
        const pendingLogin = requests.expectOne({ method: 'POST', url: `${base}/login` });
        const replacement = { ...response, user: { ...response.user, id: 'new-account' }, sessionId: 'new-session' };
        service.acceptExternalAuthentication(authResponseFromSdk(replacement));
        pendingLogin.flush({ ...response, sessionId: 'renewed-session' });
        const result = await renewal;

        expect(service.getUserId()).toBe(replacement.user.id);
        expect(service.getSessionKey()).toBe(replacement.sessionId);
        expect(result).toEqual(new Error('Authentication session changed'));
    });
});

describe('email verification requests and session ownership', () => {
    const unconfirmed = { ...response, user: { ...response.user, isEmailConfirmed: false } };

    it('registers with the browser origin and marks the verification message sent for the new account', async () => {
        const credentials = new RegisterRequest({ email: response.user.email, password: login.password, language: 'en' });
        const registered = firstValueFrom(service.register(credentials));
        const request = requests.expectOne({ method: 'POST', url: `${base}/register` });
        expect(request.request.body).toEqual({
            email: credentials.email,
            password: credentials.password,
            language: 'en',
            clientOrigin: window.location.origin,
        });
        request.flush(unconfirmed);
        await registered;
        expect(service.isAuthenticated()).toBe(true);
        expect(service.requiresEmailVerification()).toBe(true);
        expect(TestBed.inject(EmailVerificationCooldownService).remainingSeconds(response.user.id)).toBeGreaterThan(0);
    });

    it('updates the email state for the same session and ignores a verification that finishes after a new login', async () => {
        service.acceptExternalAuthentication(authResponseFromSdk(unconfirmed));
        const current = firstValueFrom(service.verifyEmail(response.user.id, 'verification-fixture'));
        const verification = requests.expectOne({ method: 'POST', url: `${base}/verify-email` });
        expect(verification.request.body).toEqual({ userId: response.user.id, token: 'verification-fixture' });
        verification.flush(null);
        await current;
        expect(service.isEmailConfirmed()).toBe(true);

        const late = firstValueFrom(service.verifyEmail(response.user.id, 'repeat-verification-fixture'));
        const pending = requests.expectOne(`${base}/verify-email`);
        service.acceptExternalAuthentication(authResponseFromSdk({ ...unconfirmed, sessionId: 'replacement-session' }));
        pending.flush(null);
        await late;
        expect(service.getSessionKey()).toBe('replacement-session');
        expect(service.requiresEmailVerification()).toBe(true);
    });

    it('keeps the resend cooldown bound to the account that sent the request', async () => {
        service.acceptExternalAuthentication(authResponseFromSdk(unconfirmed));
        const resend = firstValueFrom(service.resendEmailVerification());
        const pending = requests.expectOne({ method: 'POST', url: `${base}/verify-email/resend` });
        expect(pending.request.body).toEqual({ clientOrigin: window.location.origin });
        const another = { ...unconfirmed.user, id: 'replacement-account' };
        service.acceptExternalAuthentication(authResponseFromSdk({ ...unconfirmed, sessionId: 'replacement-session', user: another }));
        pending.flush(null);
        await resend;

        const cooldown = TestBed.inject(EmailVerificationCooldownService);
        expect(cooldown.remainingSeconds(response.user.id)).toBeGreaterThan(0);
        expect(cooldown.remainingSeconds(another.id)).toBe(0);
    });
});

it('sends a password reset link request and accepts the fresh cookie returned by the confirmation', async () => {
    const requested = firstValueFrom(service.requestPasswordReset(new PasswordResetRequest({ email: response.user.email })));
    const link = requests.expectOne({ method: 'POST', url: `${base}/password-reset/request` });
    expect(link.request.body).toEqual({ email: response.user.email, clientOrigin: window.location.origin });
    link.flush(null);
    await requested;
    expect(service.isAuthenticated()).toBe(false);

    const change = new ConfirmPasswordResetRequest({ userId: response.user.id, token: 'reset-fixture', newPassword: 'new-fixture' });
    const confirmed = firstValueFrom(service.confirmPasswordReset(change));
    const request = requests.expectOne({ method: 'POST', url: `${base}/password-reset/confirm` });
    expect(request.request.body).toEqual(change);
    request.flush(response);
    await confirmed;
    expect(service.getSessionKey()).toBe(sessionId);
    expect(service.isAuthenticated()).toBe(true);
});

describe('Authentication HTTP outcomes', () => {
    it.each([
        { endpoint: 'login', status: HttpStatusCode.Unauthorized, start: (): Observable<unknown> => service.login(login) },
        {
            endpoint: 'register',
            status: HttpStatusCode.Conflict,
            start: (): Observable<unknown> =>
                service.register(new RegisterRequest({ email: response.user.email, password: login.password, language: 'en' })),
        },
        {
            endpoint: 'verify-email',
            status: HttpStatusCode.BadRequest,
            start: (): Observable<unknown> => service.verifyEmail(response.user.id, 'expired-verification-fixture'),
        },
        {
            endpoint: 'verify-email/resend',
            status: HttpStatusCode.TooManyRequests,
            start: (): Observable<unknown> => service.resendEmailVerification(),
        },
        {
            endpoint: 'restore',
            status: HttpStatusCode.Unauthorized,
            start: (): Observable<unknown> =>
                service.restoreAccount(new RestoreAccountRequest({ email: response.user.email, password: login.password })),
        },
        {
            endpoint: 'google',
            status: HttpStatusCode.Conflict,
            start: (): Observable<unknown> => service.loginWithGoogle({ credential: 'google-fixture', rememberMe: true }),
        },
        {
            endpoint: 'password-reset/request',
            status: HttpStatusCode.TooManyRequests,
            start: (): Observable<unknown> => service.requestPasswordReset(new PasswordResetRequest({ email: response.user.email })),
        },
        {
            endpoint: 'password-reset/confirm',
            status: HttpStatusCode.BadRequest,
            start: (): Observable<unknown> =>
                service.confirmPasswordReset(
                    new ConfirmPasswordResetRequest({
                        userId: response.user.id,
                        token: 'expired-reset-fixture',
                        newPassword: 'new-fixture',
                    }),
                ),
        },
    ] satisfies Array<{ endpoint: string; status: HttpStatusCode; start: () => Observable<unknown> }>)(
        'preserves the session and reports the server rejection from $endpoint',
        async ({ endpoint, status, start }) => {
            const existing = { ...response, user: { ...response.user, isEmailConfirmed: false } };
            service.acceptExternalAuthentication(authResponseFromSdk(existing));
            const log = vi.spyOn(console, 'error').mockImplementation(() => {});
            const payload = { message: 'Request rejected by the server' };

            try {
                const rejected = expect(firstValueFrom(start())).rejects.toMatchObject({ status, error: payload });
                requests
                    .expectOne({ method: 'POST', url: `${base}/${endpoint}` })
                    .flush(payload, { status, statusText: 'Request rejected' });
                await rejected;
                expect(service.getSessionKey()).toBe(sessionId);
                expect(service.requiresEmailVerification()).toBe(true);
                expect(TestBed.inject(EmailVerificationCooldownService).remainingSeconds(response.user.id)).toBe(0);
            } finally {
                log.mockRestore();
            }
        },
    );
});

describe('Google account linking', () => {
    it('maps the linked Google profile and retains the current browser session', async () => {
        signIn();
        const result = firstValueFrom(service.linkGoogle('new-google-link-fixture'));
        const request = requests.expectOne({ method: 'POST', url: `${base}/google/link` });
        expect(request.request.body).toEqual({ credential: 'new-google-link-fixture' });
        request.flush({ ...response.user, username: 'Updated profile', hasGoogleIdentity: true });

        await expect(result).resolves.toMatchObject({ id: response.user.id, username: 'Updated profile', hasGoogleIdentity: true });
        expect(service.getSessionKey()).toBe(sessionId);
        expect(service.getUserId()).toBe(response.user.id);
    });

    it('returns a Google identity conflict while keeping the signed-in account', async () => {
        signIn();
        const log = vi.spyOn(console, 'error').mockImplementation(() => {});
        const payload = { error: 'Authentication.GoogleIdentityAlreadyLinked' };
        const credential = 'google-identity-already-linked-fixture';

        try {
            const rejected = expect(firstValueFrom(service.linkGoogle(credential))).rejects.toMatchObject({
                status: HttpStatusCode.Conflict,
                error: payload,
            });
            const request = requests.expectOne({ method: 'POST', url: `${base}/google/link` });
            expect(request.request.body).toEqual({ credential });
            request.flush(payload, { status: HttpStatusCode.Conflict, statusText: 'Conflict' });
            await rejected;

            expect(service.isAuthenticated()).toBe(true);
            expect(service.getSessionKey()).toBe(sessionId);
            expect(service.getUserId()).toBe(response.user.id);
            expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
        } finally {
            log.mockRestore();
        }
    });
});

describe('server-backed browser sessions', () => {
    it.each([
        ['/?view=food#impersonationCode=one-time-code&tab=notes', '?view=food', '#tab=notes'],
        ['/?view=food&impersonationCode=one-time-code#tab=notes', '?view=food', '#tab=notes'],
        ['/?impersonationCode=stale-code#impersonationCode=one-time-code', '', ''],
    ])('exchanges an impersonation handoff and cleans its URL before sending it: %s', async (url, search, hash) => {
        window.history.replaceState({}, '', url);
        const restored = service.restoreSessionAsync();
        const exchange = requests.expectOne(`${base}/impersonation/exchange`);
        expect(exchange.request.body).toEqual({ code: 'one-time-code' });
        expect(window.location.search).toBe(search);
        expect(window.location.hash).toBe(hash);
        exchange.flush({ contextId: 'context-one' });
        await vi.waitFor(() => {
            requests.expectOne(`${base}/session`).flush({ ...response, sessionId: null, impersonationContext: 'context-one' });
        });
        await restored;
        expect(service.isAuthenticated()).toBe(true);
        expect(service.isImpersonating()).toBe(true);
        expect(sessionStorage.getItem('impersonationContext')).toBe('context-one');
    });

    it('starts as a guest and accepts server session metadata without a bearer credential', () => {
        expect(service.isAuthenticated()).toBe(false);
        signIn();
        expect(service.isAuthenticated()).toBe(true);
        expect(service.getUserId()).toBe('user-123');
        expect(service.getSessionKey()).toBe(sessionId);
        expect(service.getToken()).toBeNull();
        for (const storage of [localStorage, sessionStorage]) {
            expect(storage.getItem('authToken')).toBeNull();
            expect(storage.getItem('refreshToken')).toBeNull();
        }
    });

    it('restores the HttpOnly session from the API after a reload', async () => {
        const restored = service.restoreSessionAsync();
        const request = requests.expectOne(`${base}/session`);
        expect(request.request.method).toBe('POST');
        request.flush(response);
        await restored;
        expect(service.isAuthenticated()).toBe(true);
        expect(service.isAuthReady()).toBe(true);
        expect(service.getToken()).toBeNull();
    });
});

describe('anonymous browser session restoration', () => {
    it('completes a cookie-free restoration as a ready guest', async () => {
        const restored = service.restoreSessionAsync();
        requests.expectOne(`${base}/session`).flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await restored;
        expect(service.isAuthReady(), 'An anonymous 204 must complete guest initialization').toBe(true);
        expect(service.isAuthenticated()).toBe(false);
        expect(service.getUserId()).toBeNull();
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
        await service.restoreSessionAsync();
        requests.expectNone(`${base}/session`);
    });

    it('ends a known session when restoration returns no session', async () => {
        signIn();
        const restored = firstValueFrom(service.refreshSession());
        requests.expectOne(`${base}/session`).flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        expect(await restored).toBe(false);
        expect(service.isAuthenticated(), 'An anonymous 204 must remove stale authenticated state').toBe(false);
        expect(service.isAuthReady()).toBe(true);
        expect(navigation.navigateToAuthAsync).toHaveBeenCalled();
    });

    it('does not clear a newer login when an old anonymous restoration completes', async () => {
        const restored = service.restoreSessionAsync();
        const request = requests.expectOne(`${base}/session`);
        service.acceptExternalAuthentication(authResponseFromSdk({ ...response, sessionId: 'new-session' }));
        request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await restored;
        expect(service.isAuthenticated()).toBe(true);
        expect(service.getSessionKey()).toBe('new-session');
    });

    it('keeps an empty successful metadata response retryable instead of accepting it as a guest', async () => {
        const restored = service.restoreSessionAsync();
        requests.expectOne(`${base}/session`).flush(null, { status: HttpStatusCode.Ok, statusText: 'OK' });
        await restored;
        expect(service.isAuthReady()).toBe(false);
        expect(service.isAuthenticated()).toBe(false);
    });

    it('does not authenticate a returned profile without session binding and accepts a complete retry', async () => {
        const recovery = TestBed.inject(AuthRecoveryStateService);
        const unbound = { user: response.user, roles: response.roles } satisfies AuthenticationHttpResponse;
        const restored = service.restoreSessionAsync();
        requests.expectOne({ method: 'POST', url: `${base}/session` }).flush(unbound);
        await restored;

        expect(recovery.issue()).toBe('restore');
        expect(service.isAuthReady()).toBe(false);
        expect(service.isAuthenticated()).toBe(false);
        expect(service.getUserId()).toBeNull();
        expect(service.getSessionKey()).toBeNull();
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();

        const retry = service.restoreSessionAsync();
        requests.expectOne({ method: 'POST', url: `${base}/session` }).flush(response);
        await retry;

        expect(service.isAuthReady()).toBe(true);
        expect(service.isAuthenticated()).toBe(true);
        expect(service.getUserId()).toBe(response.user.id);
        expect(service.getSessionKey()).toBe(sessionId);
        expect(recovery.issue()).toBeNull();
    });
});

describe('server-backed browser session isolation', () => {
    it('coalesces simultaneous session checks and obtains updated roles from the server', async () => {
        signIn();
        const first = firstValueFrom(service.refreshSession());
        const second = firstValueFrom(service.refreshSession());
        requests.expectOne(`${base}/session`).flush({ ...response, roles: ['User', 'Premium'] });
        expect(await first).toBe(true);
        expect(await second).toBe(true);
        expect(service.isPremium()).toBe(true);
    });

    it('does not apply an old response to a newer login', async () => {
        signIn();
        const pending = firstValueFrom(service.refreshSession());
        const old = requests.expectOne(`${base}/session`);
        service.acceptExternalAuthentication(authResponseFromSdk({ ...response, sessionId: 'new-session' }));
        old.flush(response);
        expect(await pending).toBe(false);
        expect(service.isAuthenticated()).toBe(true);
        expect(service.getSessionKey()).toBe('new-session');
    });

    it('rejects a session response for another account instead of silently changing request ownership', async () => {
        signIn();
        const restored = firstValueFrom(service.refreshSession());
        requests.expectOne(`${base}/session`).flush({ ...response, user: { ...response.user, id: 'another-user' } });
        expect(await restored).toBe(false);
        expect(service.isAuthenticated()).toBe(false);
        expect(navigation.navigateToAuthAsync).toHaveBeenCalled();
    });
});

describe('browser session validation', () => {
    it('rejects a bearer response for a browser sign-in and accepts a later cookie session', async () => {
        const onAuthenticated = vi.fn<() => void>();
        const subscription = TestBed.inject(SessionEventsService).authenticated$.subscribe(onAuthenticated);

        try {
            const rejected = expect(firstValueFrom(service.login(login))).rejects.toThrow(
                'Unexpected bearer credential in a browser session response',
            );
            requests.expectOne({ method: 'POST', url: `${base}/login` }).flush({ ...response, accessToken: 'test-bearer' });
            await rejected;

            expect(service.isAuthenticated()).toBe(false);
            expect(service.getSessionKey()).toBeNull();
            expect(service.getToken()).toBeNull();
            expect(onAuthenticated).not.toHaveBeenCalled();

            const retry = firstValueFrom(service.login(login));
            requests.expectOne({ method: 'POST', url: `${base}/login` }).flush(response);
            await retry;

            expect(service.getUserId()).toBe(response.user.id);
            expect(service.getSessionKey()).toBe(sessionId);
            expect(service.getToken()).toBeNull();
            expect(onAuthenticated).toHaveBeenCalledOnce();
        } finally {
            subscription.unsubscribe();
        }
    });

    it('keeps ordinary browser validation failures retryable even when a required-field error is returned', async () => {
        const initial = service.restoreSessionAsync();
        requests
            .expectOne(`${base}/session`)
            .flush({ error: 'Validation.Required' }, { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' });
        await initial;
        expect(service.isAuthReady()).toBe(false);
        expect(service.isAuthenticated()).toBe(false);
    });
});

describe('browser session failures and logout', () => {
    it.each([HttpStatusCode.TooManyRequests, HttpStatusCode.ServiceUnavailable])(
        'allows initial restoration to be retried after a temporary %s response',
        async status => {
            const initial = service.restoreSessionAsync();
            requests.expectOne(`${base}/session`).flush({}, { status, statusText: 'Temporarily unavailable' });
            await initial;

            expect(service.isAuthReady(), 'Temporary restoration failure must remain retryable').toBe(false);
            expect(service.isAuthenticated()).toBe(false);
            expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();

            const retry = service.restoreSessionAsync();
            requests.expectOne(`${base}/session`).flush(response);
            await retry;
            expect(service.isAuthReady()).toBe(true);
            expect(service.isAuthenticated()).toBe(true);
        },
    );

    it('does not silently restore a session after its logout failed', async () => {
        signIn();
        const logout = service.onLogoutAsync();
        requests.expectOne(`${base}/logout`).flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
        await logout;

        const restoration = service.restoreSessionAsync();
        const retries = requests.match(`${base}/logout`);
        for (const request of retries) {
            expect(request.request.headers.get('X-FoodDiary-Session')).toBe(sessionId);
            request.flush(null);
        }
        // Drain the old implementation's unintended request so the failure is an
        // assertion about restored access, rather than an unfinished HTTP request.
        for (const request of requests.match(`${base}/session`)) {
            request.flush(response);
        }
        await restoration;

        expect(service.isAuthenticated(), 'Pending logout must block implicit restoration').toBe(false);
        expect(retries).toHaveLength(1);
    });

    it('ends local access when the server rejects a revoked session', async () => {
        signIn();
        const restored = firstValueFrom(service.refreshSession());
        requests.expectOne(`${base}/session`).flush({}, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });
        expect(await restored).toBe(false);
        expect(service.isAuthenticated()).toBe(false);
    });

    it('preserves local identity on a temporary server failure', async () => {
        signIn();
        const restored = firstValueFrom(service.refreshSession());
        requests.expectOne(`${base}/session`).flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
        expect(await restored).toBe(false);
        expect(service.isAuthenticated()).toBe(true);
    });

    it('cleans up before ending the server session and clears the local identity', async () => {
        signIn();
        const ended = service.onLogoutAsync();
        requests.expectOne(`${base}/logout`).flush(null);
        await ended;
        expect(service.isAuthenticated()).toBe(false);
        expect(service.getToken()).toBeNull();
        expect(navigation.navigateToLandingAsync).toHaveBeenCalled();
    });

    it('keeps impersonation selection local to the tab and never exposes its credential', async () => {
        const state = TestBed.inject(BrowserSessionStateService);
        state.setContext('context-one');
        const restored = firstValueFrom(service.refreshSession());
        requests
            .expectOne(`${base}/session`)
            .flush({ ...response, sessionId: null, impersonationContext: 'context-one', impersonationReason: 'Support request' });
        expect(await restored).toBe(true);
        expect(service.isImpersonating()).toBe(true);
        expect(service.impersonationReason()).toBe('Support request');
        expect(service.getToken()).toBeNull();
        expect(sessionStorage.getItem('impersonationContext')).toBe('context-one');
        expect(localStorage.getItem('impersonationContext')).toBeNull();
        expect(service.sessionHubUrl('/hubs/test')).toBe('/hubs/test?fd_context=context-one');
    });
});

it('logs out the selected support session and keeps the ordinary browser account preferences', async () => {
    const contextId = 'support-context';
    const browserAccess = JSON.stringify(['ordinary-user', 'User']);
    localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, browserAccess);
    const clearLanguage = vi.spyOn(TestBed.inject(LocalizationService), 'clearStoredLanguage');
    try {
        TestBed.inject(BrowserSessionStateService).setContext(contextId);
        const supportSession = firstValueFrom(service.refreshSession());
        requests.expectOne(`${base}/session`).flush({
            ...response,
            sessionId: null,
            impersonationContext: contextId,
            impersonationReason: 'Support request',
        });
        expect(await supportSession).toBe(true);
        expect(service.isImpersonating()).toBe(true);
        expect(service.impersonationReason()).toBe('Support request');

        const logout = service.onLogoutAsync();
        const request = requests.expectOne({ method: 'POST', url: `${base}/logout` });
        expect(request.request.headers.get('X-FoodDiary-Context')).toBe(contextId);
        expect(request.request.headers.has('X-FoodDiary-Session')).toBe(false);
        expect(sessionStorage.getItem('pendingImpersonationLogout')).toContain(contextId);
        expect(localStorage.getItem('pendingBrowserLogout')).toBeNull();
        request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await logout;

        expect(service.isAuthenticated()).toBe(false);
        expect(service.isImpersonating()).toBe(false);
        expect(service.impersonationReason()).toBeNull();
        expect(sessionStorage.getItem('pendingImpersonationLogout')).toBeNull();
        expect(localStorage.getItem(ACCOUNT_ACCESS_STORAGE_KEY)).toBe(browserAccess);
        expect(clearLanguage).not.toHaveBeenCalled();
        expect(navigation.navigateToLandingAsync).toHaveBeenCalledOnce();
        await service.ensureSessionReadyAsync();
        requests.expectNone(`${base}/session`);
    } finally {
        clearLanguage.mockRestore();
    }
});

describe('browser session policy and explicit Telegram mode', () => {
    it('keeps the required-password-change flow and clears the server flag after a new login', async () => {
        service.login(login).subscribe();
        requests.expectOne(`${base}/login`).flush({ ...response, user: { ...response.user, mustChangePassword: true } });
        expect(service.mustChangePassword()).toBe(true);
        const completed = service.completeRequiredPasswordChangeAsync('updated-password');
        const request = requests.expectOne(`${base}/login`);
        expect(request.request.body).toEqual({
            email: response.user.email,
            password: 'updated-password',
            rememberMe: true,
            expectedUserId: response.user.id,
        });
        request.flush({ ...response, user: { ...response.user, mustChangePassword: false } });
        await completed;
        expect(service.mustChangePassword()).toBe(false);
    });

    it('rejects an ordinary session response when restoring an impersonation context', async () => {
        TestBed.inject(BrowserSessionStateService).setContext('selected-context');
        const restored = firstValueFrom(service.refreshSession());
        requests.expectOne(`${base}/session`).flush(response);
        expect(await restored).toBe(false);
        expect(service.isAuthenticated()).toBe(false);
    });

    it('keeps an explicit Mini App access token only in memory', async () => {
        TestBed.resetTestingModule();
        window.history.replaceState({}, '', '/#tgWebAppData=unit-fixture');
        configureAuthTesting();
        const restored = firstValueFrom(service.refreshSession());
        requests.expectOne(`${base}/refresh`).flush({ user: response.user, accessToken: 'mini-access-token' });
        expect(await restored).toBe(true);
        expect(service.getToken()).toBe('mini-access-token');
        expect(localStorage.getItem('authToken')).toBeNull();
        expect(sessionStorage.getItem('authToken')).toBeNull();
        expect(localStorage.getItem('refreshToken')).toBeNull();
        expect(sessionStorage.getItem('refreshToken')).toBeNull();
    });

    it('publishes session lifecycle events for dependent feature cleanup', () => {
        const authenticated = vi.fn();
        TestBed.inject(SessionEventsService).authenticated$.subscribe(authenticated);
        signIn();
        expect(authenticated).toHaveBeenCalledOnce();
    });
});

describe('cross-tab browser session ownership', () => {
    it('keeps a restored guest tab signed out when another tab publishes account access', async () => {
        const restored = service.restoreSessionAsync();
        requests.expectOne({ method: 'POST', url: `${base}/session` }).flush(null, {
            status: HttpStatusCode.NoContent,
            statusText: 'No Content',
        });
        await restored;

        publishAccountAccess(['other-tab-user', 'Admin']);
        await waitForAsyncTasksAsync();

        expect(service.isAuthReady()).toBe(true);
        expect(service.isAuthenticated()).toBe(false);
        expect(service.getUserId()).toBeNull();
        expect(service.isAdmin()).toBe(false);
        expect(service.isEmailConfirmed()).toBe(true);
        expect(service.requiresEmailVerification()).toBe(false);
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
        requests.expectNone(`${base}/session`);
    });

    it('refreshes privileges for the same account using server metadata', async () => {
        signIn();
        expect(service.isAdmin()).toBe(false);
        expect(service.isDietologist()).toBe(false);

        publishAccountAccess([response.user.id, 'Admin']);

        const request = requests.expectOne({ method: 'POST', url: `${base}/session` });
        expect(request.request.headers.get('X-FoodDiary-Session')).toBe(sessionId);
        expect(service.isAdmin()).toBe(false);
        request.flush({ ...response, roles: ['User', 'Dietologist'] });
        await waitForAsyncTasksAsync();

        expect(service.getUserId()).toBe(response.user.id);
        expect(service.isAdmin()).toBe(false);
        expect(service.isDietologist()).toBe(true);
    });

    it('ends access in an older tab when another account signs in', async () => {
        signIn();
        const replacement = ['new-account', 'User'];

        publishAccountAccess(replacement);
        await waitForAsyncTasksAsync();

        expect(service.isAuthenticated()).toBe(false);
        expect(navigation.navigateToAuthAsync).toHaveBeenCalledExactlyOnceWith('login');
        expect(localStorage.getItem(ACCOUNT_ACCESS_STORAGE_KEY)).toBe(JSON.stringify(replacement));
        requests.expectNone(`${base}/session`);
    });

    it('ends a matching browser session when another tab starts logging out', async () => {
        signIn();
        const key = 'pendingBrowserLogout';
        const pending = JSON.stringify({ sessionId, contextId: null, telegram: false });
        localStorage.setItem(key, pending);

        dispatchAccountStorageChange(key, pending);
        await waitForAsyncTasksAsync();

        expect(service.isAuthenticated()).toBe(false);
        expect(navigation.navigateToLandingAsync).toHaveBeenCalledOnce();
        requests.expectNone(`${base}/logout`);
    });
});

it('retries the original browser logout after the document is denied its cookie lock', async () => {
    signIn();
    const recovery = TestBed.inject(AuthRecoveryStateService);
    const blocked = new Subject<never>();
    const lock = vi.fn(async () => firstValueFrom(blocked));
    const navigator = window.navigator;
    const previousLocks = Object.getOwnPropertyDescriptor(navigator, 'locks');
    Object.defineProperty(navigator, 'locks', { configurable: true, value: { request: lock } });
    try {
        const logout = service.onLogoutAsync(true);
        expect(lock).toHaveBeenCalledOnce();
        expect(service.isAuthenticated()).toBe(false);
        expect(recovery.busy()).toBe(true);
        requests.expectNone({ method: 'POST', url: `${base}/logout` });

        blocked.error(new DOMException('The document is not fully active', 'InvalidStateError'));
        await logout;

        expect(recovery.busy()).toBe(false);
        expect(recovery.issue()).toBe('logout');
        expect(recovery.pendingLogout()).toEqual({ sessionId, contextId: null, telegram: false });
        expect(localStorage.getItem('pendingBrowserLogout')).toContain(sessionId);
        requests.expectNone({ method: 'POST', url: `${base}/logout` });
    } finally {
        if (previousLocks === undefined) {
            Reflect.deleteProperty(navigator, 'locks');
        } else {
            Object.defineProperty(navigator, 'locks', previousLocks);
        }
    }

    const retry = service.retryPendingLogoutAsync();
    const request = requests.expectOne({ method: 'POST', url: `${base}/logout` });
    expect(request.request.headers.get('X-FoodDiary-Session')).toBe(sessionId);
    expect(request.request.headers.get('X-FoodDiary-Client')).toBe('web');
    request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    await retry;

    expect(service.isAuthReady()).toBe(true);
    expect(service.isAuthenticated()).toBe(false);
    expect(recovery.pendingLogout()).toBeNull();
    expect(recovery.issue()).toBeNull();
    expect(localStorage.getItem('pendingBrowserLogout')).toBeNull();
});

describe('browser session recovery boundaries', () => {
    it('refreshes on visible activity after the renewal interval and coalesces nearby activity', () => {
        const interval = 3_600_000;
        const clock = vi.spyOn(Date, 'now').mockReturnValue(Date.now() + interval);
        const visibility = vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('hidden');
        try {
            signIn();
            document.dispatchEvent(new Event('pointerdown'));
            requests.expectNone(`${base}/session`);

            visibility.mockReturnValue('visible');
            document.dispatchEvent(new Event('visibilitychange'));
            const renewal = requests.expectOne({ method: 'POST', url: `${base}/session` });
            renewal.flush({ ...response, roles: ['User', 'Premium'] });
            document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
            requests.expectNone(`${base}/session`);

            expect(service.isPremium()).toBe(true);
        } finally {
            clock.mockRestore();
            visibility.mockRestore();
        }
    });

    it('preserves a newer login when an older logout is waiting for session cleanup', async () => {
        signIn();
        const cleanup = new Subject<void>();
        TestBed.inject(SessionEventsService).registerSessionEndCleanup(async () => firstValueFrom(cleanup));

        const logout = service.onLogoutAsync(true);
        expect(service.isAuthenticated()).toBe(false);
        requests.expectNone(`${base}/logout`);

        const replacement = { ...response, user: { ...response.user, id: 'replacement-account' }, sessionId: 'replacement-session' };
        service.acceptExternalAuthentication(authResponseFromSdk(replacement));
        cleanup.next();
        await logout;

        expect(service.getUserId()).toBe(replacement.user.id);
        expect(service.getSessionKey()).toBe(replacement.sessionId);
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
        requests.expectNone(`${base}/logout`);
    });

    it('logs out after cleanup rejects and the remaining session cleanup completes', async () => {
        signIn();
        const sessions = TestBed.inject(SessionEventsService);
        const failed = new Subject<void>();
        const remaining = new Subject<void>();
        sessions.registerSessionEndCleanup(async () => firstValueFrom(failed));
        sessions.registerSessionEndCleanup(async () => firstValueFrom(remaining));

        const logout = service.onLogoutAsync();
        expect(service.isAuthenticated()).toBe(false);
        failed.error(new Error('Device cleanup unavailable'));
        await waitForAsyncTasksAsync();
        requests.expectNone(`${base}/logout`);
        expect(navigation.navigateToLandingAsync).not.toHaveBeenCalled();

        remaining.next();
        remaining.complete();
        const request = await vi.waitFor(() => requests.expectOne({ method: 'POST', url: `${base}/logout` }));
        request.flush(null);
        await logout;

        expect(localStorage.getItem('pendingBrowserLogout')).toBeNull();
        expect(navigation.navigateToLandingAsync).toHaveBeenCalledOnce();
    });

    it('treats an already replaced cookie as a completed logout', async () => {
        signIn();

        const logout = service.onLogoutAsync();
        requests
            .expectOne(`${base}/logout`)
            .flush({ error: 'Authentication.SessionChanged' }, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });
        await logout;

        expect(service.isAuthenticated()).toBe(false);
        expect(service.isAuthReady()).toBe(true);
        expect(localStorage.getItem('pendingBrowserLogout')).toBeNull();
        expect(navigation.navigateToLandingAsync).toHaveBeenCalledOnce();
    });
});

describe('AuthService account-switch logout', () => {
    it('clears the old session and opens login with a profile return path from the live owner', async () => {
        signIn();
        const sessions = TestBed.inject(SessionEventsService);
        const cleanup = vi.fn<() => Promise<void>>().mockResolvedValue(undefined);
        const unregister = sessions.registerSessionEndCleanup(cleanup);
        const owner = createEnvironmentInjector([], TestBed.inject(EnvironmentInjector));
        navigation.navigateToAuthAsync.mockImplementation(async () => {
            await waitForAsyncTasksAsync();
            expect(sessions.isSessionEndNavigation).toBe(true);
        });

        try {
            const done = service.logoutForAccountSwitchAsync('/profile', owner.get(DestroyRef));
            expect(cleanup).toHaveBeenCalledOnce();
            expect(service.isAuthenticated()).toBe(false);
            expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
            const request = await vi.waitFor(() => requests.expectOne({ method: 'POST', url: `${base}/logout` }));
            request.flush(null);
            await done;

            expect(navigation.navigateToAuthAsync).toHaveBeenCalledExactlyOnceWith('login', '/profile');
            expect(navigation.navigateToLandingAsync).not.toHaveBeenCalled();
            expect(sessions.isSessionEndNavigation).toBe(false);
        } finally {
            unregister();
            owner.destroy();
        }
    });

    it.each(['destroyed route', 'newer session'] as const)('keeps sign-out from redirecting after %s', async transition => {
        signIn();
        const owner = createEnvironmentInjector([], TestBed.inject(EnvironmentInjector));
        const scope = owner.get(DestroyRef);
        const replacement = { ...response, sessionId: 'replacement-session', user: { ...response.user, id: 'replacement-user' } };
        try {
            const done = service.logoutForAccountSwitchAsync('/profile', scope);
            const request = requests.expectOne({ method: 'POST', url: `${base}/logout` });
            if (transition === 'destroyed route') {
                owner.destroy();
            } else {
                service.acceptExternalAuthentication(authResponseFromSdk(replacement));
            }
            request.flush(null);
            await done;

            expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
            expect(navigation.navigateToLandingAsync).not.toHaveBeenCalled();
            expect(service.getSessionKey()).toBe(transition === 'newer session' ? replacement.sessionId : null);
        } finally {
            if (!scope.destroyed) {
                owner.destroy();
            }
        }
    });
});

describe('browser session link exchanges', () => {
    it('does not apply an old impersonation exchange after a newer login', async () => {
        window.history.replaceState({}, '', '/#impersonationCode=old-fixture');
        const restoration = service.restoreSessionAsync();
        const exchange = requests.expectOne({ method: 'POST', url: `${base}/impersonation/exchange` });
        const replacement = { ...response, user: { ...response.user, id: 'replacement-account' }, sessionId: 'replacement-session' };
        service.acceptExternalAuthentication(authResponseFromSdk(replacement));

        exchange.flush({ contextId: 'expired-context' });
        await waitForAsyncTasksAsync();
        const obsolete = requests.match(`${base}/session`);
        for (const request of obsolete) {
            request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        }
        await restoration;

        expect(obsolete).toEqual([]);
        expect(TestBed.inject(BrowserSessionStateService).contextId()).toBeNull();
        expect(service.getUserId()).toBe(replacement.user.id);
        expect(service.getSessionKey()).toBe(replacement.sessionId);
    });

    it('removes an expired handoff from the URL and leaves the browser in its guest state', async () => {
        window.history.replaceState({}, '', '/?view=food#impersonationCode=expired-fixture&tab=notes');
        const restoration = service.restoreSessionAsync();
        requests.expectOne(`${base}/impersonation/exchange`).flush({ contextId: '' });
        await restoration;

        expect(window.location.search).toBe('?view=food');
        expect(window.location.hash).toBe('#tab=notes');
        expect(service.isAuthenticated()).toBe(false);
        expect(service.isAuthReady()).toBe(true);
        requests.expectNone(`${base}/session`);
    });

    it('restores the browser cookie for a recovered account and keeps the requested remember choice', async () => {
        const recovery = firstValueFrom(
            service.restoreAccount(new RestoreAccountRequest({ email: login.email, password: login.password })),
        );
        const request = requests.expectOne({ method: 'POST', url: `${base}/restore` });
        expect(request.request.body).toEqual({ email: login.email, password: login.password, rememberMe: false });
        request.flush(response);
        await recovery;

        expect(service.isAuthenticated()).toBe(true);
        expect(service.getSessionKey()).toBe(sessionId);
        expect(localStorage.getItem('authToken')).toBeNull();
        expect(sessionStorage.getItem('authRemembered')).toBe('false');
    });
});

function publishAccountAccess(access: string[]): void {
    const payload = JSON.stringify(access);
    localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, payload);
    dispatchAccountStorageChange(ACCOUNT_ACCESS_STORAGE_KEY, payload);
}

describe('Telegram recovery SDK contracts', () => {
    it('starts the backup email provider flow with the requested address', async () => {
        const email = response.user.email;
        const started = firstValueFrom(service.startTelegramBackupEmail(email));
        const start = requests.expectOne({ method: 'POST', url: `${base}/telegram/backup-email/oidc/start` });
        expect(start.request.body).toEqual({ email });
        const authorizationUrl = 'https://oauth.telegram.org/auth?state=fixture';
        start.flush({ authorizationUrl });
        await expect(started).resolves.toEqual({ authorizationUrl });
    });

    it.each(['backup-email', 'unlink'] as const)(
        'completes the previously started %s provider flow with its callback values',
        async kind => {
            const code = telegramOAuthCode('fixture-code');
            const state = telegramOAuthState('fixture-state');
            const completed = firstValueFrom(
                kind === 'backup-email' ? service.completeTelegramBackupEmail(code, state) : service.completeTelegramUnlink(code, state),
            );
            const finish = requests.expectOne({ method: 'POST', url: `${base}/telegram/${kind}/oidc/complete` });
            expect(finish.request.body).toEqual({ code, state });
            finish.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
            await expect(completed).resolves.toBeNull();
        },
    );

    it('uses the Mini App proof when requesting backup email and unlinking without an OIDC redirect', async () => {
        const email = response.user.email;
        const initData = 'fixture-mini-app-launch';
        const backup = firstValueFrom(service.requestTelegramBackupEmail(email, initData));
        const emailRequest = requests.expectOne({ method: 'POST', url: `${base}/telegram/backup-email` });
        expect(emailRequest.request.body).toEqual({ email, initData });
        emailRequest.flush(null);
        await backup;

        const unlink = firstValueFrom(service.unlinkTelegram(initData));
        const unlinkRequest = requests.expectOne({ method: 'POST', url: `${base}/telegram/unlink` });
        expect(unlinkRequest.request.body).toEqual({ initData });
        unlinkRequest.flush(null);
        await unlink;
    });

    it.each([undefined, 'diary-review-fixture'])('verifies a backup email and returns the optional transfer ticket %s', async ticket => {
        const token = 'verification-fixture';
        const verified = firstValueFrom(service.verifyTelegramBackupEmail(response.user.id, token));
        const request = requests.expectOne({ method: 'POST', url: `${base}/telegram/backup-email/verify` });
        expect(request.request.body).toEqual({ userId: response.user.id, token });
        request.flush(ticket === undefined ? {} : { transferTicket: ticket });
        await expect(verified).resolves.toBe(ticket ?? null);
    });
});

describe('Telegram diary transfer SDK contracts', () => {
    it('separates a review from confirmation and keeps an explicit choice to retain email goals', async () => {
        const ticket = 'diary-review-fixture';
        const preview: TelegramDiaryTransferHttpResponse = {
            counts: { meals: 2, recipes: 0, products: 1, waterEntries: 1, measurements: 0 },
            emailGoals: { calories: 2100 },
            telegramGoals: { calories: 1800, water: 2200 },
            canUseTelegramGoals: true,
        };
        const loaded = firstValueFrom(service.transferTelegramDiary(ticket));
        const request = requests.expectOne({ method: 'POST', url: `${base}/telegram/diary-transfer` });
        expect(request.request.serializeBody()).toBe(JSON.stringify({ ticket }));
        request.flush(preview);
        await expect(loaded).resolves.toMatchObject({
            counts: preview.counts,
            emailGoals: { calories: 2100, water: null, steps: null },
            telegramGoals: { calories: 1800, water: 2200, steps: null },
            canUseTelegramGoals: true,
        });

        const confirmed = firstValueFrom(service.transferTelegramDiary(ticket, false));
        const confirm = requests.expectOne({ method: 'POST', url: `${base}/telegram/diary-transfer` });
        expect(confirm.request.body).toEqual({ ticket, useTelegramGoals: false });
        confirm.flush(preview);
        await expect(confirmed).resolves.toMatchObject({ counts: preview.counts });
    });
});

describe('Telegram disconnect account ownership', () => {
    const owner = entityId<'user'>(response.user.id);
    const path = `${base}/telegram/disconnect/confirm`;

    it.each([
        ['password', { password: 'test-password' }, false],
        ['Google', { googleCredential: 'test-google-confirmation' }, true],
    ] as const)('installs the same owner’s replacement session after %s confirmation', async (_method, confirmation, rememberMe) => {
        service.acceptExternalAuthentication(authResponseFromSdk(response));
        const result = firstValueFrom(service.disconnectTelegram(owner, confirmation));
        const request = requests.expectOne({ method: 'POST', url: path });
        expect(request.request.body).toEqual({ ...confirmation, rememberMe });

        const replacementSession = 'telegram-disconnected-session';
        request.flush({ ...response, sessionId: replacementSession, user: { ...response.user, hasTelegramIdentity: false } });
        await expect(result).resolves.toMatchObject({ id: owner, hasTelegramIdentity: false });
        expect(service.getUserId()).toBe(owner);
        expect(service.getSessionKey()).toBe(replacementSession);
    });

    it('does not send an old confirmation after another account has taken over', async () => {
        service.acceptExternalAuthentication(authResponseFromSdk(response));
        const confirmation: TelegramDisconnectConfirmation = { password: 'previous-account-fixture' };
        const disconnect = service.disconnectTelegram(owner, confirmation);
        const replacement = telegramDisconnectReplacement();
        service.acceptExternalAuthentication(authResponseFromSdk(replacement));

        await expect(firstValueFrom(disconnect)).rejects.toThrow('Account changed');
        requests.expectNone(path);
        expect(service.getSessionKey()).toBe(replacement.sessionId);
    });

    it('does not replace a newer account when the old confirmation returns late', async () => {
        service.acceptExternalAuthentication(authResponseFromSdk(response));
        const disconnect = firstValueFrom(service.disconnectTelegram(owner, { password: 'previous-account-fixture' }));
        const pending = requests.expectOne({ method: 'POST', url: path });
        const replacement = telegramDisconnectReplacement();
        service.acceptExternalAuthentication(authResponseFromSdk(replacement));

        pending.flush({ ...response, sessionId: 'obsolete-confirmed-session' });
        await expect(disconnect).rejects.toThrow('Account changed');
        expect(service.getUserId()).toBe(replacement.user.id);
        expect(service.getSessionKey()).toBe(replacement.sessionId);
    });
});

function telegramDisconnectReplacement(): typeof response {
    return { ...response, sessionId: 'other-account-session', user: { ...response.user, id: 'other-account' } };
}

function dispatchAccountStorageChange(key: string, value: string): void {
    const event = new StorageEvent('storage', { key, newValue: value });
    Object.defineProperty(event, 'storageArea', { value: window.localStorage });
    window.dispatchEvent(event);
}

describe('Authentication with a consumed impersonation handoff', () => {
    it('keeps public URL state and finishes as a guest when the exchange returns 401', async () => {
        const logError = vi.spyOn(console, 'error').mockImplementation(() => {});
        try {
            window.history.replaceState({}, '', '/?view=food#impersonationCode=already-used&tab=notes');
            const restoration = service.restoreSessionAsync();
            const exchange = requests.expectOne({ method: 'POST', url: `${base}/impersonation/exchange` });
            expect(exchange.request.body).toEqual({ code: 'already-used' });
            expect(window.location.search).toBe('?view=food');
            expect(window.location.hash).toBe('#tab=notes');

            exchange.flush(
                { error: 'Authentication.InvalidToken', message: 'Invalid authorization token.' },
                { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' },
            );
            await restoration;

            expect(service.isAuthReady()).toBe(true);
            expect(service.isAuthenticated()).toBe(false);
            expect(TestBed.inject(BrowserSessionStateService).contextId()).toBeNull();
            expect(sessionStorage.getItem('impersonationContext')).toBeNull();
            expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
            requests.expectNone(`${base}/session`);
            await service.restoreSessionAsync();
            requests.expectNone(`${base}/impersonation/exchange`);
        } finally {
            logError.mockRestore();
        }
    });
});

describe('Telegram Mini App pending logout restoration', () => {
    it('keeps the same launch signed out while resuming a failed logout on reload', async () => {
        TestBed.resetTestingModule();
        window.history.replaceState({}, '', '/#tgWebAppData=synthetic-launch');
        configureAuthTesting();
        const payload = window
            .btoa(JSON.stringify({ refresh_session_id: sessionId }))
            .replaceAll('+', '-')
            .replaceAll('/', '_')
            .replace(/=+$/u, '');
        const accessToken = `eyJhbGciOiJIUzI1NiJ9.${payload}.synthetic-signature`;
        const restored = firstValueFrom(service.refreshSession());
        requests.expectOne({ method: 'POST', url: `${base}/refresh` }).flush({ user: response.user, accessToken });
        expect(await restored).toBe(true);
        expect(await TestBed.inject(TelegramWebAppService).isAutomaticSignInAllowedAsync()).toBe(true);

        const logout = service.onLogoutAsync();
        const logoutRequest = await vi.waitFor(() => requests.expectOne({ method: 'POST', url: `${base}/logout` }));
        expect(logoutRequest.request.headers.get('X-FoodDiary-Client')).toBe('telegram');
        expect(logoutRequest.request.headers.get('X-FoodDiary-Session')).toBe(sessionId);
        expect(await TestBed.inject(TelegramWebAppService).isAutomaticSignInAllowedAsync()).toBe(false);
        logoutRequest.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
        await logout;

        expect(localStorage.getItem('pendingTelegramLogout')).toBe(JSON.stringify({ sessionId, contextId: null, telegram: true }));
        expect(localStorage.getItem('pendingBrowserLogout')).toBeNull();
        expect(service.getToken()).toBeNull();
        TestBed.resetTestingModule();
        configureAuthTesting();

        const resumed = service.restoreSessionAsync();
        const resumedLogout = await vi.waitFor(() => requests.expectOne({ method: 'POST', url: `${base}/logout` }));
        expect(resumedLogout.request.headers.get('X-FoodDiary-Client')).toBe('telegram');
        expect(resumedLogout.request.headers.get('X-FoodDiary-Session')).toBe(sessionId);
        resumedLogout.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await resumed;

        expect(service.isAuthReady()).toBe(true);
        expect(service.isAuthenticated()).toBe(false);
        expect(localStorage.getItem('pendingTelegramLogout')).toBeNull();
        expect(await TestBed.inject(TelegramWebAppService).isAutomaticSignInAllowedAsync()).toBe(false);
        requests.expectNone(`${base}/refresh`);
        requests.expectNone(`${base}/session`);
    });
});
