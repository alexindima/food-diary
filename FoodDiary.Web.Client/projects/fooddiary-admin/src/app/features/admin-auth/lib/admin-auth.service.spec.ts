import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminAuthService } from './admin-auth.service';

const base = environment.apiUrls.auth;
const response = { sessionId: 'admin-session', roles: ['User', 'Admin'], user: { id: 'admin-user' } };
let service: AdminAuthService;
let requests: HttpTestingController;

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/');
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdminAuthService);
    requests = TestBed.inject(HttpTestingController);
});
afterEach(() => {
    requests.verify();
    localStorage.clear();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/');
});

describe('administrative session restoration recovery', () => {
    it.each([HttpStatusCode.TooManyRequests, HttpStatusCode.ServiceUnavailable])(
        'retains known identity after temporary restore failure %s',
        async status => {
            const signedIn = service.exchangeSsoCodeAsync('initial');
            requests.expectOne(`${base}/admin-sso/exchange`).flush(response);
            await signedIn;
            const restored = service.restoreSessionAsync(true);
            requests.expectOne(`${base}/session`).flush({}, { status, statusText: 'Temporarily unavailable' });
            await restored;
            expect(service.getSessionId(), 'Temporary restoration failure must retain the known administrative session').toBe(
                'admin-session',
            );
        },
    );

    it('does not silently adopt another administrative session during restoration', async () => {
        const signedIn = service.exchangeSsoCodeAsync('initial');
        requests.expectOne(`${base}/admin-sso/exchange`).flush(response);
        await signedIn;
        const restored = service.restoreSessionAsync(true);
        requests.expectOne(`${base}/session`).flush({ ...response, sessionId: 'another-session' });
        await restored;
        expect(service.getSessionId(), 'Restoration must not rebind a running admin tab to a replacement session').toBeNull();
    });

    it('ignores an old restoration response after a fresh SSO exchange', async () => {
        const restored = service.restoreSessionAsync();
        const old = requests.expectOne(`${base}/session`);
        const signedIn = service.exchangeSsoCodeAsync('fresh');
        requests.expectOne(`${base}/admin-sso/exchange`).flush({ ...response, sessionId: 'fresh-session' });
        await signedIn;
        old.flush(response);
        await restored;
        expect(service.getSessionId(), 'A late restoration must preserve the fresh SSO selection').toBe('fresh-session');
    });
});

describe('administrative browser sessions', () => {
    it('gets its administrative identity from the SSO response without persisting a token', async () => {
        const exchanged = service.exchangeSsoCodeAsync('one-use-code');
        const request = requests.expectOne(`${base}/admin-sso/exchange`);
        expect(request.request.withCredentials).toBe(true);
        request.flush(response);
        expect(await exchanged).toBe(true);
        expect(service.isAdmin()).toBe(true);
        expect(service.getSessionId()).toBe('admin-session');
        expect(localStorage.getItem('authToken')).toBeNull();
        expect(sessionStorage.getItem('authToken')).toBeNull();
    });

    it('restores the separate administrative cookie after reloading', async () => {
        const restored = service.restoreSessionAsync();
        requests.expectOne(`${base}/session`).flush(response);
        await restored;
        expect(service.isAuthenticated()).toBe(true);
        expect(service.isAdmin()).toBe(true);
    });

    it('does not treat a legacy bearer credential in storage as a signed-in session', () => {
        localStorage.setItem('authToken', 'old-token');
        expect(service.isAuthenticated()).toBe(false);
        expect(service.isAdmin()).toBe(false);
    });

    it('exchanges a fragment code and removes the code from the visible URL', async () => {
        window.history.replaceState({}, '', '/users?page=2#code=one-use-code');
        const applied = service.applySsoFromQueryAsync();
        requests.expectOne(`${base}/admin-sso/exchange`).flush(response);
        await applied;
        expect(window.location.hash).toBe('');
        expect(window.location.search).toBe('?page=2');
        expect(service.isAdmin()).toBe(true);
    });

    it('cleans a return URL only after a successful one-use exchange', async () => {
        const applied = service.tryApplySsoFromReturnUrlAsync('/users?page=3#code=code-for-return');
        requests.expectOne(`${base}/admin-sso/exchange`).flush(response);
        expect(await applied).toBe('/users?page=3');
    });

    it('does not infer administrator permissions from an incomplete response', async () => {
        const applied = service.exchangeSsoCodeAsync('invalid-response');
        requests.expectOne(`${base}/admin-sso/exchange`).flush({ sessionId: 'admin-session' });
        expect(await applied).toBe(false);
        expect(service.isAuthenticated()).toBe(false);
    });

    it('preserves the rate-limit hint and clears it on a subsequent successful exchange', async () => {
        const limited = service.exchangeSsoCodeAsync('limited');
        requests
            .expectOne(`${base}/admin-sso/exchange`)
            .flush({}, { status: HttpStatusCode.TooManyRequests, statusText: 'Too Many Requests' });
        expect(await limited).toBe(false);
        expect(service.ssoRateLimited()).toBe(true);
        const next = service.exchangeSsoCodeAsync('new-code');
        requests.expectOne(`${base}/admin-sso/exchange`).flush(response);
        expect(await next).toBe(true);
        expect(service.ssoRateLimited()).toBe(false);
    });
});

