import { HTTP_INTERCEPTORS, HttpClient, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { userFixture } from '../../../testing/api-fixtures';
import { AuthInterceptor } from '../../interceptor/auth.interceptor';
import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';
import { LocalizationService } from '../i18n/localization.service';
import { ThemeService } from '../theme/theme.service';
import { ACCOUNT_ACCESS_STORAGE_KEY } from './auth-metadata-storage.service';
import { SessionEventsService } from './session-events.service';

const authUrl = environment.apiUrls.auth;
const navigation = {
    navigateToAuthAsync: vi.fn().mockResolvedValue(undefined),
    navigateToLandingAsync: vi.fn().mockResolvedValue(undefined),
};

function response(userId: string, sessionId = `session-${userId}`): object {
    return { sessionId, roles: ['User'], user: userFixture({ id: userId, isEmailConfirmed: userId === 'alice' }) };
}

function context(): { auth: AuthService; http: HttpClient; requests: HttpTestingController; sessions: SessionEventsService } {
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(withInterceptorsFromDi()),
            provideHttpClientTesting(),
            { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
            { provide: NavigationService, useValue: navigation },
            {
                provide: LocalizationService,
                useValue: { applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(undefined), clearStoredLanguage: vi.fn() },
            },
            { provide: ThemeService, useValue: { syncWithUserPreferences: vi.fn() } },
        ],
    });
    return {
        auth: TestBed.inject(AuthService),
        http: TestBed.inject(HttpClient),
        requests: TestBed.inject(HttpTestingController),
        sessions: TestBed.inject(SessionEventsService),
    };
}

function publishBob(): void {
    localStorage.setItem('userId', 'bob');
    localStorage.setItem('emailConfirmed', 'false');
    localStorage.setItem('mustChangePassword', 'true');
    localStorage.setItem('refreshSession', 'true');
    localStorage.setItem(ACCOUNT_ACCESS_STORAGE_KEY, JSON.stringify(['bob', 'User']));
}

function dispatchAccountChange(): void {
    const event = new StorageEvent('storage', {
        key: ACCOUNT_ACCESS_STORAGE_KEY,
        newValue: localStorage.getItem(ACCOUNT_ACCESS_STORAGE_KEY),
    });
    Object.defineProperty(event, 'storageArea', { value: localStorage });
    window.dispatchEvent(event);
}

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    vi.clearAllMocks();
    window.history.replaceState({}, '', '/');
});
afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
});

describe('browser session identity boundary', () => {
    it('serializes logout and the next login through the cookie response', async () => {
        const { auth, requests, http } = context();
        auth.login({ email: 'alice@example.test', password: 'password', rememberMe: false }).subscribe();
        requests.expectOne(`${authUrl}/login`).flush(response('alice'));
        const logout = auth.onLogoutAsync();
        const ending = await vi.waitFor(() => requests.expectOne(`${authUrl}/logout`));
        auth.login({ email: 'bob@example.test', password: 'password', rememberMe: false }).subscribe();
        requests.expectNone(`${authUrl}/login`);
        expect(ending.request.headers.get('X-FoodDiary-Session')).toBe('session-alice');
        ending.flush(null);
        const receiving = await vi.waitFor(() => requests.expectOne(`${authUrl}/login`));
        expect(receiving.request.headers.has('Authorization')).toBe(false);
        receiving.flush(response('bob'));
        await logout;
        expect(auth.getUserId()).toBe('bob');
        const url = `${authUrl.replace(/\/auth$/, '')}/users/info`;
        http.get(url).subscribe();
        const profile = requests.expectOne(url);
        expect(profile.request.headers.has('Authorization')).toBe(false);
        expect(profile.request.headers.get('X-FoodDiary-Session')).toBe('session-bob');
        profile.flush({});
    });

    it.each([true, false])('pins mutations before the foreign account event (remembered=%s)', remembered => {
        const { auth, http, requests, sessions } = context();
        auth.login({ email: 'alice@example.test', password: 'password', rememberMe: remembered }).subscribe();
        requests.expectOne(`${authUrl}/login`).flush(response('alice'));
        publishBob();
        const url = `${authUrl.replace(/\/auth$/, '')}/hydrations`;
        http.post(url, { amount: 100 }).subscribe();
        const mutation = requests.expectOne(url);
        expect(mutation.request.headers.get('X-FoodDiary-Session')).toBe('session-alice');
        mutation.flush({});
        dispatchAccountChange();
        expect(auth.getUserId()).toBeNull();
        expect(sessions.isLocalSessionInvalidated).toBe(true);
        expect(localStorage.getItem('userId')).toBe('bob');
        requests.expectNone(`${authUrl}/logout`);
    });
});

