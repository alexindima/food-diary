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
import { ACCOUNT_ACCESS_STORAGE_KEY } from '../../services/token-storage.service';
import { LocalizationService } from '../i18n/localization.service';
import { ThemeService } from '../theme/theme.service';
import { SessionEventsService } from './session-events.service';

const authUrl = environment.apiUrls.auth;
const navigation = {
    navigateToAuthAsync: vi.fn().mockResolvedValue(undefined),
    navigateToLandingAsync: vi.fn().mockResolvedValue(undefined),
};

function token(userId: string, extra: Record<string, unknown> = {}): string {
    return `${btoa('{}')}.${btoa(JSON.stringify({ sub: userId, role: 'User', ...extra }))}.signature`;
}

function response(userId: string, accessToken = token(userId)): { accessToken: string; user: ReturnType<typeof userFixture> } {
    return { accessToken, user: userFixture({ id: userId, isEmailConfirmed: userId === 'alice' }) };
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
    localStorage.setItem('authToken', token('bob'));
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
});
afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
});

describe('tab session identity boundary', () => {
    it('sends Bob login only after Alice logout has applied both its cookie response and local session end', async () => {
        const { auth, requests, http } = context();
        auth.login({ email: 'alice@example.test', password: 'password', rememberMe: false }).subscribe();
        requests.expectOne(`${authUrl}/login`).flush(response('alice'));
        const logout = auth.onLogoutAsync();
        const ending = await vi.waitFor(() => requests.expectOne(`${authUrl}/logout`));
        auth.login({ email: 'bob@example.test', password: 'password', rememberMe: false }).subscribe();
        requests.expectNone(`${authUrl}/login`);

        ending.flush(null);

        const receiving = await vi.waitFor(() => requests.expectOne(`${authUrl}/login`));
        expect(receiving.request.headers.has('Authorization')).toBe(false);
        expect(auth.getToken()).toBeNull();
        receiving.flush(response('bob'));
        await logout;
        expect(auth.getUserId()).toBe('bob');
        http.get('/api/v1/users/current').subscribe();
        const ownProfile = requests.expectOne('/api/v1/users/current');
        expect(ownProfile.request.headers.get('Authorization')).toBe(`Bearer ${token('bob')}`);
        ownProfile.flush({ id: 'bob' });
    });

    it.each([true, false])('pins the initial mutation to Alice before a foreign storage event (remembered=%s)', remembered => {
        const { auth, http, requests, sessions } = context();
        auth.login({ email: 'alice@example.test', password: 'password', rememberMe: remembered }).subscribe();
        requests.expectOne(`${authUrl}/login`).flush(response('alice'));
        publishBob();
        http.post('/api/v1/hydrations', { amount: 100 }).subscribe();
        const mutation = requests.expectOne('/api/v1/hydrations');
        expect(auth.getUserId()).toBe('alice');
        expect(mutation.request.headers.get('Authorization')).toBe(`Bearer ${token('alice')}`);
        mutation.flush({});
        dispatchAccountChange();
        expect(auth.getToken()).toBeNull();
        expect(auth.getUserId()).toBeNull();
        expect(sessions.isLocalSessionInvalidated).toBe(true);
        expect(navigation.navigateToAuthAsync).toHaveBeenCalledWith('login');
        expect(localStorage.getItem('authToken')).toBe(token('bob'));
        expect(localStorage.getItem('refreshSession')).toBe('true');
        requests.expectNone(`${authUrl}/logout`);
    });
});

