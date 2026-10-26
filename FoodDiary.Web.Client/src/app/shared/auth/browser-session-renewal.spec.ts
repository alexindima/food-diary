import { DOCUMENT } from '@angular/common';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { userFixture } from '../../../testing/api-fixtures';
import { AuthInterceptor } from '../../interceptor/auth.interceptor';
import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';
import { authResponseFromSdk } from '../api/sdk/auth-sdk.mapper';
import { LocalizationService } from '../i18n/localization.service';
import { BrowserSessionStateService } from './browser-session-state.service';

const base = environment.apiUrls.auth;
const response = { sessionId: 'e574a3ec-06a0-4edf-949e-ac5cf0a05d11', roles: ['User'], user: userFixture({ isEmailConfirmed: true }) };
const hour = 3_600_000;
const restorationTimeout = 30_000;
let auth: AuthService;
let requests: HttpTestingController;
let page: Document;
let now: number;

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    now = Date.now();
    vi.spyOn(Date, 'now').mockImplementation(() => now);
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
    requests = TestBed.inject(HttpTestingController);
    page = TestBed.inject(DOCUMENT);
    auth.acceptExternalAuthentication(authResponseFromSdk(response));
});

afterEach(() => {
    requests.verify();
    TestBed.resetTestingModule();
    vi.restoreAllMocks();
    localStorage.clear();
    sessionStorage.clear();
});

describe('ordinary browser session renewal', () => {
    it('releases a stalled restoration so a queued sign-in can proceed', async () => {
        vi.useFakeTimers();
        try {
            const restoration = firstValueFrom(auth.refreshSession());
            const stalled = requests.expectOne(`${base}/session`);
            const login = firstValueFrom(auth.login({ email: 'new@example.com', password: 'Password123!', rememberMe: true }));
            requests.expectNone(`${base}/login`);
            await vi.advanceTimersByTimeAsync(restorationTimeout);
            expect(await restoration).toBe(false);
            expect(stalled.cancelled).toBe(true);
            requests.expectOne(`${base}/login`).flush(response);
            await login;
        } finally {
            vi.useRealTimers();
        }
    });

    it('revalidates on foreground interaction at most once per hour and pins the known session', async () => {
        now += hour;
        page.dispatchEvent(new Event('pointerdown'));
        const renewal = requests.expectOne(`${base}/session`);
        expect(renewal.request.headers.get('X-FoodDiary-Session')).toBe(response.sessionId);
        page.dispatchEvent(new Event('keydown'));
        renewal.flush(response);
        await TestBed.inject(ApplicationRef).whenStable();
        now += hour - 1;
        page.dispatchEvent(new Event('pointerdown'));
        requests.expectNone(`${base}/session`);
        now += 1;
        page.dispatchEvent(new Event('keydown'));
        requests.expectOne(`${base}/session`).flush(response);
    });

    it('does not send a stale restoration while a sign-in is changing the cookie', async () => {
        const login = firstValueFrom(auth.login({ email: 'new@example.com', password: 'Password123!', rememberMe: true }));
        const pending = requests.expectOne(`${base}/login`);
        const restoration = firstValueFrom(auth.refreshSession());
        const premature = requests.match(`${base}/session`);
        pending.flush({ ...response, sessionId: 'a45aac5f-264c-419e-9c08-58b24a420dbb' });
        premature.forEach(request => {
            request.flush(response);
        });
        await login;
        await restoration;
        expect(premature, 'Session renewal must wait for cookie-changing sign-in and discard its stale selection').toHaveLength(0);
        requests.expectNone(`${base}/session`);
    });

    it('does not renew an impersonation context or an anonymous session', async () => {
        TestBed.inject(BrowserSessionStateService).setContext('support-context');
        now += hour;
        page.dispatchEvent(new Event('pointerdown'));
        requests.expectNone(`${base}/session`);
        TestBed.inject(BrowserSessionStateService).setContext(null);
        auth.invalidateLocalSession(false);
        now += hour;
        page.dispatchEvent(new Event('keydown'));
        requests.expectNone(`${base}/session`);
        await TestBed.inject(ApplicationRef).whenStable();
    });
});
