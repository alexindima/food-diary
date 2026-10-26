import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../environments/environment';
import { LoginRequest } from '../shared/auth/auth.data';
import { BrowserSessionStateService } from '../shared/auth/browser-session-state.service';
import { SessionEventsService } from '../shared/auth/session-events.service';
import { LocalizationService } from '../shared/i18n/localization.service';
import { AuthService } from './auth.service';
import { NavigationService } from './navigation.service';

const base = environment.apiUrls.auth;
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
let navigation: { navigateToAuthAsync: ReturnType<typeof vi.fn>; navigateToLandingAsync: ReturnType<typeof vi.fn> };

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/');
    navigation = {
        navigateToAuthAsync: vi.fn().mockResolvedValue(undefined),
        navigateToLandingAsync: vi.fn().mockResolvedValue(undefined),
    };
    TestBed.configureTestingModule({
        providers: [
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
});

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

describe('server-backed browser sessions', () => {
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
        service.login(login).subscribe();
        requests.expectOne(`${base}/login`).flush({ ...response, sessionId: 'new-session' });
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

describe('browser session failures and logout', () => {
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

describe('browser session policy and explicit Telegram mode', () => {
    it('keeps the required-password-change flow and clears the server flag after a new login', async () => {
        service.login(login).subscribe();
        requests.expectOne(`${base}/login`).flush({ ...response, user: { ...response.user, mustChangePassword: true } });
        expect(service.mustChangePassword()).toBe(true);
        const completed = service.completeRequiredPasswordChangeAsync('updated-password');
        const request = requests.expectOne(`${base}/login`);
        expect(request.request.body).toMatchObject({ rememberMe: true });
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
        TestBed.configureTestingModule({
            providers: [
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