describe('tab session identity restore', () => {
    it('restores pinned subject and owner metadata with identical roles after another tab signs in', async () => {
        sessionStorage.setItem('authToken', token('alice'));
        sessionStorage.setItem('userId', 'alice');
        sessionStorage.setItem('emailConfirmed', 'true');
        sessionStorage.setItem('mustChangePassword', 'false');
        publishBob();
        const { auth, requests } = context();
        await auth.restoreSessionAsync();
        expect(auth.getToken()).toBe(token('alice'));
        expect(auth.getUserId()).toBe('alice');
        expect(auth.isEmailConfirmed()).toBe(true);
        expect(auth.mustChangePassword()).toBe(false);
        requests.expectNone(`${authUrl}/refresh`);
    });

    it.each([false, true])('rejects a foreign cookie refresh without clearing Bob storage (expired=%s)', async expired => {
        sessionStorage.setItem('authToken', token('alice', expired ? { exp: 1 } : {}));
        sessionStorage.setItem('userId', 'alice');
        localStorage.setItem('refreshSession', 'true');
        const { auth, requests, sessions } = context();
        const pending = expired ? auth.restoreSessionAsync() : firstValueFrom(auth.refreshToken());
        const refresh = await vi.waitFor(() => requests.expectOne(`${authUrl}/refresh`));
        publishBob();
        refresh.flush(response('bob'));
        await pending;
        expect(auth.getToken()).toBeNull();
        expect(auth.getUserId()).toBeNull();
        expect(sessions.isLocalSessionInvalidated).toBe(true);
        expect(localStorage.getItem('authToken')).toBe(token('bob'));
        expect(localStorage.getItem('userId')).toBe('bob');
        expect(localStorage.getItem('refreshSession')).toBe('true');
        requests.expectNone(`${authUrl}/logout`);
    });

    it('rejects disagreeing JWT and response subjects even during tokenless cookie bootstrap', async () => {
        localStorage.setItem('refreshSession', 'true');
        const { auth, requests } = context();
        auth.initializeAuth();
        auth.initializeAuth();
        const pending = auth.restoreSessionAsync();
        const refresh = await vi.waitFor(() => requests.expectOne(`${authUrl}/refresh`));
        refresh.flush(response('bob', token('alice')));
        await pending;
        expect(auth.isAuthenticated()).toBe(false);
        expect(auth.getUserId()).toBeNull();
        requests.expectNone(`${authUrl}/logout`);
    });

    it('permits consistent tokenless cookie bootstrap', async () => {
        localStorage.setItem('refreshSession', 'true');
        const { auth, requests } = context();
        const pending = auth.restoreSessionAsync();
        const refresh = await vi.waitFor(() => requests.expectOne(`${authUrl}/refresh`));
        refresh.flush(response('alice'));
        await pending;
        expect(auth.getUserId()).toBe('alice');
        expect(auth.getToken()).toBe(token('alice'));
    });
});

describe('tab session policy and impersonation', () => {
    it('does not rebind unowned policy metadata and requires owner confirmation on restore', async () => {
        sessionStorage.setItem('authToken', token('alice'));
        localStorage.setItem('emailConfirmed', 'true');
        localStorage.setItem('mustChangePassword', 'false');
        localStorage.setItem('refreshSession', 'true');
        const { auth, requests } = context();
        auth.initializeAuth();
        auth.initializeAuth();
        const pending = auth.restoreSessionAsync();
        const refresh = await vi.waitFor(() => requests.expectOne(`${authUrl}/refresh`));
        expect(auth.isEmailConfirmed()).toBe(false);
        expect(auth.mustChangePassword()).toBe(true);

        refresh.flush(response('alice'));
        await pending;

        expect(auth.getUserId()).toBe('alice');
        expect(auth.isEmailConfirmed()).toBe(true);
        expect(auth.mustChangePassword()).toBe(false);
    });

    it('rejects Bob cookie while restoring legacy Alice without owner-bound metadata', async () => {
        sessionStorage.setItem('authToken', token('alice'));
        localStorage.setItem('refreshSession', 'true');
        const { auth, requests, sessions } = context();
        const pending = auth.restoreSessionAsync();
        const refresh = await vi.waitFor(() => requests.expectOne(`${authUrl}/refresh`));
        publishBob();
        refresh.flush(response('bob'));
        await pending;

        expect(auth.isAuthenticated()).toBe(false);
        expect(sessions.isLocalSessionInvalidated).toBe(true);
        expect(localStorage.getItem('authToken')).toBe(token('bob'));
        requests.expectNone(`${authUrl}/logout`);
        expect(await firstValueFrom(auth.refreshToken())).toBeNull();
        await auth.onLogoutAsync(true);
        requests.expectNone(`${authUrl}/refresh`);
        requests.expectNone(`${authUrl}/logout`);
        expect(localStorage.getItem('refreshSession')).toBe('true');
    });

    it('closes a restored token whose unknown policy cannot be confirmed', async () => {
        sessionStorage.setItem('authToken', token('alice'));
        localStorage.setItem('refreshSession', 'true');
        const { auth, requests } = context();
        const pending = auth.restoreSessionAsync();
        const refresh = await vi.waitFor(() => requests.expectOne(`${authUrl}/refresh`));
        refresh.flush(null, { status: 503, statusText: 'Unavailable' });
        await pending;
        expect(auth.isAuthenticated()).toBe(false);
        requests.expectNone(`${authUrl}/logout`);
    });

    it('keeps impersonation separate from a remembered account and never refreshes its shared cookie', async () => {
        const impersonatedToken = token('alice', { fd_impersonation: 'true' });
        sessionStorage.setItem('authToken', impersonatedToken);
        publishBob();
        const { auth, requests } = context();
        auth.initializeAuth();
        dispatchAccountChange();
        expect(auth.getUserId()).toBe('alice');
        expect(auth.getToken()).toBe(impersonatedToken);
        expect(await firstValueFrom(auth.refreshToken())).toBeNull();
        expect(localStorage.getItem('authToken')).toBe(token('bob'));
        requests.expectNone(`${authUrl}/refresh`);
        requests.expectNone(`${authUrl}/logout`);
    });
});
