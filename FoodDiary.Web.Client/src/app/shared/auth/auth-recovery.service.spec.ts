import { HTTP_INTERCEPTORS, HttpStatusCode, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { AuthInterceptor } from '../../interceptor/auth.interceptor';
import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';
import { authResponseFromSdk } from '../api/sdk/auth-sdk.mapper';
import { LocalizationService } from '../i18n/localization.service';
import { AuthRecoveryStateService } from './auth-recovery-state.service';

const base = environment.apiUrls.auth;
const sessionId = 'dd281aa8-5d85-4fda-bc28-a7e1fd6085db';
const INITIAL_TIME_MS = 1000;
const AFTER_RETRY_TIME_MS = 4000;
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
let auth: AuthService;
let recovery: AuthRecoveryStateService;
let requests: HttpTestingController;

function boot(): void {
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(withInterceptorsFromDi()),
            provideHttpClientTesting(),
            { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
            { provide: NavigationService, useValue: { navigateToAuthAsync: vi.fn(), navigateToLandingAsync: vi.fn() } },
            { provide: LocalizationService, useValue: { applyLanguagePreferenceAsync: vi.fn(), clearStoredLanguage: vi.fn() } },
        ],
    });
    auth = TestBed.inject(AuthService);
    recovery = TestBed.inject(AuthRecoveryStateService);
    requests = TestBed.inject(HttpTestingController);
}

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    boot();
    auth.acceptExternalAuthentication(authResponseFromSdk(response));
});

afterEach(() => {
    requests.verify();
    vi.restoreAllMocks();
    localStorage.clear();
    sessionStorage.clear();
});

async function loseLogoutAsync(): Promise<void> {
    const pending = auth.onLogoutAsync();
    requests.expectOne(`${base}/logout`).flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
    await pending;
}

describe('session recovery across requests and reloads', () => {
    it('keeps the original session selector when logout is requested again after a failure', async () => {
        await loseLogoutAsync();
        const repeated = auth.onLogoutAsync();
        const request = requests.expectOne(`${base}/logout`);
        const selected = request.request.headers.get('X-FoodDiary-Session');
        request.flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
        await repeated;
        expect(selected, 'Repeated logout must retain the original selected session').toBe(sessionId);
        expect(recovery.pendingLogout()?.sessionId).toBe(sessionId);
    });

    it('persists only public selectors and retries the original logout after recreating the app', async () => {
        await loseLogoutAsync();
        expect(JSON.parse(localStorage.getItem('pendingBrowserLogout') ?? '{}')).toEqual({ sessionId, contextId: null, telegram: false });
        expect(auth.isAuthenticated()).toBe(false);
        expect(recovery.issue()).toBe('logout');
        requests.verify();
        TestBed.resetTestingModule();
        boot();

        const restored = auth.restoreSessionAsync();
        const retry = requests.expectOne(`${base}/logout`);
        expect(retry.request.headers.get('X-FoodDiary-Session')).toBe(sessionId);
        expect(retry.request.headers.has('Authorization')).toBe(false);
        retry.flush(null);
        await restored;

        requests.expectNone(`${base}/session`);
        expect(auth.isAuthenticated()).toBe(false);
        expect(recovery.pendingLogout()).toBeNull();
        expect(localStorage.getItem('pendingBrowserLogout')).toBeNull();
    });

    it('keeps the logout fence when the retry is temporarily unavailable', async () => {
        await loseLogoutAsync();
        const retry = auth.retryPendingLogoutAsync();
        requests.expectOne(`${base}/logout`).flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
        await retry;

        expect(await firstValueFrom(auth.refreshSession())).toBe(false);
        requests.expectNone(`${base}/session`);
        expect(recovery.issue()).toBe('logout');
        expect(recovery.pendingLogout()?.sessionId).toBe(sessionId);
    });

    it('acknowledges a replaced cookie without logging out or implicitly entering its replacement session', async () => {
        await loseLogoutAsync();
        const retry = auth.retryPendingLogoutAsync();
        const request = requests.expectOne(`${base}/logout`);
        expect(request.request.headers.get('X-FoodDiary-Session')).toBe(sessionId);
        request.flush({ error: 'Authentication.SessionChanged' }, { status: HttpStatusCode.Unauthorized, statusText: 'Changed' });
        await retry;

        expect(recovery.pendingLogout()).toBeNull();
        expect(auth.isAuthenticated()).toBe(false);
        requests.expectNone(`${base}/session`);
    });

    it('does not let a late logout failure change the recovery state of a new authenticated session', async () => {
        const logout = auth.onLogoutAsync();
        const request = requests.expectOne(`${base}/logout`);
        auth.acceptExternalAuthentication(authResponseFromSdk({ ...response, sessionId: 'replacement' }));
        request.flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
        await logout;

        expect(auth.getSessionKey()).toBe('replacement');
        expect(recovery.issue(), 'A late logout result must not change the replacement session').toBeNull();
        expect(recovery.pendingLogout()).toBeNull();
    });

    it('respects Retry-After and restores the session when the retry becomes eligible', async () => {
        const now = vi.spyOn(Date, 'now').mockReturnValue(INITIAL_TIME_MS);
        const restored = firstValueFrom(auth.refreshSession());
        requests
            .expectOne(`${base}/session`)
            .flush({}, { status: HttpStatusCode.TooManyRequests, statusText: 'Limited', headers: { 'Retry-After': '3' } });
        expect(await restored).toBe(false);
        expect(await firstValueFrom(auth.refreshSession())).toBe(false);
        requests.expectNone(`${base}/session`);
        now.mockReturnValue(AFTER_RETRY_TIME_MS);
        const retry = firstValueFrom(auth.refreshSession());
        requests.expectOne(`${base}/session`).flush(response);
        expect(await retry).toBe(true);
        expect(recovery.issue()).toBeNull();
    });
});