describe('browser session restoration boundaries', () => {
    it('discards legacy credentials and confirms identity and policy on the server', async () => {
        sessionStorage.setItem('authToken', 'old-access');
        localStorage.setItem('refreshToken', 'old-refresh');
        localStorage.setItem('emailConfirmed', 'true');
        const { auth, requests } = context();
        expect(auth.isAuthenticated()).toBe(false);
        expect(sessionStorage.getItem('authToken')).toBeNull();
        expect(localStorage.getItem('refreshToken')).toBeNull();
        const pending = auth.restoreSessionAsync();
        const restore = await vi.waitFor(() => requests.expectOne(`${authUrl}/session`));
        restore.flush(response('alice'));
        await pending;
        expect(auth.getUserId()).toBe('alice');
        expect(auth.isEmailConfirmed()).toBe(true);
        expect(auth.getToken()).toBeNull();
    });

    it('rejects restoration into a different server session without logging it out', async () => {
        const { auth, requests, sessions } = context();
        auth.login({ email: 'alice@example.test', password: 'password', rememberMe: false }).subscribe();
        requests.expectOne(`${authUrl}/login`).flush(response('alice'));
        const pending = firstValueFrom(auth.refreshSession());
        publishBob();
        requests.expectOne(`${authUrl}/session`).flush(response('bob'));
        expect(await pending).toBe(false);
        expect(auth.isAuthenticated()).toBe(false);
        expect(sessions.isLocalSessionInvalidated).toBe(true);
        expect(localStorage.getItem('userId')).toBe('bob');
        requests.expectNone(`${authUrl}/logout`);
    });

    it('does not restore local policy hints when the server is unavailable', async () => {
        publishBob();
        const { auth, requests } = context();
        const pending = auth.restoreSessionAsync();
        const restore = await vi.waitFor(() => requests.expectOne(`${authUrl}/session`));
        restore.flush(null, { status: 503, statusText: 'Unavailable' });
        await pending;
        expect(auth.isAuthenticated()).toBe(false);
        requests.expectNone(`${authUrl}/logout`);
    });

    it('restores an isolated impersonation context and ignores ordinary account events', async () => {
        sessionStorage.setItem('impersonationContext', 'support-context');
        publishBob();
        const { auth, requests } = context();
        const pending = auth.restoreSessionAsync();
        const restore = await vi.waitFor(() => requests.expectOne(`${authUrl}/session`));
        expect(restore.request.headers.get('X-FoodDiary-Context')).toBe('support-context');
        restore.flush({ roles: ['User'], impersonationContext: 'support-context', user: userFixture({ id: 'alice' }) });
        await pending;
        dispatchAccountChange();
        expect(auth.getUserId()).toBe('alice');
        expect(auth.isImpersonating()).toBe(true);
        expect(auth.getToken()).toBeNull();
        expect(localStorage.getItem('userId')).toBe('bob');
        requests.expectNone(`${authUrl}/logout`);
    });

    it('never falls back to the ordinary account after a rejected impersonation context', async () => {
        sessionStorage.setItem('impersonationContext', 'expired-context');
        const { auth, requests } = context();
        const pending = auth.restoreSessionAsync();
        const restore = await vi.waitFor(() => requests.expectOne(`${authUrl}/session`));
        restore.flush(null, { status: 401, statusText: 'Unauthorized' });
        await pending;
        expect(auth.isAuthenticated()).toBe(false);
        requests.expectNone(`${authUrl}/session`);
        requests.expectNone(`${authUrl}/logout`);
    });
});