describe('administrative restoration request lifetime', () => {
    it('shares an in-flight cookie restore and keeps the settled session without additional requests', async () => {
        const first = service.restoreSessionAsync();
        const second = service.restoreSessionAsync(true);
        const request = requests.expectOne({ method: 'POST', url: `${base}/session` });
        expect(request.request.withCredentials).toBe(true);
        expect(request.request.headers.has('X-FoodDiary-Session')).toBe(false);
        request.flush(response);
        await Promise.all([first, second]);

        await service.restoreSessionAsync();
        requests.expectNone(`${base}/session`);
        expect(service.getUserId()).toBe('admin-user');
        expect(service.sessionUnavailable()).toBe(false);
    });

    it('waits for Retry-After, restores the same session, and recovers the admin check', async () => {
        const seconds = 2;
        const millisecondsPerSecond = 1000;
        const clock = vi.spyOn(Date, 'now').mockReturnValue(0);
        try {
            await restoreAdminAsync();
            const rejected = service.tryUpgradeToAdminAsync();
            const failed = requests.expectOne(`${base}/session`);
            expect(failed.request.headers.get('X-FoodDiary-Session')).toBe(response.sessionId);
            failed.flush(
                {},
                { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable', headers: { 'Retry-After': `${seconds}` } },
            );
            expect(await rejected).toBe(false);
            expect(service.isAuthenticated()).toBe(true);
            expect(service.sessionUnavailable()).toBe(true);

            clock.mockReturnValue(seconds * millisecondsPerSecond - 1);
            await expect(service.tryUpgradeToAdminAsync()).resolves.toBe(false);
            requests.expectNone(`${base}/session`);

            clock.mockReturnValue(seconds * millisecondsPerSecond);
            const recovered = service.tryUpgradeToAdminAsync();
            requests.expectOne(`${base}/session`).flush(response);
            expect(await recovered).toBe(true);
            expect(service.sessionUnavailable()).toBe(false);
        } finally {
            clock.mockRestore();
        }
    });

    it.each([HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden])('drops a rejected administrative session on %s', async status => {
        await restoreAdminAsync();
        const restored = service.restoreSessionAsync(true);
        requests.expectOne(`${base}/session`).flush({}, { status, statusText: 'Rejected' });
        await restored;

        expect(service.getSessionId()).toBeNull();
        expect(service.getUserId()).toBeNull();
        expect(service.isAdmin()).toBe(false);
        expect(service.sessionUnavailable()).toBe(false);
    });

    it('ignores a stale restoration failure after the user signs in again', async () => {
        const old = service.restoreSessionAsync();
        const obsolete = requests.expectOne(`${base}/session`);
        const current = service.exchangeSsoCodeAsync('new-code');
        requests.expectOne(`${base}/admin-sso/exchange`).flush(response);
        expect(await current).toBe(true);
        obsolete.flush({}, { status: HttpStatusCode.Unauthorized, statusText: 'Rejected' });
        await old;

        expect(service.getSessionId()).toBe(response.sessionId);
        expect(service.isAdmin()).toBe(true);
        expect(service.sessionUnavailable()).toBe(false);
    });
});

describe('administrative sign-in and URL cleanup', () => {
    it('sends the expected account with both confirmation providers and ignores an older response', async () => {
        const expected = adminId<'user'>('admin-user');
        const password = service.signInWithPasswordAsync('admin@example.test', 'password-fixture', expected);
        const pending = requests.expectOne({ method: 'POST', url: `${base}/admin-sso/login` });
        expect(pending.request.withCredentials).toBe(true);
        expect(pending.request.body).toEqual({ email: 'admin@example.test', password: 'password-fixture', expectedUserId: expected });

        const google = service.signInWithGoogleAsync('google-fixture', expected);
        const next = requests.expectOne({ method: 'POST', url: `${base}/admin-sso/google` });
        expect(next.request.withCredentials).toBe(true);
        expect(next.request.body).toEqual({ credential: 'google-fixture', expectedUserId: expected });
        next.flush({ ...response, sessionId: 'new-session' });
        expect(await google).toBe(true);

        pending.flush(response);
        expect(await password).toBe(false);
        expect(service.getSessionId()).toBe('new-session');
    });

    it('uses the cookie on navigation without a code and cleans a rejected code without dropping list filters', async () => {
        window.history.replaceState({}, '', '/users?page=2');
        const restored = service.applySsoFromQueryAsync();
        requests.expectOne(`${base}/session`).flush(response);
        await restored;
        requests.expectNone(`${base}/admin-sso/exchange`);
        expect(service.isAdmin()).toBe(true);

        window.history.replaceState({}, '', '/users?page=2&code=expired-code');
        const applied = service.applySsoFromQueryAsync();
        requests.expectOne(`${base}/admin-sso/exchange`).flush({}, { status: HttpStatusCode.Unauthorized, statusText: 'Expired' });
        await applied;
        expect(window.location.pathname + window.location.search).toBe('/users?page=2&ssoError=1');
        expect(service.isAuthenticated()).toBe(false);
    });

    it('continues cookie sign-in and cleans other legacy storage when local storage is inaccessible', async () => {
        sessionStorage.setItem('authToken', 'legacy-session');
        sessionStorage.setItem('refreshToken', 'legacy-refresh');
        const storage = vi.spyOn(window, 'localStorage', 'get').mockImplementation(() => {
            throw new DOMException('Storage disabled', 'SecurityError');
        });
        try {
            const exchanged = service.exchangeSsoCodeAsync('initial');
            requests.expectOne(`${base}/admin-sso/exchange`).flush(response);
            expect(await exchanged).toBe(true);
            expect(service.isAdmin()).toBe(true);
            expect(sessionStorage.getItem('authToken')).toBeNull();
            expect(sessionStorage.getItem('refreshToken')).toBeNull();
        } finally {
            storage.mockRestore();
        }
    });
});

describe('administrative restoration runtime boundaries', () => {
    it('cancels a timed-out HTTP restore and permits a new recheck', async () => {
        const restorationDeadline = 30_000;
        vi.useFakeTimers();
        try {
            const restored = service.restoreSessionAsync();
            const expired = requests.expectOne(`${base}/session`);
            await vi.advanceTimersByTimeAsync(restorationDeadline);
            await restored;
            expect(expired.cancelled).toBe(true);
            expect(service.sessionUnavailable()).toBe(true);
            expect(service.isAuthenticated()).toBe(false);

            await restoreAdminAsync();
            expect(service.isAdmin()).toBe(true);
            expect(service.sessionUnavailable()).toBe(false);
        } finally {
            vi.useRealTimers();
        }
    });

    it('skips browser storage and cookie restoration on the server', async () => {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [{ provide: PLATFORM_ID, useValue: 'server' }, provideHttpClient(), provideHttpClientTesting()],
        });
        localStorage.setItem('authToken', 'legacy-browser-fixture');
        requests = TestBed.inject(HttpTestingController);
        service = TestBed.inject(AdminAuthService);

        await service.restoreSessionAsync(true);
        await service.applySsoFromQueryAsync();

        requests.expectNone(`${base}/session`);
        requests.expectNone(`${base}/admin-sso/exchange`);
        expect(service.isAuthenticated()).toBe(false);
        expect(localStorage.getItem('authToken')).toBe('legacy-browser-fixture');
    });
});

async function restoreAdminAsync(): Promise<void> {
    const restored = service.restoreSessionAsync();
    requests.expectOne(`${base}/session`).flush(response);
    await restored;
}
